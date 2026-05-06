using MediatR;
using Ruig.Application.Badges.Queries.GetBadgeSvg;
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
        var mediator = new FakeMediator(BuildHeatmap(athleteId, "lucas", new DateOnly(2025, 5, 6), new DateOnly(2026, 5, 5)));

        var handler = new GetBadgeSvgHandler(badgeRepository, renderer, dateTimeProvider, mediator);

        var result = await handler.Handle(new GetBadgeSvgQuery("abc123"), CancellationToken.None);

        Assert.Equal("abc123", result.Slug);
        Assert.Equal("<svg/>", result.Svg);
        Assert.Equal(new DateOnly(2026, 5, 5), result.RangeTo);
        Assert.Equal(new DateOnly(2025, 5, 6), result.RangeFrom);
        Assert.Equal(new DateTimeOffset(2026, 5, 5, 12, 0, 0, TimeSpan.Zero), result.GeneratedAtUtc);

        Assert.NotNull(mediator.LastQuery);
        Assert.Equal(athleteId, mediator.LastQuery!.AthleteId);
        Assert.Equal("lucas", mediator.LastQuery.GitHubUsername);
        Assert.Equal(new DateOnly(2025, 5, 6), mediator.LastQuery.From);
        Assert.Equal(new DateOnly(2026, 5, 5), mediator.LastQuery.To);
    }

    [Fact]
    public async Task Handle_ThrowsBadgeNotFoundWhenSlugUnknown()
    {
        var handler = new GetBadgeSvgHandler(
            new FakeBadgeRepository(null),
            new FakeRenderer("<svg/>"),
            new FakeDateTimeProvider(DateTime.UtcNow),
            new FakeMediator(null));

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
            new FakeRenderer("<svg/>"),
            new FakeDateTimeProvider(DateTime.UtcNow),
            new FakeMediator(null));

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

        public string Render(Heatmap heatmap) => _svg;
    }

    private sealed class FakeDateTimeProvider : IDateTimeProvider
    {
        public FakeDateTimeProvider(DateTime utcNow)
        {
            UtcNow = utcNow;
        }

        public DateTime UtcNow { get; }
    }

    private sealed class FakeMediator : IMediator
    {
        private readonly Heatmap? _heatmap;

        public FakeMediator(Heatmap? heatmap)
        {
            _heatmap = heatmap;
        }

        public GetHeatmapQuery? LastQuery { get; private set; }

        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
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

        public Task<object?> Send(object request, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default) where TRequest : IRequest
            => throw new NotSupportedException();

        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task Publish(object notification, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default) where TNotification : INotification
            => throw new NotSupportedException();
    }
}
