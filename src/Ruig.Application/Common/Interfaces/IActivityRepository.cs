using Ruig.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ruig.Application.Common.Interfaces
{
    public interface IActivityRepository
    {
        Task<Activity?> GetByIdAsync(Guid activityId, CancellationToken cancellationToken);
        Task<List<Activity?>> ListByAthleteIdAsync(Guid athleteId, CancellationToken cancellationToken);
        Task<bool> AddAsync(Activity activity, CancellationToken cancellationToken);
        Task SaveChangesAsync(CancellationToken cancellationToken);
    }
}
