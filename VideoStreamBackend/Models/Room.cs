namespace VideoStreamBackend.Models;

public class Room : GuidPrimaryKey {
    public virtual ApplicationUser Owner { get; set; }
    public string Name { get; set; }
    public ICollection<QueueItem> Queue { get; set; } = new List<QueueItem>();
}