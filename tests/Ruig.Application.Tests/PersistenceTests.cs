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

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
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
