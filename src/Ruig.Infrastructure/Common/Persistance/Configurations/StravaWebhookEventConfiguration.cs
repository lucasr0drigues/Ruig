using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruig.Infrastructure.Strava;

namespace Ruig.Infrastructure.Common.Persistance.Configurations
{
    public sealed class StravaWebhookEventConfiguration : IEntityTypeConfiguration<StravaWebhookEvent>
    {
        public void Configure(EntityTypeBuilder<StravaWebhookEvent> builder)
        {
            builder.ToTable("StravaWebhookEvents");

            builder.HasKey(e => e.Id);

            builder.Property(e => e.Id)
                .ValueGeneratedNever();

            builder.Property(e => e.ObjectType)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(e => e.AspectType)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(e => e.UpdatesJson)
                .IsRequired()
                .HasColumnType("jsonb");

            builder.Property(e => e.EventTimeUtc)
                .IsRequired();

            builder.Property(e => e.ReceivedAtUtc)
                .IsRequired();

            builder.Property(e => e.ProcessingError)
                .HasColumnType("text");

            builder.HasIndex(e => new { e.ObjectType, e.ObjectId, e.AspectType, e.EventTimeUtc })
                .IsUnique();

            builder.HasIndex(e => e.ProcessedAtUtc);
            builder.HasIndex(e => e.OwnerId);
        }
    }
}
