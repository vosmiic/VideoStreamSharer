using FileSignatures;
using FileSignatures.Formats;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Net.Http.Headers;
using StackExchange.Redis;
using VideoStreamBackend.Helpers;
using VideoStreamBackend.Hubs;
using VideoStreamBackend.Interfaces;
using VideoStreamBackend.Models;
using VideoStreamBackend.Models.ApiModels;
using VideoStreamBackend.Models.PlayableType;
using VideoStreamBackend.Models.YtDlp;
using VideoStreamBackend.Services;

namespace VideoStreamBackend.Controllers;

[ApiController]
[Route("[controller]")]
public class QueueController : Controller {
    private readonly IRoomService _roomService;
    private readonly IQueueItemService _queueItemService;
    private IHubContext<PrimaryHub> _primaryHubContext { get; set; }
    private readonly IDatabase  _redis;
    private readonly IConfiguration _configuration;

    public QueueController(IRoomService roomService, IQueueItemService queueItemService, IHubContext<PrimaryHub> primaryHubContext, IConnectionMultiplexer connectionMultiplexer, IConfiguration configuration) {
        _roomService = roomService;
        _queueItemService = queueItemService;
        _primaryHubContext = primaryHubContext;
        _configuration = configuration;
        _redis = connectionMultiplexer.GetDatabase();
    }

    [HttpPost]
    [Route("{roomId}/add")]
    public async Task<ActionResult> AddToQueue([FromRoute] Guid roomId, [FromBody] QueueAdd data) {
        var room = await _roomService.GetRoomById(roomId);
        if (room == null) return new NotFoundResult();

        Uri uri = QueueHelper.GetYouTubeUrl(new Uri(data.Url));
        YtDlpHelper ytDlpHelper = new YtDlpHelper(new CliWrapper());
        (List<StreamUrl>? urls, VideoInfo? videoInfo, string? error) streams = await QueueHelper.GetStreams(ytDlpHelper, uri);
        if (streams is { error: not null, urls: not null, videoInfo: not null }) return new BadRequestObjectResult($"Error: {streams.error ?? "could not retrieve video info."}");
        
        var video = new YouTubeVideo {
            Title = streams.videoInfo.Title,
            VideoUrl = uri,
            ThumbnailLocation = streams.videoInfo.Thumbnail,
            Order = room.Queue.Any() ? room.Queue.Max(queue => queue.Order) + 1 : 0,
        };
        room.Queue.Add(video);
        await _roomService.SaveChanges();
        QueueHelper.StoreStreams(_redis, streams.urls, room.Id, video.Id);
        await _primaryHubContext.Clients.Group(roomId.ToString()).SendAsync(PrimaryHub.QueueAdded, video);
        if (video.Order == 0)
            await _primaryHubContext.Clients.Group(room.StringifiedId).SendAsync(PrimaryHub.LoadVideoMethod, QueueHelper.FilterStreams(streams.urls, HttpContext.Request));

        /* Old youtube solution
        Regex regex = new Regex(@"(?:youtube\.com\/(?:[^\/]+\/.+\/|(?:v|e(?:mbed)?)\/|.*[?&]v=)|youtu\.be\/)([^""&?\/\s]{11})", RegexOptions.IgnoreCase);

        MatchCollection matchCollection = regex.Matches(url);
        string? videoId = null;
        foreach (Match match in matchCollection) {
            GroupCollection groupCollection = match.Groups;
            videoId = groupCollection[1].Value;
        }

        if (videoId == null) return new BadRequestResult();

        room.Queue.Add(new YouTubeVideo {
            Title = "YouTube Video", //todo get title from youtube metadata endpoint
            VideoId = videoId
        });

        await _roomService.SaveChanges();*/
        return Ok();
    }

    [HttpPut]
    [Route("{roomId}/order")]
    public async Task<ActionResult> ChangeOrderOfQueue([FromRoute] Guid roomId, [FromBody] List<QueueOrderResponse> queue) {
        Room? room = await _roomService.GetRoomById(roomId);
        if (room == null) return new NotFoundResult();
        bool currentVideoChanged = false;

        foreach (QueueOrderResponse queueOrderResponse in queue) {
            QueueItem? originalQueue = room.Queue.FirstOrDefault(q => q.Id == queueOrderResponse.Id);
            if (originalQueue != null && originalQueue.Order != queueOrderResponse.Order) {
                originalQueue.Order = queueOrderResponse.Order;
                if (queueOrderResponse.Order == 0) currentVideoChanged = true;
            }
        }

        if (room.Queue.Any(queue => room.Queue.Count(item => item.Order == queue.Order) > 1)) {
            return BadRequest("Duplicate order");
        }

        await _queueItemService.SaveChanges();
        string stringifiedRoomId = roomId.ToString();
        if (currentVideoChanged) {
            List<StreamUrl>? newStreamUrls = await RoomHelper.GetStreamUrls(_redis, Request, room);
            if (newStreamUrls == null)  return new BadRequestResult();
            await _primaryHubContext.Clients.Group(stringifiedRoomId).SendAsync(PrimaryHub.VideoChangedMethod, QueueHelper.FilterStreams(newStreamUrls, ControllerContext.HttpContext.Request));
        }
        
        await _primaryHubContext.Clients.Group(stringifiedRoomId).SendAsync(PrimaryHub.QueueOrderChangedMethod, room.Queue.Select(queueItem => new {queueItem.Id, queueItem.Order}));
        return Ok();
    }

    [HttpGet]
    [Route("lookup")]
    public async Task<ActionResult> Lookup([FromQuery] string url) {
        Uri uri = new Uri(url);
        YtDlpHelper ytDlpHelper = new YtDlpHelper(new CliWrapper());
        (VideoInfo? videoInfo, string? error) info = await QueueHelper.RetrieveVideoInfoWithRetries(ytDlpHelper, uri);
        if (info.videoInfo == null) return new BadRequestObjectResult($"Error: {info.error ?? "could not retrieve video info"}");

        var model = new LookupModel {
            Title = info.videoInfo.Title,
            ChannelTitle = info.videoInfo.Channel.Name,
            ThumbnailUrl = info.videoInfo.Thumbnail,
            Viewcount = $"{info.videoInfo.Viewcount:N0}",
            Duration = new DateTimeOffset().AddSeconds(info.videoInfo.Duration).ToString("t"),
        };

        return Ok(model);
    }

    [HttpPost]
    [Route("{roomId}/upload")]
    [RequestFormLimits(ValueLengthLimit = int.MaxValue, MultipartBodyLengthLimit = int.MaxValue)]
    public async Task<IActionResult> UploadVideo([FromRoute] Guid roomId) {
        if (!Request.ContentType?.StartsWith("multipart/form-data", StringComparison.OrdinalIgnoreCase) ?? false) {
            return BadRequest("Unsupported content type");
        }
        
        Room? room = await _roomService.GetRoomById(roomId);
        if (room == null) return new NotFoundResult();

        string? boundary = HeaderUtilities.RemoveQuotes(MediaTypeHeaderValue.Parse(Request.ContentType).Boundary).Value;
        if (boundary == null) return BadRequest("No boundary");
        
        var reader = new MultipartReader(boundary, Request.Body);
        CancellationToken cancellationToken = HttpContext.RequestAborted;
        string? path = null;
        string? fileName = null;

        try {
            while (await reader.ReadNextSectionAsync(cancellationToken) is { } section) {
                ContentDispositionHeaderValue? contentDisposition = section.GetContentDispositionHeader();
                if (contentDisposition != null && contentDisposition.IsFileDisposition()) {
                    fileName = contentDisposition.FileName.Value ?? contentDisposition.FileNameStar.Value;
                    if (!string.IsNullOrEmpty(fileName)) {
                        fileName = HeaderUtilities.RemoveQuotes(fileName).Value;
                    } else {
                        return BadRequest("No filename");
                    }

                    var roomDirectoryTask = RoomHelper.GetRoomDirectory(_configuration, roomId);
                    if (!roomDirectoryTask.success || roomDirectoryTask.path == null) return BadRequest($"Error: {roomDirectoryTask.error}");
                    string filePath = RoomHelper.GetVideoFilePath(roomDirectoryTask.path, fileName);
                    path = filePath;
                    await using FileStream fileStream = new FileStream(path: filePath, mode: FileMode.Create, access: FileAccess.Write, share: FileShare.None, bufferSize: 16 * 1024 * 1024, useAsync: true);
                    await section.Body.CopyToAsync(fileStream, cancellationToken);
                } else {
                    return BadRequest("No content");
                }
            }
        } catch (Exception ex) {
            Console.WriteLine(ex.Message); //todo log this properly
        }

        if (path == null) return BadRequest("No video path");
        
        UploadedMedia uploadedMedia = new UploadedMedia {
            Title = fileName,
            Path = path.Replace(_configuration["videoUploadStorageFolder"], ""),
            Order = room.Queue.Any() ? room.Queue.Max(queue => queue.Order) + 1 : 0,
        };

        YtDlpHelper ytDlpHelper = new YtDlpHelper(new CliWrapper());
        var generateThumbnailTask = await QueueHelper.GenerateThumbnail(ytDlpHelper, _configuration, roomId, path, Guid.NewGuid().ToString().Replace("-", ""));
        if (generateThumbnailTask is { success: false, thumbnailPath: not null }) {
            Console.WriteLine(generateThumbnailTask.error); // todo log this
            string? staticFileDirectory = _configuration["staticFileDirectory"];
            if (string.IsNullOrWhiteSpace(staticFileDirectory)) return BadRequest("No static file folder in configuration");
            DirectoryInfo staticFileDirectoryInfo = new DirectoryInfo(staticFileDirectory);
            if (!staticFileDirectoryInfo.Exists) return BadRequest("Static file directory not found");
            var staticFileDirectoryPermissions = FileHelper.GetDirectoryPermissions(staticFileDirectoryInfo);
            if (!staticFileDirectoryPermissions.read) return BadRequest("Cannot read static file directory");
            FileInfo? defaultThumbnailFile = staticFileDirectoryInfo.EnumerateFiles("defaultThumbnail.*").FirstOrDefault();
            if (defaultThumbnailFile == null) return BadRequest("No default thumbnail file found in static file directory");
            FileFormatInspector fileFormatInspector = new FileFormatInspector();
            var format = fileFormatInspector.DetermineFileFormat(defaultThumbnailFile.OpenRead());
            if (format == null) return BadRequest("File format of default thumbnail file could not be determined");
            if (format is not Image) return BadRequest($"Invalid default thumbnail file format; must be an image, detected format: {format.MediaType}");
            uploadedMedia.ThumbnailLocation = defaultThumbnailFile.FullName.Replace(_configuration["videoUploadStorageFolder"], "");
        } else {
            uploadedMedia.ThumbnailLocation = generateThumbnailTask.thumbnailPath.Replace(_configuration["videoUploadStorageFolder"], "");
        }
        
        room.Queue.Add(uploadedMedia);
        await _roomService.SaveChanges();
        _roomService.Detach(room);
        uploadedMedia.ThumbnailLocation = RoomHelper.GetVideoFileUrl(Request, uploadedMedia.ThumbnailLocation);
        await _primaryHubContext.Clients.Group(room.StringifiedId).SendAsync(PrimaryHub.QueueAdded, uploadedMedia, cancellationToken: cancellationToken);
        if (uploadedMedia.Order == 0)
            await _primaryHubContext.Clients.Group(room.StringifiedId).SendAsync(PrimaryHub.LoadVideoMethod, QueueHelper.FilterStreams(await RoomHelper.GetStreamUrls(_redis, Request, room), ControllerContext.HttpContext.Request), cancellationToken: cancellationToken);

        return Ok();
    }

    
}