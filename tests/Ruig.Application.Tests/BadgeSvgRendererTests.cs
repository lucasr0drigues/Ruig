using Ruig.Application.Common.Interfaces;
using Ruig.Application.Common.Interfaces.GitHub.Models;
using Ruig.Application.Heatmaps.Models;
using Ruig.Infrastructure.Badges;

namespace Ruig.Application.Tests;

public sealed class BadgeSvgRendererTests
{
    [Fact]
    public void Render_UsesPurplePaletteByDefaultAndStravaOrangeStroke()
    {
        var renderer = new BadgeSvgRenderer();

        var heatmap = BuildSampleHeatmap();

        var svg = renderer.Render(new BadgeRenderRequest(heatmap, "lucas", "purple", "strava"));

        Assert.StartsWith("<svg", svg);
        Assert.EndsWith("</svg>", svg);
        Assert.Contains("xmlns=\"http://www.w3.org/2000/svg\"", svg);

        // Purple palette colors
        Assert.Contains("fill=\"#26262e\"", svg);
        Assert.Contains("fill=\"#4c1d95\"", svg);
        Assert.Contains("fill=\"#7c3aed\"", svg);
        Assert.Contains("fill=\"#a78bfa\"", svg);
        Assert.Contains("fill=\"#c4b5fd\"", svg);

        // Strava orange stroke
        Assert.Contains("stroke=\"#fc4c02\"", svg);
    }

    [Fact]
    public void Render_AppliesSelectedThemeAndAccent()
    {
        var renderer = new BadgeSvgRenderer();
        var heatmap = BuildSampleHeatmap();

        var svg = renderer.Render(new BadgeRenderRequest(heatmap, "lucas", "green", "cyan"));

        Assert.Contains("fill=\"#39d353\"", svg); // green L4
        Assert.Contains("stroke=\"#22d3ee\"", svg); // cyan accent
        Assert.DoesNotContain("stroke=\"#fc4c02\"", svg);
    }

    [Fact]
    public void Render_FallsBackToDefaultsWhenKeysAreInvalid()
    {
        var renderer = new BadgeSvgRenderer();
        var heatmap = BuildSampleHeatmap();

        var svg = renderer.Render(new BadgeRenderRequest(heatmap, "lucas", "not-a-theme", "not-an-accent"));

        // Default = purple + strava
        Assert.Contains("fill=\"#7c3aed\"", svg);
        Assert.Contains("stroke=\"#fc4c02\"", svg);
    }

    [Fact]
    public void Render_IncludesUsernameInFooterAndAriaLabel()
    {
        var renderer = new BadgeSvgRenderer();
        var heatmap = BuildYearHeatmap();

        var svg = renderer.Render(new BadgeRenderRequest(heatmap, "lucasr0drigues", "purple", "strava"));

        Assert.Contains("aria-label=\"Ruig heatmap badge for lucasr0drigues\"", svg);
        Assert.Contains("GitHub: ", svg);
        Assert.Contains(">lucasr0drigues<", svg);
    }

    private static Heatmap BuildYearHeatmap()
    {
        var from = new DateOnly(2025, 5, 8);
        var to = new DateOnly(2026, 5, 7);

        var days = new List<HeatmapDay>();
        for (var date = from; date <= to; date = date.AddDays(1))
        {
            days.Add(new HeatmapDay(date, 0, GitHubContributionLevel.None, false));
        }

        return new Heatmap("lucasr0drigues", Guid.NewGuid(), from, to, 0, 0, days);
    }

    [Fact]
    public void Render_IncludesLessAndMoreLegendLabels()
    {
        var renderer = new BadgeSvgRenderer();
        var heatmap = BuildSampleHeatmap();

        var svg = renderer.Render(new BadgeRenderRequest(heatmap, "lucas", "purple", "strava"));

        Assert.Contains(">Less<", svg);
        Assert.Contains(">More<", svg);
        Assert.Contains(">Strava<", svg);
    }

    [Fact]
    public void Render_HtmlEncodesSuspiciousUsername()
    {
        var renderer = new BadgeSvgRenderer();
        var heatmap = BuildSampleHeatmap();

        var svg = renderer.Render(new BadgeRenderRequest(heatmap, "<script>x</script>", "purple", "strava"));

        Assert.DoesNotContain("<script>", svg);
        Assert.Contains("&lt;script&gt;", svg);
    }

    private static Heatmap BuildSampleHeatmap()
    {
        return new Heatmap(
            "lucas",
            Guid.NewGuid(),
            new DateOnly(2026, 5, 1),
            new DateOnly(2026, 5, 7),
            TotalGitHubContributions: 7,
            TotalStravaActivityDays: 1,
            new[]
            {
                new HeatmapDay(new DateOnly(2026, 5, 1), 0, GitHubContributionLevel.None, false),
                new HeatmapDay(new DateOnly(2026, 5, 2), 4, GitHubContributionLevel.First, true),
                new HeatmapDay(new DateOnly(2026, 5, 3), 1, GitHubContributionLevel.Second, false),
                new HeatmapDay(new DateOnly(2026, 5, 4), 0, GitHubContributionLevel.None, false),
                new HeatmapDay(new DateOnly(2026, 5, 5), 0, GitHubContributionLevel.None, false),
                new HeatmapDay(new DateOnly(2026, 5, 6), 2, GitHubContributionLevel.Third, false),
                new HeatmapDay(new DateOnly(2026, 5, 7), 6, GitHubContributionLevel.Fourth, false),
            });
    }
}
