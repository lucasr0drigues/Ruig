using Ruig.Application.Common.Interfaces.GitHub.Models;
using Ruig.Application.Heatmaps.Models;
using Ruig.Infrastructure.Badges;

namespace Ruig.Application.Tests;

public sealed class BadgeSvgRendererTests
{
    [Fact]
    public void Render_ProducesSvgWithCellPerDayAndStravaStrokeForActiveDays()
    {
        var renderer = new BadgeSvgRenderer();

        var heatmap = new Heatmap(
            "lucas",
            Guid.NewGuid(),
            new DateOnly(2026, 5, 1),
            new DateOnly(2026, 5, 7),
            TotalGitHubContributions: 7,
            TotalStravaActivityDays: 1,
            new[]
            {
                new HeatmapDay(new DateOnly(2026, 5, 1), 0, GitHubContributionLevel.None, false),
                new HeatmapDay(new DateOnly(2026, 5, 2), 4, GitHubContributionLevel.Second, true),
                new HeatmapDay(new DateOnly(2026, 5, 3), 1, GitHubContributionLevel.First, false),
                new HeatmapDay(new DateOnly(2026, 5, 4), 0, GitHubContributionLevel.None, false),
                new HeatmapDay(new DateOnly(2026, 5, 5), 0, GitHubContributionLevel.None, false),
                new HeatmapDay(new DateOnly(2026, 5, 6), 2, GitHubContributionLevel.Third, false),
                new HeatmapDay(new DateOnly(2026, 5, 7), 6, GitHubContributionLevel.Fourth, false),
            });

        var svg = renderer.Render(heatmap);

        Assert.StartsWith("<svg", svg);
        Assert.EndsWith("</svg>", svg);
        Assert.Contains("xmlns=\"http://www.w3.org/2000/svg\"", svg);
        Assert.Contains("role=\"img\"", svg);

        Assert.Contains("fill=\"#ebedf0\"", svg);
        Assert.Contains("fill=\"#9be9a8\"", svg);
        Assert.Contains("fill=\"#40c463\"", svg);
        Assert.Contains("fill=\"#30a14e\"", svg);
        Assert.Contains("fill=\"#216e39\"", svg);

        Assert.Contains("stroke=\"#fc4c02\"", svg);
        Assert.Single(System.Text.RegularExpressions.Regex.Matches(svg, "stroke=\"#fc4c02\""));

        Assert.Contains("<title>2026-05-02: 4 GitHub, Strava activity</title>", svg);
        Assert.Contains("<title>2026-05-01: 0 GitHub, no Strava</title>", svg);

        Assert.Contains("7 contributions", svg);
        Assert.Contains("1 active days", svg);
    }

    [Fact]
    public void Render_PadsGridToFullWeeksUsingNeutralFill()
    {
        var renderer = new BadgeSvgRenderer();

        var heatmap = new Heatmap(
            "lucas",
            Guid.NewGuid(),
            new DateOnly(2026, 5, 4),
            new DateOnly(2026, 5, 4),
            TotalGitHubContributions: 0,
            TotalStravaActivityDays: 0,
            new[]
            {
                new HeatmapDay(new DateOnly(2026, 5, 4), 0, GitHubContributionLevel.None, false)
            });

        var svg = renderer.Render(heatmap);

        var rectMatches = System.Text.RegularExpressions.Regex.Matches(svg, "<rect ");
        Assert.True(rectMatches.Count >= 8, $"Expected at least 8 rects (1 background + 7 days), got {rectMatches.Count}");
    }
}
