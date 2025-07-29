using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using StackExchange.Redis;
using VideoStreamBackend.Helpers;
using VideoStreamBackend.Interfaces;
using VideoStreamBackend.Models;
using VideoStreamBackend.Models.PlayableType;
using VideoStreamBackend.Redis;
using VideoStreamBackend.Services;

namespace VideoStreamBackend.Hubs;

public class PrimaryHub : Hub {
    private readonly IDatabase _redis;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IRoomService _roomService;
    private readonly IQueueItemService _queueItemService;

    #region HubMethods

    private readonly string StatusChangeMethod = "StatusChange";
    private readonly string LoadVideoMethod = "LoadVideo";
    private readonly string PauseVideoMethod = "PauseVideo";
    private readonly string PlayVideoMethod = "PlayVideo";

    #endregion

    public PrimaryHub(IConnectionMultiplexer connectionMultiplexer, UserManager<ApplicationUser> userManager, IRoomService roomService, IQueueItemService queueItemService) {
        _redis = connectionMultiplexer.GetDatabase();
        _userManager = userManager;
        _roomService = roomService;
        _queueItemService = queueItemService;
    }
    
    public override async Task OnDisconnectedAsync(Exception? exception) {
        var roomId = Guid.Parse(Context.GetHttpContext().Request.Query["roomId"]);
        string roomConnectionKey = RedisKeys.RoomConnectionsKey(roomId);
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
        var roomId = Guid.Parse(Context.GetHttpContext().Request.Query["roomId"]);

        string roomConnectionKey = RedisKeys.RoomConnectionsKey(roomId);
        _redis.HashSet(roomConnectionKey, Context.ConnectionId, username);

        HashEntry[] connections = _redis.HashGetAll(roomConnectionKey);
        foreach (HashEntry connection in connections) {
            await Clients.Client(connection.Name).SendAsync("AddUser", username);
        }
        await base.OnConnectedAsync();
    }
    
    [AllowAnonymous]
    public async Task StatusChange(Status status) {
        var roomId = Guid.Parse(Context.GetHttpContext().Request.Query["roomId"]);
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

        await SendToAllRoomClients(roomId, StatusChangeMethod, status);
    }

    [AllowAnonymous]
    public async Task PauseVideo() => await StatusUpdate(Status.Paused);

    [AllowAnonymous]
    public async Task PlayVideo() => await StatusUpdate(Status.Playing);

    [AllowAnonymous]
    public async Task FinishedVideo() {
        var roomId = Guid.Parse(Context.GetHttpContext().Request.Query["roomId"]);
        Room? room = await _roomService.GetRoomById(roomId);
        if (room == null) return;
        QueueItem? latestQueueItem = room.CurrentVideo();
        if (latestQueueItem == null) return;
        await _queueItemService.Remove(latestQueueItem);
        QueueItem? nextQueueItem = room.CurrentVideo(); // get next video
        YtDlpHelper ytDlpHelper = new YtDlpHelper(new CliWrapper());
        StringBuilder standardOutput = new StringBuilder();
        StringBuilder errorOutput = new StringBuilder();
        var result = await ytDlpHelper.GetVideoUrls(nextQueueItem is YouTubeVideo youTubeVideo ? youTubeVideo.VideoUrl : null, standardOutput, errorOutput);
        if (!result.success) return;
        await SendToAllRoomClients(roomId, LoadVideoMethod, result.urls);
        _redis.HashSet(RedisKeys.RoomKey(roomId), RedisKeys.RoomCurrentVideoField(), JsonSerializer.Serialize(result.urls));
    }

    private async Task SendToAllRoomClients(Guid roomId, string method, object? data) {
        string roomConnectionKey = RedisKeys.RoomConnectionsKey(roomId);
        HashEntry[] connections = _redis.HashGetAll(roomConnectionKey);
        foreach (HashEntry connection in connections) {
            await Clients.Client(connection.Name).SendAsync(method, data);
        }
    }
    
    private async Task StatusUpdate(Status status) {
        var roomId = Guid.Parse(Context.GetHttpContext().Request.Query["roomId"]);
        Room? room = await _roomService.GetRoomById(roomId);
        if (room == null) return;
        room.Status = status;
        await _roomService.SaveChanges();
        QueueItem? currentVideo = room?.CurrentVideo();
        if (currentVideo == null) return;
        string? method = null;
        switch (status) {
            case Status.Playing:
                method = PlayVideoMethod;
                break;
            case Status.Paused:
                method = PauseVideoMethod;
                break;
        }
        if (method != null)
            await SendToAllRoomClients(roomId, method, null);
    }
}