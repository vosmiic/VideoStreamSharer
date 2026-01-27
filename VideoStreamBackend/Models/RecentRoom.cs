namespace VideoStreamBackend.Models;

public class RecentRoom {
    public virtual ApplicationUser User { get; init; }
    public required string UserId { get; set; }
    public virtual Room Room { get; init; }
    public Guid RoomId { get; set; }
    public required DateTime VisitDateTime { get; set; }
}