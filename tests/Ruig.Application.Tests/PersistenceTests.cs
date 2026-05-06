using Microsoft.EntityFrameworkCore;
using Ruig.Application.Common.Interfaces;
using Ruig.Application.Common.Interfaces.Strava;
using Ruig.Application.Common.Interfaces.Strava.Models;
using Ruig.Domain.Entities;
using Ruig.Domain.Enums;
using Ruig.Infrastructure.Common.Persistance;
using Ruig.Infrastructure.Common.Persistance.Repositories;
using Ruig.Infrastructure.Strava;

namespace Ruig.Application.Tests;

public sealed class PersistenceTests
{
    private static readonly DateTime FixedUtcNow = new(2026, 5, 5, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task AthleteRepository_AddAndUpdate_PersistsAthlete()
    {
        await using var dbContext = CreateDbContext();
        var repository = new AthleteRepository(dbContext);

        var athlete = CreateAthlete(firstName: "Old");
        await repository.AddAsync(athlete, CancellationToken.None);

        await repository.UpdateFromExternalAsync(athlete.Id, CreateAthlete(firstName: "New"), CancellationToken.None);

        var saved = await repository.GetByExternalIdAsync("123", CancellationToken.None);
        Assert.NotNull(saved);
        Assert.Equal("New", saved.Firstname);
        Assert.NotEqual(default, saved.CreatedAt);
        Assert.NotEqual(default, saved.LastUpdatedAt);
    }

    [Fact]
    public async Task StravaTokenStore_SaveOrUpdate_UpsertsTokenForAthlete()
    {
        await using var dbContext = CreateDbContext();
        var athlete = CreateAthlete(firstName: "Lucas");
        dbContext.Athletes.Add(athlete);
        await dbContext.SaveChangesAsync();

        var authClient = new FakeStravaAuthClient();
        var store = new StravaTokenStore(dbContext, authClient, new FakeDateTimeProvider(FixedUtcNow));

        await store.SaveOrUpdateAsync(
            athlete.Id,
            123,
            "access-one",
            "refresh-one",
            new DateTimeOffset(FixedUtcNow).AddHours(1),
            "read,activity:read",
            CancellationToken.None);

        await store.SaveOrUpdateAsync(
            athlete.Id,
            123,
            "access-two",
            "refresh-two",
            new DateTimeOffset(FixedUtcNow).AddHours(2),
            "read,activity:read",
            CancellationToken.None);

        var accessToken = await store.GetAccessTokenAsync(athlete.Id, CancellationToken.None);

        Assert.Equal("access-two", accessToken);
        Assert.False(authClient.RefreshWasCalled);
        Assert.Equal(1, await dbContext.StravaTokens.CountAsync());
        Assert.Equal("refresh-two", await dbContext.StravaTokens.Select(t => t.RefreshToken).SingleAsync());
    }

    [Fact]
    public async Task StravaTokenStore_GetAccessToken_RefreshesExpiredToken()
    {
        await using var dbContext = CreateDbContext();
        var athlete = CreateAthlete(firstName: "Lucas");
        dbContext.Athletes.Add(athlete);
        await dbContext.SaveChangesAsync();

        var authClient = new FakeStravaAuthClient();
        var store = new StravaTokenStore(dbContext, authClient, new FakeDateTimeProvider(FixedUtcNow));

        await store.SaveOrUpdateAsync(
            athlete.Id,
            123,
            "expired-access",
            "old-refresh",
            new DateTimeOffset(FixedUtcNow).AddMinutes(-1),
            "read,activity:read",
            CancellationToken.None);

        var accessToken = await store.GetAccessTokenAsync(athlete.Id, CancellationToken.None);

        Assert.Equal("fresh-access", accessToken);
        Assert.True(authClient.RefreshWasCalled);
        Assert.Equal("old-refresh", authClient.ObservedRefreshToken);
        Assert.Equal("fresh-refresh", await dbContext.StravaTokens.Select(t => t.RefreshToken).SingleAsync());
        Assert.Equal(1_900_000_000, await dbContext.StravaTokens.Select(t => t.ExpiresAtUtc.ToUnixTimeSeconds()).SingleAsync());
    }

    [Fact]
    public async Task StravaTokenStore_RevokeByStravaAthleteId_PreventsAccessTokenUse()
    {
        await using var dbContext = CreateDbContext();
        var athlete = CreateAthlete(firstName: "Lucas");
        dbContext.Athletes.Add(athlete);
        await dbContext.SaveChangesAsync();

        var authClient = new FakeStravaAuthClient();
        var store = new StravaTokenStore(dbContext, authClient, new FakeDateTimeProvider(FixedUtcNow));

        await store.SaveOrUpdateAsync(
            athlete.Id,
            123,
            "access-token",
            "refresh-token",
            new DateTimeOffset(FixedUtcNow).AddHours(1),
            "read,activity:read",
            CancellationToken.None);

        await store.RevokeByStravaAthleteIdAsync(123, new DateTimeOffset(FixedUtcNow), CancellationToken.None);

        var accessToken = await store.GetAccessTokenAsync(athlete.Id, CancellationToken.None);

        Assert.Null(accessToken);
        Assert.Equal(new DateTimeOffset(FixedUtcNow), await dbContext.StravaTokens.Select(t => t.RevokedAtUtc).SingleAsync());
    }

    [Fact]
    public async Task StravaTokenStore_ListActiveAthleteIdsAsync_ExcludesRevokedTokens()
    {
        await using var dbContext = CreateDbContext();
        var activeAthlete = CreateAthlete(firstName: "Active");
        var revokedAthlete = new Athlete(
            "456",
            "revoked",
            "Revoked",
            "Test",
            null,
            null,
            null,
            null,
            Sex.M,
            new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2024, 1, 2, 0, 0, 0, DateTimeKind.Utc),
            "medium",
            "profile");

        dbContext.Athletes.AddRange(activeAthlete, revokedAthlete);
        await dbContext.SaveChangesAsync();

        var store = new StravaTokenStore(
            dbContext,
            new FakeStravaAuthClient(),
            new FakeDateTimeProvider(FixedUtcNow));

        await store.SaveOrUpdateAsync(
            activeAthlete.Id,
            123,
            "active-access",
            "active-refresh",
            new DateTimeOffset(FixedUtcNow).AddHours(1),
            "read,activity:read",
            CancellationToken.None);

        await store.SaveOrUpdateAsync(
            revokedAthlete.Id,
            456,
            "revoked-access",
            "revoked-refresh",
            new DateTimeOffset(FixedUtcNow).AddHours(1),
            "read,activity:read",
            CancellationToken.None);

        await store.RevokeByStravaAthleteIdAsync(456, new DateTimeOffset(FixedUtcNow), CancellationToken.None);

        var activeAthleteIds = await store.ListActiveAthleteIdsAsync(CancellationToken.None);

        Assert.Single(activeAthleteIds);
        Assert.Equal(activeAthlete.Id, activeAthleteIds[0]);
    }

    [Fact]
    public async Task ActivityRepository_GetActiveLocalDatesAsync_ReturnsDistinctDatesExcludingDeleted()
    {
        await using var dbContext = CreateDbContext();
        var athlete = CreateAthlete(firstName: "Lucas");
        dbContext.Athletes.Add(athlete);
        await dbContext.SaveChangesAsync();

        var repository = new ActivityRepository(dbContext);

        var startMay1 = new DateTimeOffset(2026, 5, 1, 9, 0, 0, TimeSpan.Zero);
        var startMay1Evening = new DateTimeOffset(2026, 5, 1, 18, 0, 0, TimeSpan.Zero);
        var startMay3 = new DateTimeOffset(2026, 5, 3, 12, 0, 0, TimeSpan.Zero);
        var startMay7Outside = new DateTimeOffset(2026, 5, 7, 12, 0, 0, TimeSpan.Zero);
        var startMay4Deleted = new DateTimeOffset(2026, 5, 4, 7, 0, 0, TimeSpan.Zero);

        await repository.UpsertAsync(BuildActivity(athlete.Id, "1", startMay1), CancellationToken.None);
        await repository.UpsertAsync(BuildActivity(athlete.Id, "2", startMay1Evening), CancellationToken.None);
        await repository.UpsertAsync(BuildActivity(athlete.Id, "3", startMay3), CancellationToken.None);
        await repository.UpsertAsync(BuildActivity(athlete.Id, "4", startMay7Outside), CancellationToken.None);

        var deleted = BuildActivity(athlete.Id, "5", startMay4Deleted);
        deleted.MarkDeleted(new DateTimeOffset(FixedUtcNow));
        await repository.UpsertAsync(deleted, CancellationToken.None);

        await repository.SaveChangesAsync(CancellationToken.None);

        var dates = await repository.GetActiveLocalDatesAsync(
            athlete.Id,
            new DateOnly(2026, 5, 1),
            new DateOnly(2026, 5, 5),
            CancellationToken.None);

        var orderedDates = dates.OrderBy(d => d).ToList();

        Assert.Equal(new[] { new DateOnly(2026, 5, 1), new DateOnly(2026, 5, 3) }, orderedDates);
    }

    [Fact]
    public async Task StravaWebhookEventStore_SaveAsync_PersistsEvent()
    {
        await using var dbContext = CreateDbContext();
        var store = new StravaWebhookEventStore(dbContext, new FakeDateTimeProvider(FixedUtcNow));

        await store.SaveAsync(
            new StravaWebhookEventMessage(
                "activity",
                987,
                "create",
                123,
                456,
                1_800_000_000,
                new Dictionary<string, string>()),
            CancellationToken.None);

        var savedEvent = await dbContext.StravaWebhookEvents.SingleAsync();
        Assert.Equal("activity", savedEvent.ObjectType);
        Assert.Equal(987, savedEvent.ObjectId);
        Assert.Equal("create", savedEvent.AspectType);
        Assert.Equal(new DateTimeOffset(FixedUtcNow), savedEvent.ReceivedAtUtc);
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    private static Activity BuildActivity(Guid athleteId, string externalId, DateTimeOffset startedAtUtc)
    {
        return new Activity(
            athleteId,
            externalId,
            "Test Activity",
            ActivitySport.Run,
            distanceMeters: 5000,
            movingTimeSeconds: 1800,
            elapsedTimeSeconds: 1900,
            totalElevationGainMeters: 10,
            startedAtUtc: startedAtUtc,
            utcOffsetAtStart: TimeSpan.Zero,
            visibility: ActivityVisibility.Everyone,
            deviceName: null,
            externalMapId: null,
            summaryPolyline: null);
    }

    private static Athlete CreateAthlete(string firstName)
    {
        return new Athlete(
            "123",
            "lucas",
            firstName,
            "Test",
            null,
            null,
            null,
            null,
            Sex.M,
            new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2024, 1, 2, 0, 0, 0, DateTimeKind.Utc),
            "medium",
            "profile");
    }

    private sealed class FakeStravaAuthClient : IStravaAuthClient
    {
        public bool RefreshWasCalled { get; private set; }
        public string? ObservedRefreshToken { get; private set; }

        public string BuildAuthorizeUrl(string state)
        {
            throw new NotSupportedException();
        }

        public Task<StravaTokenResponse> ExchangeCodeAsync(string code, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<StravaRefreshTokenResponse> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken)
        {
            RefreshWasCalled = true;
            ObservedRefreshToken = refreshToken;

            return Task.FromResult(new StravaRefreshTokenResponse(
                "fresh-access",
                "fresh-refresh",
                1_900_000_000));
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
