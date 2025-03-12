namespace VideoStreamBackend.Models.ApiModels;

public class GetRoomResponse {
    public Room Room { get; set; }
    public IEnumerable<string> Users { get; set; }
}