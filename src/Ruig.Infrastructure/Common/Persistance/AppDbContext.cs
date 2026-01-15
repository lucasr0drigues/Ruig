using Microsoft.EntityFrameworkCore;
using Ruig.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ruig.Infrastructure.Common.Persistance
{
    public sealed class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
            
        }

        public DbSet<Athlete> Athletes => Set<Athlete>();
        public DbSet<Activity>  Activities => Set<Activity>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        }
    }
}
