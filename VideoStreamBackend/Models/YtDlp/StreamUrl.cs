namespace VideoStreamBackend.Models.YtDlp;

public class StreamUrl {
    public int Id { get; set; }
    public string Url { get; set; }
    public StreamType StreamType { get; set; }
    /// <summary>
    /// Expiry in unix time seconds.
    /// </summary>
    public long Expiry { get; set; }
    public VideoInfo.Protocol Protocol { get; set; }
    public string Resolution { get; set; }
    public string ResolutionName { get; set; }
}

public enum StreamType {
    Video,
    Audio,
    VideoAndAudio
}