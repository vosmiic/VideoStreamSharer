using System.Text.Json.Serialization;

namespace VideoStreamBackend.Models.YtDlp;

public class VideoInfo {
    public string Title { get; set; }
    public StreamFormat[] Formats { get; set; }
    public string Thumbnail { get; set; }
    public VideoChannel Channel { get; set; }
    public int Duration { get; set; }
    public long Viewcount { get; set; }

    public class StreamFormat {
        private string? Dynamic_range { get; set; }
        [JsonPropertyName("format_id")]
        public string FormatId { get; set; }
        public string Resolution { get; set; }
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public Protocol Protocol { get; set; }
        public double Quality { get; set; }
        [JsonPropertyName("filesize")]
        public long Filesize { get; set; }
        public bool IsAudio => Resolution == "audio only";
        [JsonPropertyName("vcodec")]
        public string? VideoCodec { get; set; }
        [JsonPropertyName("acodec")]
        public string? AudioCodec { get; set; }
        [JsonIgnore]
        public bool IsHdr => Dynamic_range?.StartsWith("HDR") ?? false;
        [JsonPropertyName("url")]
        public string Url { get; set; }
    }

    public class VideoChannel {
        public string Name { get; set; }
        public string Url { get; set; }
    }
    
    public enum Protocol {
        mhtml,
        m3u8_native,
        https,
        http,
        http_dash_segments,
        websocket_frag,
        mms,
        f4f
    }
}