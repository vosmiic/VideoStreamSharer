using System.Linq.Expressions;
using VideoStreamBackend.Models;
using VideoStreamBackend.Services;

namespace UnitTests.MockServices;

public class RoomService : IRoomService {
    public Task SaveChanges() {
        return Task.CompletedTask;
    }

    public void Detach(Room room) {
        throw new NotImplementedException();
    }

    public Task<Room?> GetRoomById(Guid id) {
        throw new NotImplementedException();
    }

    public IEnumerable<T> GetMultiple<T>(Expression<Func<Room, T>> cast, Expression<Func<Room, bool>>? selector = null) {
        throw new NotImplementedException();
    }
}