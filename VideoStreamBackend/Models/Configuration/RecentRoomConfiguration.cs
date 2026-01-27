using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace VideoStreamBackend.Models.Configuration;

public class RecentRoomConfiguration : IEntityTypeConfiguration<RecentRoom> {
    public void Configure(EntityTypeBuilder<RecentRoom> builder) {
        builder.ToTable("RecentRooms");
        builder.HasKey(r => new { r.RoomId, r.UserId });
        builder.Property(r => r.UserId).HasMaxLength(36).IsRequired();
        
        builder.HasOne<Room>(r => r.Room).WithMany().HasForeignKey(r => r.RoomId);
        builder.HasOne<ApplicationUser>(r =>  r.User).WithMany().HasForeignKey(r => r.UserId);
    }
}