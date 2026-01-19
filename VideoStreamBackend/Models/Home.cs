using System.Text.Json.Serialization;

namespace VideoStreamBackend.Models;

public class Home {
    [JsonPropertyName("RoomNames")]
    public required IEnumerable<HomeRoomNameDisplay> RoomNames { get; set; }
}

public class HomeRoomNameDisplay {
    [JsonPropertyName("Id")]
    public Guid RoomId { get; set; }
    [JsonPropertyName("Name")]
    public required string RoomName { get; set; }
}