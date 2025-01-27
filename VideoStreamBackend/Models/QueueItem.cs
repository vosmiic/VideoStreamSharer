using System.Text.Json.Serialization;

namespace VideoStreamBackend.Models;

public class QueueItem : GuidPrimaryKey {
    [JsonIgnore]
    public Room Room { get; init; }
    public string Title { get; set; }
    public string? ThumbnailLocation { get; set; }
}