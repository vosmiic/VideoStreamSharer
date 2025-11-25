namespace VideoStreamBackend.Models.ApiModels;

public class QueueAdd {
    public string Url { get; set; }
    public string VideoFormatId { get; set; }
    public string? AudioFormatId { get; set; }
}