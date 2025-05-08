using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using VideoStreamBackend.Identity;
using VideoStreamBackend.Models;
using VideoStreamBackend.Models.ApiModels;
using VideoStreamBackend.Models.PlayableType;
using VideoStreamBackend.Redis;

namespace VideoStreamBackend.Controllers;

[ApiController]
[Route("[controller]")]
public class RoomController : Controller {
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IDatabase _redis;

    public RoomController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IConnectionMultiplexer connectionMultiplexer) {
        _context = context;
        _userManager = userManager;
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
        
        var connections = _redis.HashGetAll(RedisKeys.RoomConnectionsKey(roomId));
        
        return Ok(new GetRoomResponse {
            Room = new RoomApiModel {
                Id = room.Id,
                Name = room.Name,
                Queue = room.Queue.Select(q => new QueueItemApiModel {
                    Id = q.Id,
                    Title = q.Title,
                    ThumbnailLocation = q.ThumbnailLocation,
                    Order = q.Order,
                    ItemLink = q is YouTubeVideo youTubeVideo ? youTubeVideo.VideoId : ((UploadedMedia)q).Path,
                    Type = q.GetType().Name
                })
            },
            Users = connections.Length > 0 ? connections.Select(connection => connection.Value.ToString()) : Array.Empty<string>()
        });
    }
}