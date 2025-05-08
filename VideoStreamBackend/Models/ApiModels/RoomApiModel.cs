namespace VideoStreamBackend.Models.ApiModels;

public class RoomApiModel() : GuidPrimaryKey {
    public string Name { get; set; }
    public IEnumerable<QueueItemApiModel> Queue { get; set; }
}