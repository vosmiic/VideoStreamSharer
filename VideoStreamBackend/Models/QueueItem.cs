using System.Text.Json.Serialization;

namespace VideoStreamBackend.Models;

public class QueueItem : GuidPrimaryKey {
    [JsonIgnore]
    public virtual Room Room { get; init; }
    public Guid RoomId { get; set; }
    public string Title { get; set; }
    public string? ThumbnailLocation { get; set; }
    public int Order { get; set; }
}