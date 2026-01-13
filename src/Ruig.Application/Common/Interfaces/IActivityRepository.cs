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
        Task<PagedResult<ListActivitiesByAthleteDto>> ListByAthleteIdAsync(Guid AthleteId, int Page, int PageSize, DateTime? FromUtc, DateTime? ToUtc, CancellationToken cancellationToken);
        Task<bool> AddAsync(Activity activity, CancellationToken cancellationToken);
        Task SaveChangesAsync(CancellationToken cancellationToken);
    }
}
