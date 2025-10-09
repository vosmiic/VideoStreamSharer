using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using VideoStreamBackend.Helpers;
using VideoStreamBackend.Identity;
using VideoStreamBackend.Models;
using VideoStreamBackend.Models.ApiModels;
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

        var redisCurrentTime = _redis.HashGet(roomId.ToString(), RedisKeys.RoomCurrentTimeField());
        double currentTime = redisCurrentTime != RedisValue.Null && redisCurrentTime.TryParse(out double time) ? time : room.CurrentTime;
        
        return Ok(new GetRoomResponse {
            Room = new RoomApiModel {
                Id = room.Id,
                Name = room.Name,
                StreamUrls = room.CurrentVideo() != null ? await RoomHelper.GetStreamUrls(_redis, Request, room) : null,
                Status = room.Status,
                CurrentTime = currentTime,
                Queue = RoomHelper.GetQueueModel(room, Request)
            },
            Users = connections.Length > 0 ? connections.Select(connection => connection.Value.ToString()) : Array.Empty<string>()
        });
    }
}