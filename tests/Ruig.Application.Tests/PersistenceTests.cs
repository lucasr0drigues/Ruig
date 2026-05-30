using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Ruig.Application.Common.Interfaces;
using Ruig.Application.Common.Interfaces.Strava;
using Ruig.Application.Common.Interfaces.Strava.Models;
using Ruig.Domain.Entities;
using Ruig.Infrastructure.Common.Persistance;
using Ruig.Infrastructure.Common.Persistance.Repositories;
using Ruig.Infrastructure.Security;
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

        var saved = await repository.GetByIdAsync(athlete.Id, CancellationToken.None);
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
        var tokenEncryptor = CreateTokenEncryptor();
        var store = new StravaTokenStore(dbContext, authClient, new FakeDateTimeProvider(FixedUtcNow), tokenEncryptor);

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
        var storedRefreshToken = await dbContext.StravaTokens.Select(t => t.RefreshToken).SingleAsync();
        Assert.NotEqual("refresh-two", storedRefreshToken);
        Assert.Equal("refresh-two", tokenEncryptor.Decrypt(storedRefreshToken));
    }

    [Fact]
    public async Task StravaTokenStore_GetAccessToken_RefreshesExpiredToken()
    {
        await using var dbContext = CreateDbContext();
        var athlete = CreateAthlete(firstName: "Lucas");
        dbContext.Athletes.Add(athlete);
        await dbContext.SaveChangesAsync();

        var authClient = new FakeStravaAuthClient();
        var tokenEncryptor = CreateTokenEncryptor();
        var store = new StravaTokenStore(dbContext, authClient, new FakeDateTimeProvider(FixedUtcNow), tokenEncryptor);

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
        var storedRefreshToken = await dbContext.StravaTokens.Select(t => t.RefreshToken).SingleAsync();
        Assert.NotEqual("fresh-refresh", storedRefreshToken);
        Assert.Equal("fresh-refresh", tokenEncryptor.Decrypt(storedRefreshToken));
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
        var store = new StravaTokenStore(dbContext, authClient, new FakeDateTimeProvider(FixedUtcNow), CreateTokenEncryptor());

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
        var revokedAthlete = new Athlete("Revoked", "Test");

        dbContext.Athletes.AddRange(activeAthlete, revokedAthlete);
        await dbContext.SaveChangesAsync();

        var store = new StravaTokenStore(
            dbContext,
            new FakeStravaAuthClient(),
            new FakeDateTimeProvider(FixedUtcNow),
            CreateTokenEncryptor());

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
    public async Task StravaTokenStore_GetAthleteIdByStravaAthleteId_ReturnsActiveTokenAthlete()
    {
        await using var dbContext = CreateDbContext();
        var athlete = CreateAthlete(firstName: "Lucas");
        dbContext.Athletes.Add(athlete);
        await dbContext.SaveChangesAsync();

        var store = new StravaTokenStore(
            dbContext,
            new FakeStravaAuthClient(),
            new FakeDateTimeProvider(FixedUtcNow),
            CreateTokenEncryptor());

        await store.SaveOrUpdateAsync(
            athlete.Id,
            123,
            "access-token",
            "refresh-token",
            new DateTimeOffset(FixedUtcNow).AddHours(1),
            "read,activity:read",
            CancellationToken.None);

        var athleteId = await store.GetAthleteIdByStravaAthleteIdAsync(123, CancellationToken.None);

        Assert.Equal(athlete.Id, athleteId);
    }

    [Fact]
    public async Task ActivityRepository_GetActiveLocalDatesAsync_ReturnsDistinctDates()
    {
        await using var dbContext = CreateDbContext();
        var athlete = CreateAthlete(firstName: "Lucas");
        dbContext.Athletes.Add(athlete);
        await dbContext.SaveChangesAsync();

        var repository = new ActivityRepository(dbContext);

        await repository.UpsertAsync(BuildActivity(athlete.Id, new DateOnly(2026, 5, 1)), CancellationToken.None);
        await repository.UpsertAsync(BuildActivity(athlete.Id, new DateOnly(2026, 5, 1)), CancellationToken.None);
        await repository.UpsertAsync(BuildActivity(athlete.Id, new DateOnly(2026, 5, 3)), CancellationToken.None);
        await repository.UpsertAsync(BuildActivity(athlete.Id, new DateOnly(2026, 5, 7)), CancellationToken.None);
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
    public async Task ActivityRepository_ReplaceLocalDatesAsync_ReplacesDatesInRange()
    {
        await using var dbContext = CreateDbContext();
        var athlete = CreateAthlete(firstName: "Lucas");
        dbContext.Athletes.Add(athlete);
        await dbContext.SaveChangesAsync();

        var repository = new ActivityRepository(dbContext);

        await repository.UpsertAsync(BuildActivity(athlete.Id, new DateOnly(2026, 5, 1)), CancellationToken.None);
        await repository.UpsertAsync(BuildActivity(athlete.Id, new DateOnly(2026, 5, 2)), CancellationToken.None);
        await repository.UpsertAsync(BuildActivity(athlete.Id, new DateOnly(2026, 5, 9)), CancellationToken.None);
        await repository.SaveChangesAsync(CancellationToken.None);

        await repository.ReplaceLocalDatesAsync(
            athlete.Id,
            new DateOnly(2026, 5, 1),
            new DateOnly(2026, 5, 5),
            [new DateOnly(2026, 5, 3), new DateOnly(2026, 5, 3)],
            CancellationToken.None);

        await repository.SaveChangesAsync(CancellationToken.None);

        var dates = await dbContext.Activities
            .Where(a => a.AthleteId == athlete.Id)
            .Select(a => a.LocalDate)
            .OrderBy(d => d)
            .ToListAsync();

        Assert.Equal(new[] { new DateOnly(2026, 5, 3), new DateOnly(2026, 5, 9) }, dates);
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

    [Fact]
    public async Task StravaWebhookEventStore_SaveAsync_IgnoresDuplicateEvent()
    {
        await using var dbContext = CreateDbContext();
        var store = new StravaWebhookEventStore(dbContext, new FakeDateTimeProvider(FixedUtcNow));
        var message = new StravaWebhookEventMessage(
            "activity",
            987,
            "create",
            123,
            456,
            1_800_000_000,
            new Dictionary<string, string>());

        await store.SaveAsync(message, CancellationToken.None);
        await store.SaveAsync(message, CancellationToken.None);

        Assert.Equal(1, await dbContext.StravaWebhookEvents.CountAsync());
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    private static Activity BuildActivity(Guid athleteId, DateOnly localDate)
    {
        return new Activity(athleteId, localDate);
    }

    private static Athlete CreateAthlete(string firstName)
    {
        return new Athlete(firstName, "Test");
    }

    private static ITokenEncryptor CreateTokenEncryptor()
    {
        return new AesGcmTokenEncryptor(Options.Create(new TokenEncryptionOptions
        {
            CurrentKeyId = "test",
            Keys = new Dictionary<string, string>
            {
                ["test"] = "000102030405060708090a0b0c0d0e0f101112131415161718191a1b1c1d1e1f"
            }
        }));
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
