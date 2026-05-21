using Microsoft.EntityFrameworkCore;
using Ruig.Application.Activities.Commands.ListActivitiesByAthlete;
using Ruig.Application.Common.Interfaces;
using Ruig.Application.Common.Models;
using Ruig.Domain.Entities;

namespace Ruig.Infrastructure.Common.Persistance.Repositories
{
    public sealed class ActivityRepository : IActivityRepository
    {
        private readonly AppDbContext _dbContext;

        public ActivityRepository(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public Task<Activity?> GetByIdAsync(Guid activityId, CancellationToken cancellationToken)
        {
            return _dbContext.Activities
                .FirstOrDefaultAsync(a => a.Id == activityId, cancellationToken);
        }

        public async Task<PagedResult<ListActivitiesByAthleteDto>> ListByAthleteIdAsync(
            Guid athleteId,
            int page,
            int pageSize,
            DateTime? fromUtc,
            DateTime? toUtc,
            CancellationToken cancellationToken)
        {
            var query = _dbContext.Activities
                .AsNoTracking()
                .Where(a => a.AthleteId == athleteId);

            if (fromUtc is not null)
            {
                var from = DateOnly.FromDateTime(fromUtc.Value);
                query = query.Where(a => a.LocalDate >= from);
            }

            if (toUtc is not null)
            {
                var to = DateOnly.FromDateTime(toUtc.Value);
                query = query.Where(a => a.LocalDate <= to);
            }

            var totalCount = await query.CountAsync(cancellationToken);

            var items = await query
                .OrderByDescending(a => a.LocalDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(a => new ListActivitiesByAthleteDto(
                    a.Id,
                    a.AthleteId,
                    a.LocalDate))
                .ToListAsync(cancellationToken);

            return new PagedResult<ListActivitiesByAthleteDto>(page, pageSize, totalCount, items);
        }

        public async Task<IReadOnlyList<DateOnly>> GetActiveLocalDatesAsync(
            Guid athleteId,
            DateOnly fromInclusive,
            DateOnly toInclusive,
            CancellationToken cancellationToken)
        {
            return await _dbContext.Activities
                .AsNoTracking()
                .Where(a => a.AthleteId == athleteId
                    && a.LocalDate >= fromInclusive
                    && a.LocalDate <= toInclusive)
                .Select(a => a.LocalDate)
                .Distinct()
                .ToListAsync(cancellationToken);
        }

        public async Task<bool> AddAsync(Activity activity, CancellationToken cancellationToken)
        {
            await _dbContext.Activities.AddAsync(activity, cancellationToken);
            await SaveChangesAsync(cancellationToken);

            return true;
        }

        public async Task UpsertAsync(Activity activity, CancellationToken cancellationToken)
        {
            var exists = await _dbContext.Activities.AnyAsync(
                a => a.AthleteId == activity.AthleteId && a.LocalDate == activity.LocalDate,
                cancellationToken);

            if (!exists)
                await _dbContext.Activities.AddAsync(activity, cancellationToken);
        }

        public async Task ReplaceLocalDatesAsync(
            Guid athleteId,
            DateOnly fromInclusive,
            DateOnly toInclusive,
            IEnumerable<DateOnly> localDates,
            CancellationToken cancellationToken)
        {
            var replacementDates = localDates
                .Where(d => d >= fromInclusive && d <= toInclusive)
                .Distinct()
                .ToHashSet();

            var existing = await _dbContext.Activities
                .Where(a => a.AthleteId == athleteId
                    && a.LocalDate >= fromInclusive
                    && a.LocalDate <= toInclusive)
                .ToListAsync(cancellationToken);

            _dbContext.Activities.RemoveRange(existing.Where(a => !replacementDates.Contains(a.LocalDate)));

            var existingDates = existing.Select(a => a.LocalDate).ToHashSet();
            var newActivities = replacementDates
                .Where(d => !existingDates.Contains(d))
                .Select(d => new Activity(athleteId, d));

            await _dbContext.Activities.AddRangeAsync(newActivities, cancellationToken);
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            return _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
