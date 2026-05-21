using Ruig.Application.Activities.Commands.ListActivitiesByAthlete;
using Ruig.Application.Common.Interfaces;
using Ruig.Application.Common.Interfaces.GitHub;
using Ruig.Application.Common.Interfaces.GitHub.Models;
using Ruig.Application.Common.Models;
using Ruig.Application.Heatmaps.Queries.GetHeatmap;
using Ruig.Domain.Entities;

namespace Ruig.Application.Tests;

public sealed class GetHeatmapHandlerTests
{
    [Fact]
    public async Task Handle_CombinesGitHubContributionsAndStravaDays()
    {
        var athleteId = Guid.NewGuid();
        var from = new DateOnly(2026, 5, 1);
        var to = new DateOnly(2026, 5, 5);

        var calendar = new GitHubContributionCalendar(
            "lucas",
            10,
            new[]
            {
                new GitHubContributionDay(new DateOnly(2026, 5, 1), 0, GitHubContributionLevel.None),
                new GitHubContributionDay(new DateOnly(2026, 5, 2), 4, GitHubContributionLevel.Second),
                new GitHubContributionDay(new DateOnly(2026, 5, 3), 6, GitHubContributionLevel.Fourth),
                new GitHubContributionDay(new DateOnly(2026, 5, 5), 0, GitHubContributionLevel.None)
            });

        var githubService = new FakeGitHubService(calendar);

        var stravaDays = new List<DateOnly>
        {
            new(2026, 5, 2),
            new(2026, 5, 4),
            new(2026, 5, 4),
        };

        var activityRepository = new FakeActivityRepository(stravaDays);

        var handler = new GetHeatmapHandler(githubService, activityRepository);

        var heatmap = await handler.Handle(
            new GetHeatmapQuery("lucas", athleteId, from, to),
            CancellationToken.None);

        Assert.Equal("lucas", heatmap.GitHubUsername);
        Assert.Equal(athleteId, heatmap.AthleteId);
        Assert.Equal(from, heatmap.From);
        Assert.Equal(to, heatmap.To);
        Assert.Equal(10, heatmap.TotalGitHubContributions);
        Assert.Equal(2, heatmap.TotalStravaActivityDays);
        Assert.Equal(5, heatmap.Days.Count);

        Assert.Collection(
            heatmap.Days,
            day =>
            {
                Assert.Equal(new DateOnly(2026, 5, 1), day.Date);
                Assert.Equal(0, day.GitHubContributionCount);
                Assert.Equal(GitHubContributionLevel.None, day.GitHubLevel);
                Assert.False(day.HasStravaActivity);
            },
            day =>
            {
                Assert.Equal(new DateOnly(2026, 5, 2), day.Date);
                Assert.Equal(4, day.GitHubContributionCount);
                Assert.Equal(GitHubContributionLevel.Second, day.GitHubLevel);
                Assert.True(day.HasStravaActivity);
            },
            day =>
            {
                Assert.Equal(new DateOnly(2026, 5, 3), day.Date);
                Assert.Equal(6, day.GitHubContributionCount);
                Assert.Equal(GitHubContributionLevel.Fourth, day.GitHubLevel);
                Assert.False(day.HasStravaActivity);
            },
            day =>
            {
                Assert.Equal(new DateOnly(2026, 5, 4), day.Date);
                Assert.Equal(0, day.GitHubContributionCount);
                Assert.Equal(GitHubContributionLevel.None, day.GitHubLevel);
                Assert.True(day.HasStravaActivity);
            },
            day =>
            {
                Assert.Equal(new DateOnly(2026, 5, 5), day.Date);
                Assert.Equal(0, day.GitHubContributionCount);
                Assert.Equal(GitHubContributionLevel.None, day.GitHubLevel);
                Assert.False(day.HasStravaActivity);
            });

        Assert.Equal(athleteId, activityRepository.LastAthleteId);
        Assert.Equal(from, activityRepository.LastFrom);
        Assert.Equal(to, activityRepository.LastTo);
    }

    [Fact]
    public async Task Handle_RequestsContributionsCoveringFullRange()
    {
        var githubService = new FakeGitHubService(new GitHubContributionCalendar("lucas", 0, Array.Empty<GitHubContributionDay>()));
        var activityRepository = new FakeActivityRepository(new List<DateOnly>());

        var handler = new GetHeatmapHandler(githubService, activityRepository);

        await handler.Handle(
            new GetHeatmapQuery("lucas", Guid.NewGuid(), new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31)),
            CancellationToken.None);

        Assert.NotNull(githubService.LastFromUtc);
        Assert.NotNull(githubService.LastToUtc);
        Assert.Equal(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero), githubService.LastFromUtc!.Value);
        Assert.Equal(new DateOnly(2026, 12, 31), DateOnly.FromDateTime(githubService.LastToUtc!.Value.UtcDateTime));
    }

    private sealed class FakeGitHubService : IGitHubContributionsService
    {
        private readonly GitHubContributionCalendar _calendar;

        public FakeGitHubService(GitHubContributionCalendar calendar)
        {
            _calendar = calendar;
        }

        public DateTimeOffset? LastFromUtc { get; private set; }
        public DateTimeOffset? LastToUtc { get; private set; }

        public Task<GitHubContributionCalendar> GetContributionsAsync(
            string username,
            DateTimeOffset fromUtc,
            DateTimeOffset toUtc,
            CancellationToken cancellationToken)
        {
            LastFromUtc = fromUtc;
            LastToUtc = toUtc;

            return Task.FromResult(_calendar);
        }
    }

    private sealed class FakeActivityRepository : IActivityRepository
    {
        private readonly IReadOnlyList<DateOnly> _activeDates;

        public FakeActivityRepository(IReadOnlyList<DateOnly> activeDates)
        {
            _activeDates = activeDates;
        }

        public Guid? LastAthleteId { get; private set; }
        public DateOnly? LastFrom { get; private set; }
        public DateOnly? LastTo { get; private set; }

        public Task<IReadOnlyList<DateOnly>> GetActiveLocalDatesAsync(
            Guid athleteId,
            DateOnly fromInclusive,
            DateOnly toInclusive,
            CancellationToken cancellationToken)
        {
            LastAthleteId = athleteId;
            LastFrom = fromInclusive;
            LastTo = toInclusive;

            return Task.FromResult(_activeDates);
        }

        public Task<Activity?> GetByIdAsync(Guid activityId, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<PagedResult<ListActivitiesByAthleteDto>> ListByAthleteIdAsync(
            Guid athleteId,
            int page,
            int pageSize,
            DateTime? fromUtc,
            DateTime? toUtc,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<bool> AddAsync(Activity activity, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task UpsertAsync(Activity activity, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task ReplaceLocalDatesAsync(
            Guid athleteId,
            DateOnly fromInclusive,
            DateOnly toInclusive,
            IEnumerable<DateOnly> localDates,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task SaveChangesAsync(CancellationToken cancellationToken)
            => throw new NotSupportedException();
    }
}
