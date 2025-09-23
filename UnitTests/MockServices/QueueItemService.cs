using VideoStreamBackend.Models;
using VideoStreamBackend.Services;

namespace UnitTests.MockServices;

public class QueueItemService : IQueueItemService {
    public List<QueueItem> QueueItems { get; set; } = new ();
    
    public Task SaveChanges() {
        return Task.CompletedTask;
    }

    public void BulkAddOrUpdate(ICollection<QueueItem> queueItems) {
        QueueItems.AddRange(queueItems);
    }

    public Task<QueueItem?> GetQueueItemById(Guid id) {
        throw new NotImplementedException();
    }

    public Task Remove(QueueItem queueItem) {
        return Task.FromResult(QueueItems.Remove(queueItem));
    }
}