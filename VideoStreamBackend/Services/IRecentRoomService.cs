using System.Linq.Expressions;
using VideoStreamBackend.Models;

namespace VideoStreamBackend.Services;

public interface IRecentRoomService {
    Task Add(RecentRoom recentRoom);
    IEnumerable<T> GetMultiple<T>(Expression<Func<RecentRoom, T>> cast, Expression<Func<RecentRoom, bool>>? selector = null);
}