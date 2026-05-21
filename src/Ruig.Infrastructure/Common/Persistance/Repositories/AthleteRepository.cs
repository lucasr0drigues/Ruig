using Microsoft.EntityFrameworkCore;
using Ruig.Application.Common.Interfaces;
using Ruig.Domain.Entities;

namespace Ruig.Infrastructure.Common.Persistance.Repositories
{
    public sealed class AthleteRepository : IAthleteRepository
    {
        private readonly AppDbContext _dbContext;

        public AthleteRepository(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public Task<Athlete?> GetByIdAsync(Guid athleteId, CancellationToken cancellationToken)
        {
            return _dbContext.Athletes
                .FirstOrDefaultAsync(a => a.Id == athleteId, cancellationToken);
        }

        public Task<bool> Exists(Guid athleteId, CancellationToken cancellationToken)
        {
            return _dbContext.Athletes
                .AnyAsync(a => a.Id == athleteId, cancellationToken);
        }

        public async Task AddAsync(Athlete athlete, CancellationToken cancellationToken)
        {
            await _dbContext.Athletes.AddAsync(athlete, cancellationToken);
            await SaveChangesAsync(cancellationToken);
        }

        public async Task UpdateFromExternalAsync(Guid athleteId, Athlete externalAthlete, CancellationToken cancellationToken)
        {
            var existing = await GetByIdAsync(athleteId, cancellationToken);

            if (existing is null)
                throw new KeyNotFoundException($"Athlete '{athleteId}' was not found.");

            existing.UpdateFromExternal(externalAthlete);
            await SaveChangesAsync(cancellationToken);
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            return _dbContext.SaveChangesAsync(cancellationToken);
        }

        public async Task MarkActivitySyncCompletedAsync(Guid athleteId, DateTimeOffset syncedAtUtc, CancellationToken cancellationToken)
        {
            var athlete = await GetByIdAsync(athleteId, cancellationToken);

            if (athlete is null)
                throw new KeyNotFoundException($"Athlete '{athleteId}' was not found.");

            athlete.MarkActivitySyncCompleted(syncedAtUtc);
            await SaveChangesAsync(cancellationToken);
        }
    }
}
