using Ruig.Application.Athletes.Commands.CompleteStravaOAuth;
using Ruig.Application.Common.Interfaces;
using Ruig.Application.Common.Interfaces.Strava;
using Ruig.Application.Common.Interfaces.Strava.Models;
using Ruig.Domain.Entities;

namespace Ruig.Application.Tests;

public sealed class CompleteStravaOAuthHandlerTests
{
    [Fact]
    public async Task Handle_WithNewAthlete_SavesAthleteAndToken()
    {
        var authClient = new FakeStravaAuthClient();
        var apiClient = new FakeStravaApiClient(CreateAthleteResponse(firstName: "Lucas"));
        var tokenStore = new FakeTokenStore();
        var stateStore = new FakeOAuthStateStore(valid: true);
        var activitySyncService = new FakeActivitySyncService();
        var athleteRepository = new FakeAthleteRepository();
        var handler = new CompleteStravaOAuthHandler(authClient, apiClient, tokenStore, stateStore, activitySyncService, athleteRepository);

        var athleteId = await handler.Handle(new CompleteStravaOAuthCommand("code", "state"), CancellationToken.None);

        var athlete = await athleteRepository.GetByIdAsync(athleteId, CancellationToken.None);
        Assert.NotNull(athlete);
        Assert.Equal("123", athlete.ExternalAthleteId);
        Assert.Equal("Lucas", athlete.Firstname);
        Assert.Equal(athleteId, tokenStore.SavedAthleteId);
        Assert.Equal("read,activity:read", tokenStore.SavedScope);
        Assert.Equal(athleteId, activitySyncService.InitialBackfillAthleteId);
    }

    [Fact]
    public async Task Handle_WithExistingAthlete_UpdatesExistingAthleteAndToken()
    {
        var existing = CreateAthlete(firstName: "Old");
        var athleteRepository = new FakeAthleteRepository();
        await athleteRepository.AddAsync(existing, CancellationToken.None);

        var handler = new CompleteStravaOAuthHandler(
            new FakeStravaAuthClient(),
            new FakeStravaApiClient(CreateAthleteResponse(firstName: "Updated")),
            new FakeTokenStore(),
            new FakeOAuthStateStore(valid: true),
            new FakeActivitySyncService(),
            athleteRepository);

        var athleteId = await handler.Handle(new CompleteStravaOAuthCommand("code", "state"), CancellationToken.None);

        Assert.Equal(existing.Id, athleteId);
        var updated = await athleteRepository.GetByIdAsync(existing.Id, CancellationToken.None);
        Assert.Equal("Updated", updated?.Firstname);
    }

    [Fact]
    public async Task Handle_WithInvalidState_DoesNotExchangeCode()
    {
        var authClient = new FakeStravaAuthClient();
        var handler = new CompleteStravaOAuthHandler(
            authClient,
            new FakeStravaApiClient(CreateAthleteResponse(firstName: "Lucas")),
            new FakeTokenStore(),
            new FakeOAuthStateStore(valid: false),
            new FakeActivitySyncService(),
            new FakeAthleteRepository());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(new CompleteStravaOAuthCommand("code", "bad-state"), CancellationToken.None));

        Assert.False(authClient.ExchangeCodeWasCalled);
    }

    private static StravaAthleteResponse CreateAthleteResponse(string firstName)
    {
        return new StravaAthleteResponse(
            123,
            "lucas",
            firstName,
            "Test",
            "bio",
            "city",
            "state",
            "country",
            "m",
            "medium",
            "profile",
            "2024-01-01T00:00:00Z",
            "2024-01-02T00:00:00Z");
    }

    private static Athlete CreateAthlete(string firstName)
    {
        return new Athlete(
            "123",
            "lucas",
            firstName,
            "Test",
            "bio",
            "city",
            "state",
            "country",
            Ruig.Domain.Enums.Sex.M,
            new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2024, 1, 2, 0, 0, 0, DateTimeKind.Utc),
            "medium",
            "profile");
    }

    private sealed class FakeStravaAuthClient : IStravaAuthClient
    {
        public bool ExchangeCodeWasCalled { get; private set; }

        public string BuildAuthorizeUrl(string state)
        {
            throw new NotSupportedException();
        }

        public Task<StravaTokenResponse> ExchangeCodeAsync(string code, CancellationToken cancellationToken)
        {
            ExchangeCodeWasCalled = true;
            return Task.FromResult(new StravaTokenResponse(
                "access-token",
                "refresh-token",
                1_800_000_000,
                123,
                "read,activity:read"));
        }

        public Task<StravaRefreshTokenResponse> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class FakeStravaApiClient : IStravaApiClient
    {
        private readonly StravaAthleteResponse _response;

        public FakeStravaApiClient(StravaAthleteResponse response)
        {
            _response = response;
        }

        public Task<StravaAthleteResponse> GetCurrentAthleteAsync(string accessToken, CancellationToken cancellationToken)
        {
            return Task.FromResult(_response);
        }

        public Task<IReadOnlyList<StravaActivityResponse>> ListAthleteActivitiesAsync(
            string accessToken,
            DateTimeOffset? afterUtc,
            DateTimeOffset? beforeUtc,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<StravaActivityResponse> GetActivityAsync(
            string accessToken,
            long activityId,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class FakeTokenStore : IStravaTokenStore
    {
        public Guid SavedAthleteId { get; private set; }
        public string? SavedScope { get; private set; }

        public Task SaveOrUpdateAsync(
            Guid athleteId,
            long stravaAthleteId,
            string accessToken,
            string refreshToken,
            DateTimeOffset expiresAtUtc,
            string scope,
            CancellationToken cancellationToken)
        {
            SavedAthleteId = athleteId;
            SavedScope = scope;
            return Task.CompletedTask;
        }

        public Task<string?> GetAccessTokenAsync(Guid athleteId, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task RevokeByStravaAthleteIdAsync(long stravaAthleteId, DateTimeOffset revokedAtUtc, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class FakeOAuthStateStore : IStravaOAuthStateStore
    {
        private readonly bool _valid;

        public FakeOAuthStateStore(bool valid)
        {
            _valid = valid;
        }

        public Task StoreAsync(string state, TimeSpan ttl, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<bool> ConsumeAsync(string state, CancellationToken cancellationToken)
        {
            return Task.FromResult(_valid);
        }
    }

    private sealed class FakeActivitySyncService : IStravaActivitySyncService
    {
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
            throw new NotSupportedException();
        }

        public Task MarkActivityDeletedAsync(Guid athleteId, long externalActivityId, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class FakeAthleteRepository : IAthleteRepository
    {
        private readonly Dictionary<Guid, Athlete> _athletesById = new();

        public Task<Athlete?> GetByIdAsync(Guid athleteId, CancellationToken cancellationToken)
        {
            _athletesById.TryGetValue(athleteId, out var athlete);
            return Task.FromResult(athlete);
        }

        public Task<bool> Exists(Guid athleteId, CancellationToken cancellationToken)
        {
            return Task.FromResult(_athletesById.ContainsKey(athleteId));
        }

        public Task AddAsync(Athlete athlete, CancellationToken cancellationToken)
        {
            _athletesById[athlete.Id] = athlete;
            return Task.CompletedTask;
        }

        public Task UpdateFromExternalAsync(Guid athleteId, Athlete externalAthlete, CancellationToken cancellationToken)
        {
            _athletesById[athleteId].UpdateFromExternal(externalAthlete);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task<Athlete?> GetByExternalIdAsync(string externalId, CancellationToken cancellationToken)
        {
            var athlete = _athletesById.Values.FirstOrDefault(a => a.ExternalAthleteId == externalId);
            return Task.FromResult(athlete);
        }

        public Task MarkActivitySyncCompletedAsync(Guid athleteId, DateTimeOffset syncedAtUtc, CancellationToken cancellationToken)
        {
            _athletesById[athleteId].MarkActivitySyncCompleted(syncedAtUtc);
            return Task.CompletedTask;
        }
    }
}
