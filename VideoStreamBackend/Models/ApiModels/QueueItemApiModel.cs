namespace VideoStreamBackend.Models.ApiModels;

public class QueueItemApiModel : GuidPrimaryKey {
    public string Title { get; set; }
    public string? ThumbnailLocation { get; set; }
    public int Order { get; set; }
    /// <summary>
    /// Video ID if YouTube or media location if file
    /// </summary>
    public string ItemLink { get; set; }
    /// <summary>
    /// Type of queue item
    /// </summary>
    public string Type { get; set; }
}