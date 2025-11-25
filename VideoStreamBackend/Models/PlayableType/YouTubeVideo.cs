using VideoStreamBackend.Models.YtDlp;

namespace VideoStreamBackend.Models.PlayableType;

public class YouTubeVideo : QueueItem {
    public Uri? VideoUrl { get; set; }
    public string VideoFormatId { get; set; }
    public string? AudioFormatId { get; set; }
    public required VideoInfo.Protocol Protocol { get; set; }
}