using Microsoft.EntityFrameworkCore;
using Ruig.Domain.Common;
using Ruig.Domain.Entities;
using Ruig.Infrastructure.Strava;

namespace Ruig.Infrastructure.Common.Persistance
{
    public sealed class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Athlete> Athletes => Set<Athlete>();
        public DbSet<Activity> Activities => Set<Activity>();
        public DbSet<Badge> Badges => Set<Badge>();
        public DbSet<StravaToken> StravaTokens => Set<StravaToken>();
        public DbSet<StravaWebhookEvent> StravaWebhookEvents => Set<StravaWebhookEvent>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var now = DateTimeOffset.UtcNow;

            foreach (var entry in ChangeTracker.Entries<BaseAuditableEntity>())
            {
                if (entry.State == EntityState.Added)
                {
                    entry.Entity.CreatedAt = now;
                    entry.Entity.LastUpdatedAt = now;
                }
                else if (entry.State == EntityState.Modified)
                {
                    entry.Entity.LastUpdatedAt = now;
                }
            }

            return base.SaveChangesAsync(cancellationToken);
        }
    }
}
