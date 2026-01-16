using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruig.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

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

            builder.Property(a => a.ExternalActivityId)
                .IsRequired()
                .HasMaxLength(100);

            builder.HasIndex(a => new { a.AthleteId, a.ExternalActivityId })
                .IsUnique();

            builder.Property(a => a.Name)
                .HasMaxLength(200);

            builder.Property(a => a.Sport)
                .HasConversion<int?>();

            builder.Property(a => a.DistanceMeters);
            builder.Property(a => a.MovingTimeSeconds);
            builder.Property(a => a.ElapsedTimeSeconds);
            builder.Property(a => a.TotalElevationGainMeters);

            builder.Property(a => a.StartedAtUtc);
            builder.HasIndex(a => a.StartedAtUtc);

            builder.Property(a => a.UtcOffsetAtStart);

            builder.Property(a => a.DeviceName)
                .HasMaxLength(200);

            builder.OwnsOne(a => a.Map, map =>
            {
                map.Property(m => m.ExternalMapId)
                    .HasColumnName("map_external_map_id")
                    .HasMaxLength(100);

                map.Property(m => m.SummaryPolyline)
                    .HasColumnName("map_summary_polyline")
                    .HasColumnType("text");

                map.WithOwner();

                map.UsePropertyAccessMode(PropertyAccessMode.Property);
            });

            builder.Navigation(a => a.Map)
                .IsRequired(false);
        }
    }
}
