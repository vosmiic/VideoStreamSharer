using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using VideoStreamBackend.Helpers;
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

    public QueueController(IRoomService roomService, IQueueItemService queueItemService) {
        _roomService = roomService;
        _queueItemService = queueItemService;
    }

    [HttpPost]
    [Route("{roomId}/add")]
    public async Task<ActionResult> AddToQueue([FromRoute] Guid roomId, [FromBody] QueueAdd data) {
        var room = await _roomService.GetRoomById(roomId);
        if (room == null) return new NotFoundResult();

        Uri uri = new Uri(data.Url);
        YtDlpHelper ytDlpHelper = new YtDlpHelper(new CliWrapper());
        (VideoInfo? videoInfo, string? error) info = await RetrieveVideoInfoWithRetries(ytDlpHelper, uri);
        if (info.videoInfo == null) return new BadRequestObjectResult($"Error: {info.error ?? "could not retrieve video info."}");
        
        // confirm IDs exist
        var videoFormat = info.videoInfo.Formats.FirstOrDefault(format => !format.IsAudio && format.FormatId == data.VideoFormatId);
        var audioFormat = info.videoInfo.Formats.FirstOrDefault(format => format.IsAudio && format.FormatId == data.AudioFormatId);
        if (videoFormat == null || audioFormat == null) return new BadRequestObjectResult($"Error: format could not be found.");
        
        room.Queue.Add(new YouTubeVideo {
            Title = info.videoInfo.Title,
            VideoUrl = uri,
            VideoFormatId = data.VideoFormatId,
            AudioFormatId = data.AudioFormatId,
        });
        await _roomService.SaveChanges();
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

        foreach (QueueOrderResponse queueOrderResponse in queue) {
            QueueItem? originalQueue = room.Queue.FirstOrDefault(q => q.Id == queueOrderResponse.Id);
            if (originalQueue != null && originalQueue.Order != queueOrderResponse.Order) {
                originalQueue.Order = queueOrderResponse.Order;
            }
        }

        if (room.Queue.Any(queue => room.Queue.Count(item => item.Order == queue.Order) > 1)) {
            return BadRequest("Duplicate order");
        }

        await _queueItemService.SaveChanges();
        return Ok();
    }

    [HttpGet]
    [Route("lookup")]
    public async Task<ActionResult> Lookup([FromQuery] string url) {
        Uri uri = new Uri(url);
        YtDlpHelper ytDlpHelper = new YtDlpHelper(new CliWrapper());
        (VideoInfo? videoInfo, string? error) info = await RetrieveVideoInfoWithRetries(ytDlpHelper, uri);
        if (info.videoInfo == null) return new BadRequestObjectResult($"Error: {info.error ?? "could not retrieve video info"}");

        var model = new LookupModel {
            Title = info.videoInfo.Title,
            ChannelTitle = info.videoInfo.Channel.Name,
            ThumbnailUrl = info.videoInfo.Thumbnail,
            Viewcount = $"{info.videoInfo.Viewcount:N0}",
            Duration = new DateTimeOffset().AddSeconds(info.videoInfo.Duration).ToString("t"),
            AudioFormats = info.videoInfo.Formats
                .Where(format => format is { Protocol: VideoInfo.Protocol.https, IsAudio: true })
                .OrderByDescending(stream => stream.Quality)
                .ThenByDescending(stream => stream.FilesizeApprox)
                .DistinctBy(stream => stream.Resolution)
                .Select(stream => new LookupModel.LookupFormats {
                    Id = stream.FormatId,
                    Value = stream.Quality.ToString("F1"),
                }).OrderByDescending(format => format.Id),
            VideoFormats = info.videoInfo.Formats
                .Where(format => format is { Protocol: VideoInfo.Protocol.https, IsAudio: false })
                .OrderByDescending(stream => stream.Quality)
                .ThenByDescending(stream => stream.FilesizeApprox)
                .DistinctBy(stream => stream.Resolution)
                .Select(stream => new LookupModel.LookupFormats {
                    Id = stream.FormatId,
                    Value = stream.Resolution,
                }).OrderByDescending(format => format.Id)
        };

        return Ok(model);
    }

    private static async Task<(VideoInfo? videoInfo, string? error)> RetrieveVideoInfoWithRetries(YtDlpHelper ytDlpHelper, Uri uri) {
        VideoInfo? info = null;
        int counter = 0;
        while (counter < 3) {
            StringBuilder standardOutput = new StringBuilder();
            StringBuilder errorOutput = new StringBuilder();
            (VideoInfo? videoInfo, bool success, string? error) videoInfo = await ytDlpHelper.GetVideoInfo(uri, standardOutput, errorOutput);
            if (!videoInfo.success || videoInfo.videoInfo == null) return (null, videoInfo.error);

            if (videoInfo.videoInfo.Formats.All(format => format.IsAudio && format.Protocol != VideoInfo.Protocol.https) ||
                videoInfo.videoInfo.Formats.All(format => !format.IsAudio && format.Protocol != VideoInfo.Protocol.https)) {
                // retry the request, sometimes the server provides odd results
                counter++;
            } else {
                info = videoInfo.videoInfo;
                break;
            }
        }

        return (info, null);
    }
}