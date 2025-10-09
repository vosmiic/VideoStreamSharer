using Microsoft.EntityFrameworkCore.ChangeTracking;
using VideoStreamBackend.Models;

namespace VideoStreamBackend.Services;

public interface IRoomService {
    public Task SaveChanges();
    public void Detach(Room room);
    public Task<Room?> GetRoomById(Guid id);
}