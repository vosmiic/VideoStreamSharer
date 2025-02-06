using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using StackExchange.Redis;
using VideoStreamBackend.Models;
using VideoStreamBackend.Redis;

namespace VideoStreamBackend.Hubs;

public class PrimaryHub : Hub {
    private readonly IDatabase _redis;
    private readonly UserManager<ApplicationUser> _userManager;
    public PrimaryHub(IConnectionMultiplexer connectionMultiplexer, UserManager<ApplicationUser> userManager) {
        _redis = connectionMultiplexer.GetDatabase();
        _userManager = userManager;
    }
    public async Task SendMessage(string message) =>
        await Clients.Client(Context.ConnectionId).SendAsync("ReceiveMessage", Context.ConnectionId);

    public async Task JoinedRoom(Guid roomId) {
        string? username = null;
        if (Context.User != null) {
            var user = await _userManager.GetUserAsync(Context.User);
            username = user?.UserName;
        }

        if (username == null) {
            // todo generate real random usernames
            username = Guid.NewGuid().ToString().Replace("-", "");
        }
        _redis.HashSet(RedisKeys.RoomConnectionsKey(roomId), Context.ConnectionId, username);
    }

    public override Task OnDisconnectedAsync(Exception? exception) {
        _redis.HashDelete(RedisKeys.RoomConnectionsKey(Guid.Parse(Context.GetHttpContext().Request.Query["roomId"])), Context.ConnectionId);
        return base.OnDisconnectedAsync(exception);
    }
}