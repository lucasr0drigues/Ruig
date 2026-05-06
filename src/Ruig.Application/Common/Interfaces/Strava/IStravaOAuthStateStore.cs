using System;
using System.Threading;
using System.Threading.Tasks;

namespace Ruig.Application.Common.Interfaces.Strava
{
    public interface IStravaOAuthStateStore
    {
        Task StoreAsync(string state, StravaOAuthStateData data, TimeSpan ttl, CancellationToken cancellationToken);
        Task<StravaOAuthStateData?> ConsumeAsync(string state, CancellationToken cancellationToken);
    }
}
