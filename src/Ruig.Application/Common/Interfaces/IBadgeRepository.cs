using Ruig.Domain.Entities;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Ruig.Application.Common.Interfaces
{
    public interface IBadgeRepository
    {
        Task<Badge?> GetBySlugAsync(string slug, CancellationToken cancellationToken);
        Task<Badge?> GetByAthleteIdAsync(Guid athleteId, CancellationToken cancellationToken);
        Task<bool> SlugExistsAsync(string slug, CancellationToken cancellationToken);
        Task AddAsync(Badge badge, CancellationToken cancellationToken);
        Task SaveChangesAsync(CancellationToken cancellationToken);
    }
}
