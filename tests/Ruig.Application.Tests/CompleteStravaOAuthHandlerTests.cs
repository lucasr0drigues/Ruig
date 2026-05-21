using Ruig.Application.Athletes.Commands.CompleteStravaOAuth;
using Ruig.Application.Common.Interfaces;
using Ruig.Application.Common.Interfaces.Strava;
using Ruig.Application.Common.Interfaces.Strava.Models;
using Ruig.Domain.Entities;

namespace Ruig.Application.Tests;

public sealed class CompleteStravaOAuthHandlerTests
{
    [Fact]
    public async Task Handle_WithNewAthlete_SavesAthleteTokenAndCreatesBadge()
    {
        var authClient = new FakeStravaAuthClient();
        var apiClient = new FakeStravaApiClient(CreateAthleteResponse(firstName: "Lucas"));
        var tokenStore = new FakeTokenStore();
        var stateStore = new FakeOAuthStateStore(new StravaOAuthStateData("lucas"));
        var activitySyncService = new FakeActivitySyncService();
        var athleteRepository = new FakeAthleteRepository();
        var badgeRepository = new FakeBadgeRepository();
        var slugGenerator = new FakeSlugGenerator("slug-001");

        var handler = new CompleteStravaOAuthHandler(
            authClient, apiClient, tokenStore, stateStore,
            activitySyncService, athleteRepository, badgeRepository, slugGenerator);

        var result = await handler.Handle(new CompleteStravaOAuthCommand("code", "state"), CancellationToken.None);

        var athlete = await athleteRepository.GetByIdAsync(result.AthleteId, CancellationToken.None);
        Assert.NotNull(athlete);
        Assert.Equal("Lucas", athlete.Firstname);
        Assert.Equal("Test", athlete.Lastname);
        Assert.Equal(result.AthleteId, tokenStore.SavedAthleteId);
        Assert.Equal("read,activity:read", tokenStore.SavedScope);
        Assert.Equal(result.AthleteId, activitySyncService.InitialBackfillAthleteId);

        Assert.Equal("lucas", result.GitHubUsername);
        Assert.Equal("slug-001", result.BadgeSlug);

        var badge = badgeRepository.SingleOrNull;
        Assert.NotNull(badge);
        Assert.Equal("slug-001", badge!.Slug);
        Assert.Equal("lucas", badge.GitHubUsername);
        Assert.Equal(result.AthleteId, badge.AthleteId);
        Assert.True(badge.IsEnabled);
    }

    [Fact]
    public async Task Handle_WithExistingAthleteAndBadge_KeepsSlugAndUpdatesUsername()
    {
        var existing = CreateAthlete(firstName: "Old");
        var athleteRepository = new FakeAthleteRepository();
        await athleteRepository.AddAsync(existing, CancellationToken.None);

        var existingBadge = new Badge(existing.Id, "slug-existing", "old-handle");
        existingBadge.Disable();
        var badgeRepository = new FakeBadgeRepository();
        await badgeRepository.AddAsync(existingBadge, CancellationToken.None);

        var handler = new CompleteStravaOAuthHandler(
            new FakeStravaAuthClient(),
            new FakeStravaApiClient(CreateAthleteResponse(firstName: "Updated")),
            new FakeTokenStore(existing.Id),
            new FakeOAuthStateStore(new StravaOAuthStateData("new-handle")),
            new FakeActivitySyncService(),
            athleteRepository,
            badgeRepository,
            new FakeSlugGenerator("should-not-be-used"));

        var result = await handler.Handle(new CompleteStravaOAuthCommand("code", "state"), CancellationToken.None);

        Assert.Equal(existing.Id, result.AthleteId);
        Assert.Equal("slug-existing", result.BadgeSlug);
        Assert.Equal("new-handle", result.GitHubUsername);

        var badge = badgeRepository.SingleOrNull;
        Assert.NotNull(badge);
        Assert.Equal("slug-existing", badge!.Slug);
        Assert.Equal("new-handle", badge.GitHubUsername);
        Assert.True(badge.IsEnabled);
    }

    [Fact]
    public async Task Handle_RetriesSlugGenerationOnCollision()
    {
        var slugGenerator = new FakeSlugGenerator("dup", "dup", "unique");
        var badgeRepository = new FakeBadgeRepository();
        badgeRepository.AddExistingSlug("dup");

        var handler = new CompleteStravaOAuthHandler(
            new FakeStravaAuthClient(),
            new FakeStravaApiClient(CreateAthleteResponse(firstName: "Lucas")),
            new FakeTokenStore(),
            new FakeOAuthStateStore(new StravaOAuthStateData("lucas")),
            new FakeActivitySyncService(),
            new FakeAthleteRepository(),
            badgeRepository,
            slugGenerator);

        var result = await handler.Handle(new CompleteStravaOAuthCommand("code", "state"), CancellationToken.None);

        Assert.Equal("unique", result.BadgeSlug);
        Assert.Equal(3, slugGenerator.CallCount);
    }

    [Fact]
    public async Task Handle_WithInvalidState_DoesNotExchangeCode()
    {
        var authClient = new FakeStravaAuthClient();
        var handler = new CompleteStravaOAuthHandler(
            authClient,
            new FakeStravaApiClient(CreateAthleteResponse(firstName: "Lucas")),
            new FakeTokenStore(),
            new FakeOAuthStateStore(null),
            new FakeActivitySyncService(),
            new FakeAthleteRepository(),
            new FakeBadgeRepository(),
            new FakeSlugGenerator("slug"));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(new CompleteStravaOAuthCommand("code", "bad-state"), CancellationToken.None));

        Assert.False(authClient.ExchangeCodeWasCalled);
    }

    private static StravaAthleteResponse CreateAthleteResponse(string firstName)
    {
        return new StravaAthleteResponse(
            123,
            firstName,
            "Test");
    }

    private static Athlete CreateAthlete(string firstName)
    {
        return new Athlete(
            firstName,
            "Test");
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
        private readonly Guid? _athleteIdByStravaId;

        public FakeTokenStore(Guid? athleteIdByStravaId = null)
        {
            _athleteIdByStravaId = athleteIdByStravaId;
        }

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

        public Task<Guid?> GetAthleteIdByStravaAthleteIdAsync(long stravaAthleteId, CancellationToken cancellationToken)
        {
            return Task.FromResult(_athleteIdByStravaId);
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

    private sealed class FakeOAuthStateStore : IStravaOAuthStateStore
    {
        private readonly StravaOAuthStateData? _data;

        public FakeOAuthStateStore(StravaOAuthStateData? data)
        {
            _data = data;
        }

        public Task StoreAsync(string state, StravaOAuthStateData data, TimeSpan ttl, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<StravaOAuthStateData?> ConsumeAsync(string state, CancellationToken cancellationToken)
        {
            return Task.FromResult(_data);
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

        public Task MarkActivitySyncCompletedAsync(Guid athleteId, DateTimeOffset syncedAtUtc, CancellationToken cancellationToken)
        {
            _athletesById[athleteId].MarkActivitySyncCompleted(syncedAtUtc);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeBadgeRepository : IBadgeRepository
    {
        private readonly Dictionary<Guid, Badge> _byAthlete = new();
        private readonly HashSet<string> _existingSlugs = new(StringComparer.OrdinalIgnoreCase);

        public Badge? SingleOrNull => _byAthlete.Values.FirstOrDefault();

        public void AddExistingSlug(string slug) => _existingSlugs.Add(slug);

        public Task<Badge?> GetBySlugAsync(string slug, CancellationToken cancellationToken)
        {
            var badge = _byAthlete.Values.FirstOrDefault(b => string.Equals(b.Slug, slug, StringComparison.OrdinalIgnoreCase));
            return Task.FromResult(badge);
        }

        public Task<Badge?> GetByAthleteIdAsync(Guid athleteId, CancellationToken cancellationToken)
        {
            _byAthlete.TryGetValue(athleteId, out var badge);
            return Task.FromResult(badge);
        }

        public Task<bool> SlugExistsAsync(string slug, CancellationToken cancellationToken)
        {
            var exists = _existingSlugs.Contains(slug)
                || _byAthlete.Values.Any(b => string.Equals(b.Slug, slug, StringComparison.OrdinalIgnoreCase));
            return Task.FromResult(exists);
        }

        public Task AddAsync(Badge badge, CancellationToken cancellationToken)
        {
            _byAthlete[badge.AthleteId] = badge;
            _existingSlugs.Add(badge.Slug);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakeSlugGenerator : IBadgeSlugGenerator
    {
        private readonly Queue<string> _slugs;
        private readonly string _fallback;

        public FakeSlugGenerator(params string[] slugs)
        {
            _slugs = new Queue<string>(slugs);
            _fallback = slugs.Length > 0 ? slugs[^1] : "default-slug";
        }

        public int CallCount { get; private set; }

        public string Generate()
        {
            CallCount++;
            return _slugs.Count > 0 ? _slugs.Dequeue() : _fallback;
        }
    }
}
