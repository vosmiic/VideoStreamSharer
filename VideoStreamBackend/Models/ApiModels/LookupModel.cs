namespace VideoStreamBackend.Models.ApiModels;

public class LookupModel {
    public IEnumerable<LookupFormats> VideoFormats { get; set; }
    public IEnumerable<LookupFormats> AudioFormats { get; set; }
    public string ThumbnailUrl { get; set; }
    public string Title { get; set; }
    public string ChannelTitle { get; set; }
    public string Viewcount { get; set; }
    public string Duration { get; set; }
    
    public class LookupFormats {
        public string Id { get; set; }
        public string Value { get; set; }
    }
}