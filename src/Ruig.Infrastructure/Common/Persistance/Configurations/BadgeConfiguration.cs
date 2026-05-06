using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruig.Domain.Entities;

namespace Ruig.Infrastructure.Common.Persistance.Configurations
{
    public sealed class BadgeConfiguration : IEntityTypeConfiguration<Badge>
    {
        public void Configure(EntityTypeBuilder<Badge> builder)
        {
            builder.ToTable("Badges");

            builder.HasKey(b => b.Id);

            builder.Property(b => b.Id)
                .ValueGeneratedNever();

            builder.Property(b => b.Slug)
                .IsRequired()
                .HasMaxLength(64);

            builder.HasIndex(b => b.Slug)
                .IsUnique();

            builder.Property(b => b.AthleteId)
                .IsRequired();

            builder.HasIndex(b => b.AthleteId)
                .IsUnique();

            builder.HasOne<Athlete>()
                .WithMany()
                .HasForeignKey(b => b.AthleteId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Property(b => b.GitHubUsername)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(b => b.IsEnabled)
                .IsRequired();

            builder.Property(b => b.CreatedAt)
                .IsRequired();

            builder.Property(b => b.LastUpdatedAt)
                .IsRequired();
        }
    }
}
