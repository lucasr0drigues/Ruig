using Microsoft.EntityFrameworkCore;
using Ruig.Domain.Entities;
using Ruig.Domain.Enums;
using Ruig.Infrastructure.Common.Persistance;
using Ruig.Infrastructure.Common.Persistance.Repositories;
using Ruig.Infrastructure.Strava;

namespace Ruig.Application.Tests;

public sealed class PersistenceTests
{
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

        var store = new StravaTokenStore(dbContext);

        await store.SaveOrUpdateAsync(
            athlete.Id,
            123,
            "access-one",
            "refresh-one",
            DateTimeOffset.UtcNow.AddHours(1),
            "read,activity:read",
            CancellationToken.None);

        await store.SaveOrUpdateAsync(
            athlete.Id,
            123,
            "access-two",
            "refresh-two",
            DateTimeOffset.UtcNow.AddHours(2),
            "read,activity:read",
            CancellationToken.None);

        var accessToken = await store.GetAccessTokenAsync(athlete.Id, CancellationToken.None);

        Assert.Equal("access-two", accessToken);
        Assert.Equal(1, await dbContext.StravaTokens.CountAsync());
        Assert.Equal("refresh-two", await dbContext.StravaTokens.Select(t => t.RefreshToken).SingleAsync());
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
}
