using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using VideoStreamBackend.Identity;
using VideoStreamBackend.Models;

namespace VideoStreamBackend.Services;

public class RecentRoomService(ApplicationDbContext context) : IRecentRoomService {
    private readonly DbSet<RecentRoom> _recentRooms = context.Set<RecentRoom>();
    
    public async Task Add(RecentRoom recentRoom) {
        await _recentRooms.AddAsync(recentRoom);
    }

    public IEnumerable<T> GetMultiple<T>(Expression<Func<RecentRoom, T>> cast, Expression<Func<RecentRoom, bool>>? selector = null) {
        var recentRooms = _recentRooms.AsQueryable();
        
        if (selector != null) {
            recentRooms = recentRooms.Where(selector);
        }
        
        return recentRooms.Select(cast);
    }
}