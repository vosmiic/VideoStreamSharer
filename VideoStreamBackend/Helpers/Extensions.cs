using VideoStreamBackend.Models;

namespace VideoStreamBackend.Helpers;

public static class Extensions {
    public static QueueItem? CurrentVideo(this Room room) => room?.Queue.MinBy(queue => queue.Order);
}