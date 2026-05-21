using System;
using System.Collections.Generic;
using System.Text;

namespace Ruig.Application.Common.Interfaces.Strava
{
    public interface IStravaActivitySyncService
    {
        Task InitialBackfillAsync(Guid athleteId, CancellationToken cancellationToken);
        Task SyncRecentActivitiesAsync(Guid athleteId, TimeSpan lookback, CancellationToken cancellationToken);
        Task SyncActivityAsync(Guid athleteId, long externalActivityId, CancellationToken cancellationToken);
    }
}
