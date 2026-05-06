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

        public Task<Activity?> GetByExternalIdAsync(Guid athleteId, string externalActivityId, CancellationToken cancellationToken)
        {
            return _dbContext.Activities
                .FirstOrDefaultAsync(
                    a => a.AthleteId == athleteId && a.ExternalActivityId == externalActivityId,
                    cancellationToken);
        }

        public async Task<PagedResult<ListActivitiesByAthleteDto>> ListByAthleteIdAsync(
            Guid AthleteId,
            int Page,
            int PageSize,
            DateTime? FromUtc,
            DateTime? ToUtc,
            CancellationToken cancellationToken)
        {
            var query = _dbContext.Activities
                .AsNoTracking()
                .Where(a => a.AthleteId == AthleteId && a.DeletedAtUtc == null);

            if (FromUtc is not null)
            {
                var from = new DateTimeOffset(DateTime.SpecifyKind(FromUtc.Value, DateTimeKind.Utc));
                query = query.Where(a => a.StartedAtUtc >= from);
            }

            if (ToUtc is not null)
            {
                var to = new DateTimeOffset(DateTime.SpecifyKind(ToUtc.Value, DateTimeKind.Utc));
                query = query.Where(a => a.StartedAtUtc <= to);
            }

            var totalCount = await query.CountAsync(cancellationToken);

            var items = await query
                .OrderByDescending(a => a.StartedAtUtc)
                .Skip((Page - 1) * PageSize)
                .Take(PageSize)
                .Select(a => new ListActivitiesByAthleteDto(
                    a.Id,
                    a.AthleteId,
                    a.ExternalActivityId,
                    a.Name,
                    a.Sport,
                    a.StartedAtUtc,
                    a.UtcOffsetAtStart,
                    a.DeviceName))
                .ToListAsync(cancellationToken);

            return new PagedResult<ListActivitiesByAthleteDto>(Page, PageSize, totalCount, items);
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
                    && a.DeletedAtUtc == null
                    && a.LocalDate != null
                    && a.LocalDate >= fromInclusive
                    && a.LocalDate <= toInclusive)
                .Select(a => a.LocalDate!.Value)
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
            var existing = await GetByExternalIdAsync(activity.AthleteId, activity.ExternalActivityId, cancellationToken);

            if (existing is null)
            {
                await _dbContext.Activities.AddAsync(activity, cancellationToken);
            }
            else
            {
                existing.UpdateFromExternal(activity);
            }
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            return _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
