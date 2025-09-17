using VideoStreamBackend.Models;

namespace VideoStreamBackend.Services;

public interface IQueueItemService {
    public Task SaveChanges();
    public void BulkAddOrUpdate(ICollection<QueueItem> queueItems);
    public Task<QueueItem?> GetQueueItemById(Guid id);
    public Task Remove(QueueItem queueItem);
}