using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace VideoStreamBackend.Models.Configuration;

public class RoomConfiguration : IEntityTypeConfiguration<Room> {
    public void Configure(EntityTypeBuilder<Room> builder) {
        builder.HasOne<ApplicationUser>(r => r.Owner).WithMany();
        builder
            .HasMany(r => r.Queue)
            .WithOne(q => q.Room)
            .IsRequired()
            .HasForeignKey(item => item.RoomId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}