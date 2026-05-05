using System;
using System.Collections.Generic;
using System.Text;

namespace Ruig.Application.Common.Interfaces.Strava
{
    public interface IStravaOAuthStateStore
    {
        Task StoreAsync(string state, TimeSpan ttl, CancellationToken cancellationToken);
        Task<bool> ConsumeAsync(string state, CancellationToken cancellationToken);
    }
}
