using Ruig.Application.Activities.Commands.ListActivitiesByAthlete;
using Ruig.Application.Common.Models;
using Ruig.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ruig.Application.Common.Interfaces
{
    public interface IActivityRepository
    {
        Task<Activity?> GetByIdAsync(Guid activityId, CancellationToken cancellationToken);
        Task<PagedResult<ListActivitiesByAthleteDto>> ListByAthleteIdAsync(Guid athleteId, int page, int pageSize, DateTime? fromUtc, DateTime? toUtc, CancellationToken cancellationToken);
        Task<IReadOnlyList<DateOnly>> GetActiveLocalDatesAsync(Guid athleteId, DateOnly fromInclusive, DateOnly toInclusive, CancellationToken cancellationToken);
        Task<bool> AddAsync(Activity activity, CancellationToken cancellationToken);
        Task UpsertAsync(Activity activity, CancellationToken cancellationToken);
        Task ReplaceLocalDatesAsync(Guid athleteId, DateOnly fromInclusive, DateOnly toInclusive, IEnumerable<DateOnly> localDates, CancellationToken cancellationToken);
        Task SaveChangesAsync(CancellationToken cancellationToken);
    }
}
