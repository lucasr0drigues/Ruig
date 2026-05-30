using Ruig.Application.Badges.Queries.GetBadgeSvg;
using Ruig.Application.Common.Dispatching;
using Ruig.Application.Common.Interfaces;
using Ruig.Application.Common.Interfaces.GitHub.Models;
using Ruig.Application.Heatmaps.Models;
using Ruig.Application.Heatmaps.Queries.GetHeatmap;
using Ruig.Domain.Entities;

namespace Ruig.Application.Tests;

public sealed class GetBadgeSvgHandlerTests
{
    [Fact]
    public async Task Handle_RendersSvgForActiveBadgeUsingTrailingYearWindow()
    {
        var athleteId = Guid.NewGuid();
        var badge = new Badge(athleteId, "abc123", "lucas");
        var badgeRepository = new FakeBadgeRepository(badge);
        var renderer = new FakeRenderer("<svg/>");
        var dateTimeProvider = new FakeDateTimeProvider(new DateTime(2026, 5, 5, 12, 0, 0, DateTimeKind.Utc));
        var dispatcher = new FakeDispatcher(BuildHeatmap(athleteId, "lucas", new DateOnly(2025, 5, 6), new DateOnly(2026, 5, 5)));

        var handler = new GetBadgeSvgHandler(badgeRepository, new FakeAthleteRepository(null), renderer, dateTimeProvider, dispatcher);

        var result = await handler.Handle(new GetBadgeSvgQuery("abc123"), CancellationToken.None);

        Assert.Equal("abc123", result.Slug);
        Assert.Equal("<svg/>", result.Svg);
        Assert.Equal(new DateOnly(2026, 5, 5), result.RangeTo);
        Assert.Equal(new DateOnly(2025, 5, 6), result.RangeFrom);
        Assert.Equal(new DateTimeOffset(2026, 5, 5, 12, 0, 0, TimeSpan.Zero), result.GeneratedAtUtc);

        Assert.NotNull(dispatcher.LastQuery);
        Assert.Equal(athleteId, dispatcher.LastQuery!.AthleteId);
        Assert.Equal("lucas", dispatcher.LastQuery.GitHubUsername);
        Assert.Equal(new DateOnly(2025, 5, 6), dispatcher.LastQuery.From);
        Assert.Equal(new DateOnly(2026, 5, 5), dispatcher.LastQuery.To);
    }

    [Fact]
    public async Task Handle_FallsBackToCatalogDefaultsWhenQueryParamsAreMissing()
    {
        var athleteId = Guid.NewGuid();
        var badge = new Badge(athleteId, "abc123", "lucas");
        var renderer = new FakeRenderer("<svg/>");

        var handler = new GetBadgeSvgHandler(
            new FakeBadgeRepository(badge),
            new FakeAthleteRepository(null),
            renderer,
            new FakeDateTimeProvider(new DateTime(2026, 5, 7, 12, 0, 0, DateTimeKind.Utc)),
            new FakeDispatcher(BuildHeatmap(athleteId, "lucas", new DateOnly(2025, 5, 8), new DateOnly(2026, 5, 7))));

        await handler.Handle(new GetBadgeSvgQuery("abc123"), CancellationToken.None);

        Assert.NotNull(renderer.LastRequest);
        Assert.Equal("purple", renderer.LastRequest!.ThemeKey);
        Assert.Equal("strava", renderer.LastRequest.AccentKey);
        Assert.Equal("lucas", renderer.LastRequest.GitHubUsername);
    }

    [Fact]
    public async Task Handle_AppliesQueryParamsWhenProvided()
    {
        var athleteId = Guid.NewGuid();
        var badge = new Badge(athleteId, "abc123", "lucas");
        var renderer = new FakeRenderer("<svg/>");

        var handler = new GetBadgeSvgHandler(
            new FakeBadgeRepository(badge),
            new FakeAthleteRepository(null),
            renderer,
            new FakeDateTimeProvider(new DateTime(2026, 5, 7, 12, 0, 0, DateTimeKind.Utc)),
            new FakeDispatcher(BuildHeatmap(athleteId, "lucas", new DateOnly(2025, 5, 8), new DateOnly(2026, 5, 7))));

        await handler.Handle(new GetBadgeSvgQuery("abc123", Theme: "amber", Accent: "magenta"), CancellationToken.None);

        Assert.NotNull(renderer.LastRequest);
        Assert.Equal("amber", renderer.LastRequest!.ThemeKey);
        Assert.Equal("magenta", renderer.LastRequest.AccentKey);
    }

    [Fact]
    public async Task Handle_FallsBackToDefaultsWhenQueryParamsAreUnknown()
    {
        var athleteId = Guid.NewGuid();
        var badge = new Badge(athleteId, "abc123", "lucas");
        var renderer = new FakeRenderer("<svg/>");

        var handler = new GetBadgeSvgHandler(
            new FakeBadgeRepository(badge),
            new FakeAthleteRepository(null),
            renderer,
            new FakeDateTimeProvider(new DateTime(2026, 5, 7, 12, 0, 0, DateTimeKind.Utc)),
            new FakeDispatcher(BuildHeatmap(athleteId, "lucas", new DateOnly(2025, 5, 8), new DateOnly(2026, 5, 7))));

        await handler.Handle(new GetBadgeSvgQuery("abc123", Theme: "not-a-theme", Accent: "not-an-accent"), CancellationToken.None);

        Assert.Equal("purple", renderer.LastRequest!.ThemeKey);
        Assert.Equal("strava", renderer.LastRequest.AccentKey);
    }

    [Fact]
    public async Task Handle_PassesStravaFirstnameToRendererWhenAthleteIsKnown()
    {
        var athleteId = Guid.NewGuid();
        var badge = new Badge(athleteId, "abc123", "lucas");
        var renderer = new FakeRenderer("<svg/>");

        var handler = new GetBadgeSvgHandler(
            new FakeBadgeRepository(badge),
            new FakeAthleteRepository(new Athlete("Lucas", "Rodrigues")),
            renderer,
            new FakeDateTimeProvider(new DateTime(2026, 5, 7, 12, 0, 0, DateTimeKind.Utc)),
            new FakeDispatcher(BuildHeatmap(athleteId, "lucas", new DateOnly(2025, 5, 8), new DateOnly(2026, 5, 7))));

        await handler.Handle(new GetBadgeSvgQuery("abc123"), CancellationToken.None);

        Assert.Equal("Lucas", renderer.LastRequest!.StravaFirstname);
    }

    [Fact]
    public async Task Handle_LeavesStravaFirstnameNullWhenAthleteIsMissing()
    {
        var athleteId = Guid.NewGuid();
        var badge = new Badge(athleteId, "abc123", "lucas");
        var renderer = new FakeRenderer("<svg/>");

        var handler = new GetBadgeSvgHandler(
            new FakeBadgeRepository(badge),
            new FakeAthleteRepository(null),
            renderer,
            new FakeDateTimeProvider(new DateTime(2026, 5, 7, 12, 0, 0, DateTimeKind.Utc)),
            new FakeDispatcher(BuildHeatmap(athleteId, "lucas", new DateOnly(2025, 5, 8), new DateOnly(2026, 5, 7))));

        await handler.Handle(new GetBadgeSvgQuery("abc123"), CancellationToken.None);

        Assert.Null(renderer.LastRequest!.StravaFirstname);
    }

    [Fact]
    public async Task Handle_ThrowsBadgeNotFoundWhenSlugUnknown()
    {
        var handler = new GetBadgeSvgHandler(
            new FakeBadgeRepository(null),
            new FakeAthleteRepository(null),
            new FakeRenderer("<svg/>"),
            new FakeDateTimeProvider(DateTime.UtcNow),
            new FakeDispatcher(null));

        await Assert.ThrowsAsync<BadgeNotFoundException>(() =>
            handler.Handle(new GetBadgeSvgQuery("missing"), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ThrowsBadgeNotFoundWhenBadgeDisabled()
    {
        var badge = new Badge(Guid.NewGuid(), "abc123", "lucas");
        badge.Disable();

        var handler = new GetBadgeSvgHandler(
            new FakeBadgeRepository(badge),
            new FakeAthleteRepository(null),
            new FakeRenderer("<svg/>"),
            new FakeDateTimeProvider(DateTime.UtcNow),
            new FakeDispatcher(null));

        await Assert.ThrowsAsync<BadgeNotFoundException>(() =>
            handler.Handle(new GetBadgeSvgQuery("abc123"), CancellationToken.None));
    }

    private static Heatmap BuildHeatmap(Guid athleteId, string username, DateOnly from, DateOnly to)
    {
        return new Heatmap(
            username,
            athleteId,
            from,
            to,
            TotalGitHubContributions: 1,
            TotalStravaActivityDays: 0,
            new[] { new HeatmapDay(from, 1, GitHubContributionLevel.First, false) });
    }

    private sealed class FakeAthleteRepository : IAthleteRepository
    {
        private readonly Athlete? _athlete;

        public FakeAthleteRepository(Athlete? athlete)
        {
            _athlete = athlete;
        }

        public Task<Athlete?> GetByIdAsync(Guid athleteId, CancellationToken cancellationToken)
            => Task.FromResult(_athlete);

        public Task<bool> Exists(Guid athleteId, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task AddAsync(Athlete athlete, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task UpdateFromExternalAsync(Guid athleteId, Athlete externalAthlete, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task SaveChangesAsync(CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task MarkActivitySyncCompletedAsync(Guid athleteId, DateTimeOffset syncedAtUtc, CancellationToken cancellationToken)
            => throw new NotSupportedException();
    }

    private sealed class FakeBadgeRepository : IBadgeRepository
    {
        private readonly Badge? _badge;

        public FakeBadgeRepository(Badge? badge)
        {
            _badge = badge;
        }

        public Task<Badge?> GetBySlugAsync(string slug, CancellationToken cancellationToken)
            => Task.FromResult(_badge);

        public Task<Badge?> GetByAthleteIdAsync(Guid athleteId, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<bool> SlugExistsAsync(string slug, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task AddAsync(Badge badge, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task SaveChangesAsync(CancellationToken cancellationToken)
            => throw new NotSupportedException();
    }

    private sealed class FakeRenderer : IBadgeSvgRenderer
    {
        private readonly string _svg;

        public FakeRenderer(string svg)
        {
            _svg = svg;
        }

        public BadgeRenderRequest? LastRequest { get; private set; }

        public string Render(BadgeRenderRequest request)
        {
            LastRequest = request;
            return _svg;
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

    private sealed class FakeDispatcher : IRuigDispatcher
    {
        private readonly Heatmap? _heatmap;

        public FakeDispatcher(Heatmap? heatmap)
        {
            _heatmap = heatmap;
        }

        public GetHeatmapQuery? LastQuery { get; private set; }

        public Task<TResponse> Send<TResponse>(IRuigRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            if (request is GetHeatmapQuery query)
            {
                LastQuery = query;

                if (_heatmap is null)
                    throw new InvalidOperationException("No heatmap configured for this fake.");

                return Task.FromResult((TResponse)(object)_heatmap);
            }

            throw new NotSupportedException($"Unexpected request type {request.GetType().Name}");
        }
    }
}
