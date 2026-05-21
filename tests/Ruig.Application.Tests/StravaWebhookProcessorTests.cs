using Microsoft.EntityFrameworkCore;
using Ruig.Application.Common.Interfaces;
using Ruig.Application.Common.Interfaces.Strava;
using Ruig.Application.Common.Interfaces.Strava.Models;
using Ruig.Domain.Entities;
using Ruig.Infrastructure.Common.Persistance;
using Ruig.Infrastructure.Strava;
using System.Text.Json;

namespace Ruig.Application.Tests;

public sealed class StravaWebhookProcessorTests
{
    private static readonly DateTime FixedUtcNow = new(2026, 5, 5, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task ProcessPendingAsync_ForActivityCreate_SyncsActivityAndMarksProcessed()
    {
        await using var dbContext = CreateDbContext();
        var athlete = CreateAthlete();
        dbContext.Athletes.Add(athlete);
        dbContext.StravaWebhookEvents.Add(CreateEvent("activity", "create", 987, 123));
        await dbContext.SaveChangesAsync();

        var activitySyncService = new FakeActivitySyncService();
        var processor = CreateProcessor(dbContext, activitySyncService);

        var processedCount = await processor.ProcessPendingAsync(CancellationToken.None);

        Assert.Equal(1, processedCount);
        Assert.Equal(athlete.Id, activitySyncService.SyncedAthleteId);
        Assert.Equal(987, activitySyncService.SyncedActivityId);
        Assert.NotNull(await dbContext.StravaWebhookEvents.Select(e => e.ProcessedAtUtc).SingleAsync());
    }

    [Fact]
    public async Task ProcessPendingAsync_ForActivityDelete_RebuildsStoredActivityDays()
    {
        await using var dbContext = CreateDbContext();
        var athlete = CreateAthlete();
        dbContext.Athletes.Add(athlete);
        dbContext.StravaWebhookEvents.Add(CreateEvent("activity", "delete", 987, 123));
        await dbContext.SaveChangesAsync();

        var activitySyncService = new FakeActivitySyncService();
        var processor = CreateProcessor(dbContext, activitySyncService);

        await processor.ProcessPendingAsync(CancellationToken.None);

        Assert.Equal(athlete.Id, activitySyncService.InitialBackfillAthleteId);
    }

    [Fact]
    public async Task ProcessPendingAsync_ForAthleteDeauthorization_RevokesToken()
    {
        await using var dbContext = CreateDbContext();
        dbContext.StravaWebhookEvents.Add(CreateEvent(
            "athlete",
            "update",
            123,
            123,
            new Dictionary<string, string> { ["authorized"] = "false" }));
        await dbContext.SaveChangesAsync();

        var tokenStore = new FakeTokenStore();
        var processor = CreateProcessor(dbContext, tokenStore: tokenStore);

        await processor.ProcessPendingAsync(CancellationToken.None);

        Assert.Equal(123, tokenStore.RevokedStravaAthleteId);
        Assert.Equal(new DateTimeOffset(FixedUtcNow), tokenStore.RevokedAtUtc);
    }

    private static StravaWebhookProcessor CreateProcessor(
        AppDbContext dbContext,
        FakeActivitySyncService? activitySyncService = null,
        FakeTokenStore? tokenStore = null)
    {
        var athleteId = dbContext.Athletes
            .Select(a => (Guid?)a.Id)
            .FirstOrDefault();

        return new StravaWebhookProcessor(
            dbContext,
            activitySyncService ?? new FakeActivitySyncService(),
            tokenStore ?? new FakeTokenStore(athleteId),
            new FakeDateTimeProvider(FixedUtcNow));
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    private static Athlete CreateAthlete()
    {
        return new Athlete(
            "Lucas",
            "Test");
    }

    private static StravaWebhookEvent CreateEvent(
        string objectType,
        string aspectType,
        long objectId,
        long ownerId,
        IReadOnlyDictionary<string, string>? updates = null)
    {
        return new StravaWebhookEvent
        {
            Id = Guid.NewGuid(),
            ObjectType = objectType,
            ObjectId = objectId,
            AspectType = aspectType,
            OwnerId = ownerId,
            SubscriptionId = 456,
            EventTimeUtc = DateTimeOffset.FromUnixTimeSeconds(1_800_000_000),
            UpdatesJson = JsonSerializer.Serialize(updates ?? new Dictionary<string, string>()),
            ReceivedAtUtc = new DateTimeOffset(FixedUtcNow)
        };
    }

    private sealed class FakeActivitySyncService : IStravaActivitySyncService
    {
        public Guid? SyncedAthleteId { get; private set; }
        public long? SyncedActivityId { get; private set; }
        public Guid? InitialBackfillAthleteId { get; private set; }

        public Task InitialBackfillAsync(Guid athleteId, CancellationToken cancellationToken)
        {
            InitialBackfillAthleteId = athleteId;
            return Task.CompletedTask;
        }

        public Task SyncRecentActivitiesAsync(Guid athleteId, TimeSpan lookback, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task SyncActivityAsync(Guid athleteId, long externalActivityId, CancellationToken cancellationToken)
        {
            SyncedAthleteId = athleteId;
            SyncedActivityId = externalActivityId;
            return Task.CompletedTask;
        }

    }

    private sealed class FakeTokenStore : IStravaTokenStore
    {
        private readonly Guid? _athleteIdByStravaId;

        public FakeTokenStore(Guid? athleteIdByStravaId = null)
        {
            _athleteIdByStravaId = athleteIdByStravaId;
        }

        public long? RevokedStravaAthleteId { get; private set; }
        public DateTimeOffset? RevokedAtUtc { get; private set; }

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

        public Task<Guid?> GetAthleteIdByStravaAthleteIdAsync(long stravaAthleteId, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<Guid?> GetActiveAthleteIdByStravaAthleteIdAsync(long stravaAthleteId, CancellationToken cancellationToken)
        {
            return Task.FromResult(_athleteIdByStravaId);
        }

        public Task RevokeByStravaAthleteIdAsync(long stravaAthleteId, DateTimeOffset revokedAtUtc, CancellationToken cancellationToken)
        {
            RevokedStravaAthleteId = stravaAthleteId;
            RevokedAtUtc = revokedAtUtc;
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<Guid>> ListActiveAthleteIdsAsync(CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class FakeDateTimeProvider : IDateTimeProvider
    {
        public FakeDateTimeProvider(DateTime utcNow)
        {
            UtcNow = utcNow;
        }

        public DateTime UtcNow { get; }
    }
}
