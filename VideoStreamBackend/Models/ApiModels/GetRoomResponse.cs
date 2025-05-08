namespace VideoStreamBackend.Models.ApiModels;

public class GetRoomResponse {
    public RoomApiModel Room { get; set; }
    public IEnumerable<string> Users { get; set; }
}