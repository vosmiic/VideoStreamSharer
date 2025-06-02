namespace VideoStreamBackend.Models;

public class Room : GuidPrimaryKey {
    public virtual ApplicationUser Owner { get; set; }
    public string Name { get; set; }
    public virtual ICollection<QueueItem> Queue { get; set; } = new List<QueueItem>();
    public Status Status { get; set; }
}

public enum Status {
    Playing = 1,
    Paused = 2
}