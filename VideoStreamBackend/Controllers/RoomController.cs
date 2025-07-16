using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using VideoStreamBackend.Helpers;
using VideoStreamBackend.Identity;
using VideoStreamBackend.Models;
using VideoStreamBackend.Models.ApiModels;
using VideoStreamBackend.Models.PlayableType;
using VideoStreamBackend.Models.YtDlp;
using VideoStreamBackend.Redis;
using VideoStreamBackend.Services;

namespace VideoStreamBackend.Controllers;

[ApiController]
[Route("[controller]")]
public class RoomController : Controller {
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IDatabase _redis;
    private readonly IRoomService _roomService;

    public RoomController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IConnectionMultiplexer connectionMultiplexer, IRoomService roomService) {
        _context = context;
        _userManager = userManager;
        _roomService = roomService;
        _redis = connectionMultiplexer.GetDatabase();
    }

    [HttpPost]
    [Authorize]
    public async Task<ActionResult> CreateRoom([FromBody] string roomName) {
        var user = await _userManager.GetUserAsync(HttpContext.User);
        if (user == null) return Unauthorized();
        Room room = new Room {
            Name = roomName,
            Owner = user
        };
        _context.Rooms.Add(room);
        await _context.SaveChangesAsync();
        return Ok(room.Id);
    }
    
    [HttpGet]
    [Route("{roomId}")]
    public async Task<ActionResult> GetRoom([FromRoute] Guid roomId) {
        var room = await _context.Rooms.Include(room => room.Queue).FirstOrDefaultAsync(room => room.Id == roomId);
        if (room == null) return NotFound();
        
        var connections = await _redis.HashGetAllAsync(RedisKeys.RoomConnectionsKey(roomId));

        RedisValue? urls = await _redis.HashGetAsync(RedisKeys.RoomKey(roomId), RedisKeys.RoomCurrentVideoField());
        List<StreamUrl> streamUrls = new List<StreamUrl>();
        if (urls.HasValue && urls.Value.HasValue) {
            // retrieve the urls
            var xd = urls.Value.ToString();
            streamUrls = JsonSerializer.Deserialize<List<StreamUrl>>(xd);
        } else {
            // store the video in redis for others
            YtDlpHelper ytDlpHelper = new YtDlpHelper();
            var result = await ytDlpHelper.GetVideoUrls(room.CurrentVideo() is YouTubeVideo youTubeVideo ? youTubeVideo.VideoUrl : null);
            if (!result.success) return new StatusCodeResult(500);
            _redis.HashSet(roomId.ToString(), RedisKeys.RoomCurrentVideoField(), JsonSerializer.Serialize(result.urls));
            streamUrls = result.urls;
        }
        
        return Ok(new GetRoomResponse {
            Room = new RoomApiModel {
                Id = room.Id,
                Name = room.Name,
                StreamUrls = streamUrls,
                Queue = room.Queue.Select(q => new QueueItemApiModel {
                    Id = q.Id,
                    Title = q.Title,
                    ThumbnailLocation = q.ThumbnailLocation,
                    Order = q.Order,
                    Type = q.GetType().Name
                })
            },
            Users = connections.Length > 0 ? connections.Select(connection => connection.Value.ToString()) : Array.Empty<string>()
        });
    }
}