using Microsoft.EntityFrameworkCore;
using VideoStreamBackend.Identity;
using VideoStreamBackend.Models;

namespace VideoStreamBackend.Services;

public class QueueItemService (ApplicationDbContext context) : IQueueItemService {
    private readonly DbSet<QueueItem> _queueItems = context.Set<QueueItem>();

    public async Task SaveChanges() => await context.SaveChangesAsync();
    
    public void BulkAddOrUpdate(ICollection<QueueItem> queueItems) {
        _queueItems.UpdateRange(queueItems);
    }

    public Task<QueueItem?> GetQueueItemById(Guid id) => _queueItems.FirstOrDefaultAsync(queueItem => queueItem.Id == id);

    public async Task Remove(QueueItem queueItem) {
        _queueItems.Remove(queueItem);
        await context.SaveChangesAsync();
    }
}