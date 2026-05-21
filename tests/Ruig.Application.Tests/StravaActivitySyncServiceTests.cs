using Microsoft.EntityFrameworkCore;
using Ruig.Application.Common.Interfaces;
using Ruig.Application.Common.Interfaces.Strava;
using Ruig.Application.Common.Interfaces.Strava.Models;
using Ruig.Domain.Entities;
using Ruig.Infrastructure.Common.Persistance;
using Ruig.Infrastructure.Common.Persistance.Repositories;
using Ruig.Infrastructure.Strava;

namespace Ruig.Application.Tests;

public sealed class StravaActivitySyncServiceTests
{
    private static readonly DateTime FixedUtcNow = new(2026, 5, 5, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task InitialBackfill_FetchesExpectedRangeAndPersistsActivities()
    {
        await using var dbContext = CreateDbContext();
        var athlete = CreateAthlete();
        dbContext.Athletes.Add(athlete);
        await dbContext.SaveChangesAsync();

        var apiClient = new FakeStravaApiClient();
        var service = new StravaActivitySyncService(
            new FakeTokenStore(),
            apiClient,
            new ActivityRepository(dbContext),
            new AthleteRepository(dbContext),
            new FakeDateTimeProvider(FixedUtcNow));

        await service.InitialBackfillAsync(athlete.Id, CancellationToken.None);

        Assert.Equal(DateTimeOffset.Parse("2025-01-01T00:00:00Z"), apiClient.ObservedAfterUtc);
        Assert.Equal(new DateTimeOffset(FixedUtcNow), apiClient.ObservedBeforeUtc);

        var savedActivity = await dbContext.Activities.SingleAsync();
        Assert.Equal(athlete.Id, savedActivity.AthleteId);
        Assert.Equal(new DateOnly(2026, 5, 5), savedActivity.LocalDate);

        var savedAthlete = await dbContext.Athletes.SingleAsync();
        Assert.Equal(new DateTimeOffset(FixedUtcNow), savedAthlete.LastActivitySyncedAtUtc);
    }

    [Fact]
    public async Task SyncActivity_UpsertsExistingActivityDay()
    {
        await using var dbContext = CreateDbContext();
        var athlete = CreateAthlete();
        dbContext.Athletes.Add(athlete);
        await dbContext.SaveChangesAsync();

        var service = new StravaActivitySyncService(
            new FakeTokenStore(),
            new FakeStravaApiClient(),
            new ActivityRepository(dbContext),
            new AthleteRepository(dbContext),
            new FakeDateTimeProvider(FixedUtcNow));

        await service.SyncActivityAsync(athlete.Id, 987, CancellationToken.None);

        service = new StravaActivitySyncService(
            new FakeTokenStore(),
            new FakeStravaApiClient(),
            new ActivityRepository(dbContext),
            new AthleteRepository(dbContext),
            new FakeDateTimeProvider(FixedUtcNow));

        await service.SyncActivityAsync(athlete.Id, 987, CancellationToken.None);

        Assert.Equal(1, await dbContext.Activities.CountAsync());
        Assert.Equal(new DateOnly(2026, 5, 5), await dbContext.Activities.Select(a => a.LocalDate).SingleAsync());
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

    private sealed class FakeTokenStore : IStravaTokenStore
    {
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
            return Task.FromResult<string?>("access-token");
        }

        public Task<Guid?> GetAthleteIdByStravaAthleteIdAsync(long stravaAthleteId, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<Guid?> GetActiveAthleteIdByStravaAthleteIdAsync(long stravaAthleteId, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task RevokeByStravaAthleteIdAsync(long stravaAthleteId, DateTimeOffset revokedAtUtc, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<IReadOnlyList<Guid>> ListActiveAthleteIdsAsync(CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class FakeStravaApiClient : IStravaApiClient
    {
        public DateTimeOffset? ObservedAfterUtc { get; private set; }
        public DateTimeOffset? ObservedBeforeUtc { get; private set; }

        public Task<StravaAthleteResponse> GetCurrentAthleteAsync(string accessToken, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<IReadOnlyList<StravaActivityResponse>> ListAthleteActivitiesAsync(
            string accessToken,
            DateTimeOffset? afterUtc,
            DateTimeOffset? beforeUtc,
            CancellationToken cancellationToken)
        {
            ObservedAfterUtc = afterUtc;
            ObservedBeforeUtc = beforeUtc;

            return Task.FromResult<IReadOnlyList<StravaActivityResponse>>([CreateActivity()]);
        }

        public Task<StravaActivityResponse> GetActivityAsync(
            string accessToken,
            long activityId,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(CreateActivity());
        }

        private StravaActivityResponse CreateActivity()
        {
            return new StravaActivityResponse(
                "2026-05-05T12:00:00Z",
                "2026-05-05T09:00:00Z",
                -10800);
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
