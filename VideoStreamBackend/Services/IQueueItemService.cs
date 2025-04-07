using VideoStreamBackend.Models;

namespace VideoStreamBackend.Services;

public interface IQueueItemService {
    public Task SaveChanges();
    public void BulkAddOrUpdate(ICollection<QueueItem> queueItems);
}