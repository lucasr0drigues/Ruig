using Ruig.Application.Common.Interfaces.GitHub.Models;
using System;

namespace Ruig.Application.Heatmaps.Models
{
    public sealed record HeatmapDay(
        DateOnly Date,
        int GitHubContributionCount,
        GitHubContributionLevel GitHubLevel,
        bool HasStravaActivity);
}
