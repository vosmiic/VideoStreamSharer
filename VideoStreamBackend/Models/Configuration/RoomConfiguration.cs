using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace VideoStreamBackend.Models.Configuration;

public class RoomConfiguration : IEntityTypeConfiguration<Room> {
    public void Configure(EntityTypeBuilder<Room> builder) {
        builder.HasOne<IdentityUser>(r => r.Owner).WithMany().HasForeignKey(r => r.UserId).IsRequired();
    }
}