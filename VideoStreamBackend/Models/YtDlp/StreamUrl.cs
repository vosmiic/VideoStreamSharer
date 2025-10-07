namespace VideoStreamBackend.Models.YtDlp;

public class StreamUrl {
    public string Url { get; set; }
    public StreamType StreamType { get; set; }
    /// <summary>
    /// Expiry in unix time seconds.
    /// </summary>
    public long Expiry { get; set; }
}

public enum StreamType {
    Video,
    Audio,
    VideoAndAudio
}