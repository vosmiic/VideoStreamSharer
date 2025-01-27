using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VideoStreamBackend.Models.PlayableType;

namespace VideoStreamBackend.Models.Configuration;

public class QueueItemConfiguration : IEntityTypeConfiguration<QueueItem> {
    public void Configure(EntityTypeBuilder<QueueItem> builder) {
        builder.HasDiscriminator<string>("Type")
            .HasValue<YouTubeVideo>(nameof(YouTubeVideo))
            .HasValue<UploadedMedia>(nameof(UploadedMedia));
    }
}