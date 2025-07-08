namespace VideoStreamBackend.Models.YtDlp;

public class StreamUrl {
    public string Url { get; set; }
    public StreamType StreamType { get; set; }
}

public enum StreamType {
    Video,
    Audio
}