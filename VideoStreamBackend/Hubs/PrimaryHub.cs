using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using StackExchange.Redis;
using VideoStreamBackend.Helpers;
using VideoStreamBackend.Models;
using VideoStreamBackend.Models.ApiModels;
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
    public static readonly string LoadVideoMethod = "LoadVideo";
    private readonly string PauseVideoMethod = "PauseVideo";
    private readonly string PlayVideoMethod = "PlayVideo";
    private readonly string TimeUpdateMethod = "TimeUpdate";
    public static readonly string QueueAdded = "QueueAdded";
    public static readonly string VideoFinishedMethod = "VideoFinished";
    private readonly string SetQueueMethod = "SetQueue";
    public static readonly string DeleteQueueMethod = "DeleteQueue";
    public static readonly string QueueOrderChangedMethod = "QueueOrderChanged";
    public static readonly string VideoChangedMethod = "VideoChanged";

    #endregion

    public PrimaryHub(IConnectionMultiplexer connectionMultiplexer, UserManager<ApplicationUser> userManager, IRoomService roomService, IQueueItemService queueItemService) {
        _redis = connectionMultiplexer.GetDatabase();
        _userManager = userManager;
        _roomService = roomService;
        _queueItemService = queueItemService;
    }
    
    public override async Task OnDisconnectedAsync(Exception? exception) {
        var roomId = Context.GetHttpContext()?.Request.Query["roomId"].ToString();
        if (string.IsNullOrEmpty(roomId)) return;
        var parsedRoomId = Guid.Parse(roomId);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, parsedRoomId.ToString());
        string roomConnectionKey = RedisKeys.RoomConnectionsKey(parsedRoomId);
        string? username = _redis.HashGet(roomConnectionKey, Context.ConnectionId);
        _redis.HashDelete(roomConnectionKey, Context.ConnectionId);
        RedisValue currentLeader = _redis.HashGet(roomId, RedisKeys.RoomCurrentLeaderConnectionIdField());
        HashEntry[] connections = _redis.HashGetAll(roomConnectionKey);
        if (currentLeader != RedisValue.Null && currentLeader == Context.ConnectionId) {
            // remove leader
            _redis.HashDelete(roomId, RedisKeys.RoomCurrentLeaderConnectionIdField());
            // and assign a new leader
            var firstUser = connections.FirstOrDefault().Name;
            if (firstUser != RedisValue.Null)
                _redis.HashSet(roomId, RedisKeys.RoomCurrentLeaderConnectionIdField(), firstUser);
        }
        if (username != null) {
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

        string? roomId = Context.GetHttpContext()?.Request.Query["roomId"].ToString();
        if (string.IsNullOrEmpty(roomId)) return;
        Guid parsedRoomId = Guid.Parse(roomId);
        
        await Groups.AddToGroupAsync(Context.ConnectionId, parsedRoomId.ToString());

        string roomConnectionKey = RedisKeys.RoomConnectionsKey(parsedRoomId);
        _redis.HashSet(roomConnectionKey, Context.ConnectionId, username);
        RedisValue currentLeader = _redis.HashGet(roomId, RedisKeys.RoomCurrentLeaderConnectionIdField());
        if (currentLeader == RedisValue.Null || !_redis.HashExists(roomConnectionKey, currentLeader)) {
            // no leader so set the current user to be the leader
            _redis.HashSet(roomId, RedisKeys.RoomCurrentLeaderConnectionIdField(), Context.ConnectionId);
        }

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

        await Clients.Group(roomId.ToString()).SendAsync(StatusChangeMethod, status);
    }

    [AllowAnonymous]
    public async Task UpdateRoomTime(double time, bool skipCounter) {
        string? roomId = Context.GetHttpContext()?.Request.Query["roomId"];
        if (roomId == null) return;
        if (!skipCounter) {
            if (!LeadershipCheck(roomId))
                return;
        }
        if (!_redis.KeyExists(roomId) || !Guid.TryParse(roomId, out Guid parsedRoomId)) return;
        await Clients.Group(roomId).SendAsync(TimeUpdateMethod, time);
        long counter = 0;
        if (!skipCounter) {
            counter = _redis.HashIncrement(roomId, RedisKeys.RoomUpdateTimeCounterField());
        }
        _redis.HashSet(roomId, RedisKeys.RoomCurrentTimeField(), time);
        if (counter >= 5 || skipCounter) {
            Room? room = await _roomService.GetRoomById(parsedRoomId);
            if (room == null) return;
            room.CurrentTime = time;
            await _roomService.SaveChanges();
            _redis.HashDelete(roomId, RedisKeys.RoomUpdateTimeCounterField());
        }
    }

    [AllowAnonymous]
    public async Task PauseVideo() => await StatusUpdate(Status.Paused);

    [AllowAnonymous]
    public async Task PlayVideo() => await StatusUpdate(Status.Playing);

    [AllowAnonymous]
    public async Task FinishedVideo(Guid videoId) {
        string? roomId = Context.GetHttpContext()?.Request.Query["roomId"];
        if (roomId == null) return;
        if (!LeadershipCheck(roomId))
            return;
        var parsedRoomId = Guid.Parse(roomId);
        Room? room = await _roomService.GetRoomById(parsedRoomId);
        if (room == null) return;
        
        QueueItem? latestQueueItem = room.CurrentVideo();
        if (latestQueueItem == null || latestQueueItem.Id != videoId) return;

        await QueueHelper.RemoveRoomVideo(_queueItemService, _redis, _roomService, Clients, Context.GetHttpContext().Request, room, latestQueueItem);
    }

    public async Task DeleteVideo(Guid videoId) {
        string? roomId = Context.GetHttpContext()?.Request.Query["roomId"];
        if (roomId == null) return;
        var parsedRoomId = Guid.Parse(roomId);
        Room? room = await _roomService.GetRoomById(parsedRoomId);
        if (room == null) return;

        QueueItem? queueItem = await _queueItemService.GetQueueItemById(videoId);
        if (queueItem == null || queueItem.Room.Id != parsedRoomId) return;

        await QueueHelper.RemoveRoomVideo(_queueItemService, _redis, _roomService, Clients, Context.GetHttpContext().Request, room, queueItem);
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
            await Clients.Group(roomId.ToString()).SendAsync(method);
    }

    private bool LeadershipCheck(string roomId) {
        RedisValue leaderConnectionId = _redis.HashGet(roomId, RedisKeys.RoomCurrentLeaderConnectionIdField());
        if (leaderConnectionId == RedisValue.Null || Context.ConnectionId != leaderConnectionId)
            return false;
        return true;
    }
}