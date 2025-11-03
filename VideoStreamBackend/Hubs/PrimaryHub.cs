using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using StackExchange.Redis;
using VideoStreamBackend.Helpers;
using VideoStreamBackend.Models;
using VideoStreamBackend.Redis;
using VideoStreamBackend.Services;

namespace VideoStreamBackend.Hubs;

public class PrimaryHub : Hub {
    private readonly IDatabase _redis;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IRoomService _roomService;
    private readonly IQueueItemService _queueItemService;
    private readonly ILogger<PrimaryHub> _logger;

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

    public PrimaryHub(IConnectionMultiplexer connectionMultiplexer, UserManager<ApplicationUser> userManager, IRoomService roomService, IQueueItemService queueItemService, ILogger<PrimaryHub> logger) {
        _redis = connectionMultiplexer.GetDatabase();
        _userManager = userManager;
        _roomService = roomService;
        _queueItemService = queueItemService;
        _logger = logger;
    }
    
    public override async Task OnDisconnectedAsync(Exception? exception) {
        var roomId = Context.GetHttpContext()?.Request.Query["roomId"].ToString();
        if (string.IsNullOrEmpty(roomId)) return;
        var parsedRoomId = Guid.Parse(roomId);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, parsedRoomId.ToString());
        string roomConnectionKey = RedisKeys.RoomConnectionsKey(parsedRoomId);
        string? username = _redis.HashGet(roomConnectionKey, Context.ConnectionId);
        _redis.HashDelete(roomConnectionKey, Context.ConnectionId);
        UsersHelper.ChangeLeader(new Room{Id = parsedRoomId}, _redis);
        if (username != null) {
            await Clients.Group(roomId).SendAsync("RemoveUser", username);
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
        bool leaderExists = _redis.HashExists(roomId, RedisKeys.RoomCurrentLeaderConnectionIdField());
        if (!leaderExists) {
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
    public async Task UpdateRoomTime(double time, bool skipCounter) {
        string? roomId = Context.GetHttpContext()?.Request.Query["roomId"];
        if (roomId == null) return;
        if (!skipCounter) {
            if (!LeadershipCheck(roomId)) {
                // keep non-leader users in sync
                var status = _redis.HashGet(roomId, RedisKeys.RoomCurrentStatus());
                if (status != RedisValue.Null && Enum.TryParse(status, out Status parsedStatus)) {
                    string? stringifiedStatus = GetStatusMethod(parsedStatus);
                    if (stringifiedStatus != null)
                        await Clients.Client(Context.ConnectionId).SendAsync(stringifiedStatus);
                }
                return;
            }
        }
        if (!_redis.KeyExists(roomId) || !Guid.TryParse(roomId, out Guid parsedRoomId)) return;
        var roomCurrentStatus = _redis.HashGet(roomId, RedisKeys.RoomCurrentStatus());
        if (roomCurrentStatus != RedisValue.Null) {
            await Clients.Group(roomId).SendAsync(TimeUpdateMethod, time, (int)roomCurrentStatus); // don't need to parse to status
        } else {
            await Clients.Group(roomId).SendAsync(TimeUpdateMethod, time);
        }
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
        var roomId = Context.GetHttpContext().Request.Query["roomId"].ToString();
        bool leaderExists = _redis.HashExists(roomId, RedisKeys.RoomCurrentLeaderConnectionIdField());
        if (!leaderExists) return; // leader must exist in a room, otherwise room doesn't exist or no users are active (either way user shouldn't be allowed to update room status)
        if (_redis.HashExists(RedisKeys.RoomStatusLockKey(roomId), Context.ConnectionId)) {
            // User is attempting to update status too often
            _logger.LogInformation($"Too many status updates from connection {Context.ConnectionId}; status: {status}");
            // Attempt to hand over leadership to someone else
            UsersHelper.ChangeLeader(new Room{Id = Guid.Parse(roomId)}, _redis);
            return;
        }
        _logger.LogInformation($"Accepted status update from connection {Context.ConnectionId}; status: {status}");
        _redis.HashSet(roomId, RedisKeys.RoomCurrentStatus(), (int)status);
        _redis.HashSet(RedisKeys.RoomStatusLockKey(roomId), Context.ConnectionId, RedisValue.EmptyString);
        _redis.HashFieldExpire(RedisKeys.RoomStatusLockKey(roomId), [Context.ConnectionId], DateTime.UtcNow.AddMilliseconds(250));
        string? method = GetStatusMethod(status);

        if (method != null) {
            await Clients.Group(roomId).SendAsync(method);
        }
    }

    private bool LeadershipCheck(string roomId) {
        RedisValue leaderConnectionId = _redis.HashGet(roomId, RedisKeys.RoomCurrentLeaderConnectionIdField());
        if (leaderConnectionId == RedisValue.Null || Context.ConnectionId != leaderConnectionId)
            return false;
        return true;
    }

    private string? GetStatusMethod(Status status) {
        switch (status) {
            case Status.Playing:
                return PlayVideoMethod;
            case Status.Paused:
                return PauseVideoMethod;
        }
        return null;
    }
}