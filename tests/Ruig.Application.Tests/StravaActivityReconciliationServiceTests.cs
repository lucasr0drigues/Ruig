using Ruig.Application.Common.Interfaces.Strava;
using Ruig.Application.Common.Interfaces.Strava.Models;
using Ruig.Infrastructure.Strava;

namespace Ruig.Application.Tests;

public sealed class StravaActivityReconciliationServiceTests
{
    [Fact]
    public async Task ReconcileAsync_SyncsRecentActivitiesForActiveAthletes()
    {
        var firstAthleteId = Guid.NewGuid();
        var secondAthleteId = Guid.NewGuid();
        var tokenStore = new FakeTokenStore([firstAthleteId, secondAthleteId]);
        var activitySyncService = new FakeActivitySyncService();
        var reconciliationService = new StravaActivityReconciliationService(tokenStore, activitySyncService);

        var syncedCount = await reconciliationService.ReconcileAsync(CancellationToken.None);

        Assert.Equal(2, syncedCount);
        Assert.Collection(
            activitySyncService.SyncedAthletes,
            item =>
            {
                Assert.Equal(firstAthleteId, item.AthleteId);
                Assert.Equal(TimeSpan.FromDays(30), item.Lookback);
            },
            item =>
            {
                Assert.Equal(secondAthleteId, item.AthleteId);
                Assert.Equal(TimeSpan.FromDays(30), item.Lookback);
            });
    }

    private sealed class FakeTokenStore : IStravaTokenStore
    {
        private readonly IReadOnlyList<Guid> _athleteIds;

        public FakeTokenStore(IReadOnlyList<Guid> athleteIds)
        {
            _athleteIds = athleteIds;
        }

        public Task SaveOrUpdateAsync(
            Guid athleteId,
            long stravaAthleteId,
            string accessToken,
            string refreshToken,
            DateTimeOffset expiresAtUtc,
            string scope,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<string?> GetAccessTokenAsync(Guid athleteId, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task RevokeByStravaAthleteIdAsync(long stravaAthleteId, DateTimeOffset revokedAtUtc, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<IReadOnlyList<Guid>> ListActiveAthleteIdsAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(_athleteIds);
        }
    }

    private sealed class FakeActivitySyncService : IStravaActivitySyncService
    {
        public List<(Guid AthleteId, TimeSpan Lookback)> SyncedAthletes { get; } = new();

        public Task InitialBackfillAsync(Guid athleteId, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task SyncRecentActivitiesAsync(Guid athleteId, TimeSpan lookback, CancellationToken cancellationToken)
        {
            SyncedAthletes.Add((athleteId, lookback));
            return Task.CompletedTask;
        }

        public Task SyncActivityAsync(Guid athleteId, long externalActivityId, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task MarkActivityDeletedAsync(Guid athleteId, long externalActivityId, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }
    }
}
