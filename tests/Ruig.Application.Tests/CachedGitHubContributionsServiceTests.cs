using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Ruig.Application.Common.Interfaces.GitHub;
using Ruig.Application.Common.Interfaces.GitHub.Models;
using Ruig.Infrastructure.GitHub;

namespace Ruig.Application.Tests;

public sealed class CachedGitHubContributionsServiceTests
{
    [Fact]
    public async Task GetContributionsAsync_CachesResultsForSameInputs()
    {
        var client = new RecordingClient(BuildCalendar("lucas", 5));
        var cache = new MemoryCache(new MemoryCacheOptions());
        var options = Options.Create(new GitHubOptions { CacheTtl = TimeSpan.FromMinutes(30) });

        var service = new CachedGitHubContributionsService(client, cache, options);

        var from = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var to = new DateTimeOffset(2026, 5, 5, 0, 0, 0, TimeSpan.Zero);

        var first = await service.GetContributionsAsync("lucas", from, to, CancellationToken.None);
        var second = await service.GetContributionsAsync("LUCAS", from, to, CancellationToken.None);

        Assert.Equal(1, client.CallCount);
        Assert.Same(first, second);
        Assert.Equal(5, first.TotalContributions);
    }

    [Fact]
    public async Task GetContributionsAsync_DistinctRangesAreCachedSeparately()
    {
        var client = new RecordingClient(BuildCalendar("lucas", 5));
        var cache = new MemoryCache(new MemoryCacheOptions());

        var service = new CachedGitHubContributionsService(
            client,
            cache,
            Options.Create(new GitHubOptions { CacheTtl = TimeSpan.FromMinutes(30) }));

        var from = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var firstTo = new DateTimeOffset(2026, 5, 5, 0, 0, 0, TimeSpan.Zero);
        var secondTo = new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero);

        await service.GetContributionsAsync("lucas", from, firstTo, CancellationToken.None);
        await service.GetContributionsAsync("lucas", from, secondTo, CancellationToken.None);

        Assert.Equal(2, client.CallCount);
    }

    private static GitHubContributionCalendar BuildCalendar(string username, int totalContributions)
    {
        return new GitHubContributionCalendar(
            username,
            totalContributions,
            new[]
            {
                new GitHubContributionDay(new DateOnly(2026, 5, 5), totalContributions, GitHubContributionLevel.Second)
            });
    }

    private sealed class RecordingClient : IGitHubContributionsClient
    {
        private readonly GitHubContributionCalendar _result;

        public RecordingClient(GitHubContributionCalendar result)
        {
            _result = result;
        }

        public int CallCount { get; private set; }

        public Task<GitHubContributionCalendar> GetContributionsAsync(
            string username,
            DateTimeOffset fromUtc,
            DateTimeOffset toUtc,
            CancellationToken cancellationToken)
        {
            CallCount++;
            return Task.FromResult(_result);
        }
    }
}
