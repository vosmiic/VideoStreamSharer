using System.Linq.Expressions;
using VideoStreamBackend.Models;

namespace VideoStreamBackend.Services;

public interface IRoomService {
    public Task SaveChanges();
    public void Detach(Room room);
    public Task<Room?> GetRoomById(Guid id);
    public IEnumerable<T> GetMultiple<T>(Expression<Func<Room, T>> cast, Expression<Func<Room, bool>>? selector = null);
}