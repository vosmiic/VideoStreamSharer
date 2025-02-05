namespace VideoStreamBackend.Redis;

public class RedisKeys {
    public static string RoomConnectionsKey(Guid roomId) => $"room-connections-{roomId}";
}