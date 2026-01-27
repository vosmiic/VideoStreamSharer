using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using VideoStreamBackend.Models;
using VideoStreamBackend.Models.Configuration;

namespace VideoStreamBackend.Identity;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser> {
    public DbSet<Room> Rooms { get; set; }
    public DbSet<QueueItem> QueueItems { get; set; }
    
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) :
        base(options)
    { }

    protected override void OnModelCreating(ModelBuilder builder) {
        new RoomConfiguration().Configure(builder.Entity<Room>());
        new QueueItemConfiguration().Configure(builder.Entity<QueueItem>());
        new RecentRoomConfiguration().Configure(builder.Entity<RecentRoom>());
        base.OnModelCreating(builder);
    }
}