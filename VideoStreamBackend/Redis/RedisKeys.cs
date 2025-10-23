namespace VideoStreamBackend.Redis;

public class RedisKeys {
    public static string RoomConnectionsKey(Guid roomId) => $"room-connections-{roomId}";
    public static string RoomKey(Guid roomId) => $"room-key-{roomId}";
    public static string RoomCurrentVideoField() => "current-video";
    public static string RoomCurrentAudioField() => "current-audio";
    public static string RoomCurrentTimeField() => "current-time";
    public static string RoomUpdateTimeCounterField() => "update-time-counter";
    public static string RoomCurrentLeaderConnectionIdField() => "current-leader-connection-id";
    public static string RoomCurrentStatus() => "current-status";
}