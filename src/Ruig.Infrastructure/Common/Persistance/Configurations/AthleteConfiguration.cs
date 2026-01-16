using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruig.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

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

            builder.Property(a => a.ExternalAthleteId)
                .IsRequired()
                .HasMaxLength(100);

            builder.HasIndex(a => a.ExternalAthleteId)
                .IsUnique();

            builder.Property(a => a.Username).HasMaxLength(100);
            builder.Property(a => a.Firstname).HasMaxLength(100);
            builder.Property(a => a.Lastname).HasMaxLength(100);

            builder.Property(a => a.Bio).HasMaxLength(2000);

            builder.Property(a => a.City).HasMaxLength(100);
            builder.Property(a => a.State).HasMaxLength(100);
            builder.Property(a => a.Country).HasMaxLength(100);

            builder.Property(a => a.Sex)
                .HasConversion<int?>();

            builder.Property(a => a.ExternalCreatedAt)
                .IsRequired();

            builder.Property(a => a.ExternalUpdatedAt);

            builder.Property(a => a.ProfileMedium)
                .HasMaxLength(500);

            builder.Property(a => a.Profile)
                .HasMaxLength(500);

            builder.Property(a => a.CreatedAt)
                .IsRequired();

            builder.Property(a => a.LastUpdatedAt)
                .IsRequired();
        }
    }
}
