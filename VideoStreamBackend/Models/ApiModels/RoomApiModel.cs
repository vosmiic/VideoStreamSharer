using VideoStreamBackend.Models.YtDlp;

namespace VideoStreamBackend.Models.ApiModels;

public class RoomApiModel() : GuidPrimaryKey {
    public string Name { get; set; }
    public IEnumerable<QueueItemApiModel> Queue { get; set; }
    public IEnumerable<StreamUrl>? StreamUrls { get; set; }
}