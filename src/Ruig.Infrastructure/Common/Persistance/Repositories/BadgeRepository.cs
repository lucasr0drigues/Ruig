using Microsoft.EntityFrameworkCore;
using Ruig.Application.Common.Interfaces;
using Ruig.Domain.Entities;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Ruig.Infrastructure.Common.Persistance.Repositories
{
    public sealed class BadgeRepository : IBadgeRepository
    {
        private readonly AppDbContext _dbContext;

        public BadgeRepository(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public Task<Badge?> GetBySlugAsync(string slug, CancellationToken cancellationToken)
        {
            return _dbContext.Badges
                .FirstOrDefaultAsync(b => b.Slug == slug, cancellationToken);
        }

        public Task<Badge?> GetByAthleteIdAsync(Guid athleteId, CancellationToken cancellationToken)
        {
            return _dbContext.Badges
                .FirstOrDefaultAsync(b => b.AthleteId == athleteId, cancellationToken);
        }

        public Task<bool> SlugExistsAsync(string slug, CancellationToken cancellationToken)
        {
            return _dbContext.Badges.AnyAsync(b => b.Slug == slug, cancellationToken);
        }

        public async Task AddAsync(Badge badge, CancellationToken cancellationToken)
        {
            await _dbContext.Badges.AddAsync(badge, cancellationToken);
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            return _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
