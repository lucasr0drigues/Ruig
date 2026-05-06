using System;
using System.Collections.Generic;

namespace Ruig.Application.Heatmaps.Models
{
    public sealed record Heatmap(
        string GitHubUsername,
        Guid AthleteId,
        DateOnly From,
        DateOnly To,
        int TotalGitHubContributions,
        int TotalStravaActivityDays,
        IReadOnlyList<HeatmapDay> Days);
}
