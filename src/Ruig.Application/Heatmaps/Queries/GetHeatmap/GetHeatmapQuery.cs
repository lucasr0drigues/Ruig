using Ruig.Application.Common.Dispatching;
using Ruig.Application.Heatmaps.Models;
using System;

namespace Ruig.Application.Heatmaps.Queries.GetHeatmap
{
    public sealed record GetHeatmapQuery(
        string GitHubUsername,
        Guid AthleteId,
        DateOnly From,
        DateOnly To) : IRuigRequest<Heatmap>;
}
