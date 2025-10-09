using Microsoft.EntityFrameworkCore;
using VideoStreamBackend.Identity;
using VideoStreamBackend.Models;

namespace VideoStreamBackend.Services;

public class RoomService (ApplicationDbContext context) : IRoomService {
    private readonly DbSet<Room> _rooms = context.Set<Room>();

    public async Task SaveChanges() => await context.SaveChangesAsync();
    
    public void Detach(Room room) => context.Entry(room).State = EntityState.Detached;
    
    public async Task<Room?> GetRoomById(Guid id) =>
        await _rooms.Include(room => room.Queue).FirstOrDefaultAsync(room => room.Id.CompareTo(id) == 0);
}