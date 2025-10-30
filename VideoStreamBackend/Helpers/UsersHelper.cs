using StackExchange.Redis;
using VideoStreamBackend.Models;
using VideoStreamBackend.Redis;

namespace VideoStreamBackend.Helpers;

public class UsersHelper {
    public static void ChangeLeader(Room room, IDatabase redis) {
        RedisValue currentLeader = redis.HashGet(room.StringifiedId, RedisKeys.RoomCurrentLeaderConnectionIdField());
        var connections = redis.HashGetAll(RedisKeys.RoomConnectionsKey(room.Id));
        var newLeader = connections.FirstOrDefault(entry => entry.Value != currentLeader);
        redis.HashSet(room.StringifiedId, RedisKeys.RoomCurrentLeaderConnectionIdField(), newLeader.Value);
    }
}