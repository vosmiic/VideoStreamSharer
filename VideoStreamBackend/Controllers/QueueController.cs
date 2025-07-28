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
    public async Task<ActionResult> AddToQueue([FromRoute] Guid roomId, [FromBody] string url) {
        var room = await _roomService.GetRoomById(roomId);
        if (room == null) return new NotFoundResult();
        
        Uri uri = new Uri(url);
        YtDlpHelper ytDlpHelper = new YtDlpHelper(new CliWrapper());
        StringBuilder standardOutput = new StringBuilder();
        StringBuilder errorOutput = new StringBuilder();
        (VideoInfo? videoInfo, bool success, string? error) videoInfo = await ytDlpHelper.GetVideoInfo(uri, standardOutput, errorOutput);
        if (!videoInfo.success || videoInfo.videoInfo == null) return new BadRequestObjectResult($"Error: {videoInfo.error}");
        room.Queue.Add(new YouTubeVideo {
            Title = videoInfo.videoInfo.Title,
            VideoUrl = uri
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
        
        if (room.Queue.Any(queue => room.Queue.Count(item => item.Order == queue.Order) > 1))
        {
            return BadRequest("Duplicate order");
        }
        
        await _queueItemService.SaveChanges();
        return Ok();
    }
}