using Ruig.Application.Common.Interfaces.Strava;

namespace Ruig.Infrastructure.Strava
{
    public sealed class StravaActivityReconciliationService
    {
        private static readonly TimeSpan RecentLookback = TimeSpan.FromDays(30);

        private readonly IStravaTokenStore _tokenStore;
        private readonly IStravaActivitySyncService _activitySyncService;

        public StravaActivityReconciliationService(
            IStravaTokenStore tokenStore,
            IStravaActivitySyncService activitySyncService)
        {
            _tokenStore = tokenStore;
            _activitySyncService = activitySyncService;
        }

        public async Task<int> ReconcileAsync(CancellationToken cancellationToken)
        {
            var athleteIds = await _tokenStore.ListActiveAthleteIdsAsync(cancellationToken);

            foreach (var athleteId in athleteIds)
            {
                await _activitySyncService.SyncRecentActivitiesAsync(athleteId, RecentLookback, cancellationToken);
            }

            return athleteIds.Count;
        }
    }
}
