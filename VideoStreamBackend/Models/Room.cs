namespace VideoStreamBackend.Models;

public class Room : GuidPrimaryKey {
    public string StringifiedId => Id.ToString();
    public virtual ApplicationUser Owner { get; set; }
    public string Name { get; set; }
    public virtual ICollection<QueueItem> Queue { get; set; } = new List<QueueItem>();
    public Status Status { get; set; }
    public double CurrentTime { get; set; }
}

public enum Status {
    Playing = 1,
    Paused = 2
}