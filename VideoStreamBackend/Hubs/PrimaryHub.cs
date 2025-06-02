using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using StackExchange.Redis;
using VideoStreamBackend.Models;
using VideoStreamBackend.Redis;
using VideoStreamBackend.Services;

namespace VideoStreamBackend.Hubs;

public class PrimaryHub : Hub {
    private readonly IDatabase _redis;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IRoomService _roomService;

    #region HubMethods

    private readonly string StatusChangeMethod = "StatusChange";

    #endregion

    public PrimaryHub(IConnectionMultiplexer connectionMultiplexer, UserManager<ApplicationUser> userManager, IRoomService roomService) {
        _redis = connectionMultiplexer.GetDatabase();
        _userManager = userManager;
        _roomService = roomService;
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
    
    [AllowAnonymous]
    public async Task StatusChange(Status status) {
        Guid roomId = Guid.Parse(Context.GetHttpContext().Request.Query["roomId"]);
        Room? room = await _roomService.GetRoomById(roomId);
        if (room == null || room.Status == status) return;
        try {
            room.Status = status;
            await _roomService.SaveChanges();
        } catch (Exception) {
            // todo log the error here
            await Clients.Client(Context.ConnectionId).SendAsync(StatusChangeMethod, Context.ConnectionId);
            return;
        }

        await SendToAllRoomClients(StatusChangeMethod, status);
    }

    private async Task SendToAllRoomClients(string method, object data) {
        string roomConnectionKey = RedisKeys.RoomConnectionsKey(Guid.Parse(Context.GetHttpContext().Request.Query["roomId"]));
        HashEntry[] connections = _redis.HashGetAll(roomConnectionKey);
        foreach (HashEntry connection in connections) {
            await Clients.Client(connection.Name).SendAsync(method, data);
        }
    }
}