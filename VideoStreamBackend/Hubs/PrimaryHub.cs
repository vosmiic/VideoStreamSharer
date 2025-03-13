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

    public override async Task OnDisconnectedAsync(Exception? exception) {
        string roomConnectionKey = RedisKeys.RoomConnectionsKey(Guid.Parse(Context.GetHttpContext().Request.Query["roomId"]));
        string? username = _redis.HashGet(roomConnectionKey, Context.ConnectionId);
        _redis.HashDelete(roomConnectionKey, Context.ConnectionId);
        if (username != null) {
            HashEntry[] connections = _redis.HashGetAll(roomConnectionKey);
            foreach (HashEntry connection in connections) {
                await Clients.Client(connection.Name).SendAsync("RemoveUser", username);
            }
        }
        
        await base.OnDisconnectedAsync(exception);
    }
    
    public override async Task OnConnectedAsync() {
        string? username = null;
        if (Context.User != null) {
            var user = await _userManager.GetUserAsync(Context.User);
            username = user?.UserName;
        }

        if (username == null) {
            // todo generate real random usernames
            username = Guid.NewGuid().ToString().Replace("-", "");
        }

        string roomConnectionKey = RedisKeys.RoomConnectionsKey(Guid.Parse(Context.GetHttpContext().Request.Query["roomId"]));
        _redis.HashSet(roomConnectionKey, Context.ConnectionId, username);

        HashEntry[] connections = _redis.HashGetAll(roomConnectionKey);
        foreach (HashEntry connection in connections) {
            await Clients.Client(connection.Name).SendAsync("AddUser", username);
        }
        await base.OnConnectedAsync();
    }
}