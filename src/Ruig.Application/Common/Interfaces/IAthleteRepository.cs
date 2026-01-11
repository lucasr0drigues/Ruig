using Ruig.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ruig.Application.Common.Interfaces
{
    public interface IAthleteRepository
    {
        Task<Activity?> GetByIdAsync(Guid athleteId,  CancellationToken cancellationToken);
        Task<bool> Exists(Guid athleteId, CancellationToken cancellationToken);
        Task AddAsync(Athlete athlete, CancellationToken cancellationToken);
        Task SaveChangesAsync(CancellationToken cancellationToken);
    }
}
