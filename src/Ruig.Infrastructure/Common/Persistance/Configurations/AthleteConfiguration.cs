using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruig.Domain.Entities;

namespace Ruig.Infrastructure.Common.Persistance.Configurations
{
    public sealed class AthleteConfiguration : IEntityTypeConfiguration<Athlete>
    {
        public void Configure(EntityTypeBuilder<Athlete> builder)
        {
            builder.ToTable("Athletes");

            builder.HasKey(a => a.Id);

            builder.Property(a => a.Id)
                .ValueGeneratedNever();

            builder.Property(a => a.Firstname)
                .HasMaxLength(100);

            builder.Property(a => a.Lastname)
                .HasMaxLength(100);

            builder.Property(a => a.CreatedAt)
                .IsRequired();

            builder.Property(a => a.LastUpdatedAt)
                .IsRequired();

            builder.Property(a => a.LastActivitySyncedAtUtc);
        }
    }
}
