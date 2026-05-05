using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruig.Domain.Entities;
using Ruig.Infrastructure.Strava;

namespace Ruig.Infrastructure.Common.Persistance.Configurations
{
    public sealed class StravaTokenConfiguration : IEntityTypeConfiguration<StravaToken>
    {
        public void Configure(EntityTypeBuilder<StravaToken> builder)
        {
            builder.ToTable("StravaTokens");

            builder.HasKey(t => t.Id);

            builder.Property(t => t.Id)
                .ValueGeneratedNever();

            builder.Property(t => t.AthleteId)
                .IsRequired();

            builder.HasIndex(t => t.AthleteId)
                .IsUnique();

            builder.HasOne<Athlete>()
                .WithMany()
                .HasForeignKey(t => t.AthleteId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Property(t => t.StravaAthleteId)
                .IsRequired();

            builder.HasIndex(t => t.StravaAthleteId)
                .IsUnique();

            builder.Property(t => t.AccessToken)
                .IsRequired()
                .HasColumnType("text");

            builder.Property(t => t.RefreshToken)
                .IsRequired()
                .HasColumnType("text");

            builder.Property(t => t.ExpiresAtUtc)
                .IsRequired();

            builder.Property(t => t.Scope)
                .HasMaxLength(500);
        }
    }
}
