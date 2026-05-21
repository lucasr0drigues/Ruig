using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruig.Domain.Entities;

namespace Ruig.Infrastructure.Common.Persistance.Configurations
{
    public sealed class ActivityConfiguration : IEntityTypeConfiguration<Activity>
    {
        public void Configure(EntityTypeBuilder<Activity> builder)
        {
            builder.ToTable("Activities");

            builder.HasKey(a => a.Id);

            builder.Property(a => a.Id)
                .ValueGeneratedNever();

            builder.Property(a => a.AthleteId)
                .IsRequired();

            builder.HasIndex(a => a.AthleteId);

            builder.HasOne<Athlete>()
                .WithMany()
                .HasForeignKey(a => a.AthleteId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Property(a => a.LocalDate)
                .IsRequired();

            builder.HasIndex(a => a.LocalDate);

            builder.HasIndex(a => new { a.AthleteId, a.LocalDate })
                .IsUnique();
        }
    }
}
