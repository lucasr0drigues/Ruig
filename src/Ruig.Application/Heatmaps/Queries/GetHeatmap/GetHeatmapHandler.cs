using MediatR;
using Ruig.Application.Common.Interfaces;
using Ruig.Application.Common.Interfaces.GitHub;
using Ruig.Application.Common.Interfaces.GitHub.Models;
using Ruig.Application.Heatmaps.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Ruig.Application.Heatmaps.Queries.GetHeatmap
{
    public sealed class GetHeatmapHandler : IRequestHandler<GetHeatmapQuery, Heatmap>
    {
        private readonly IGitHubContributionsService _gitHubContributions;
        private readonly IActivityRepository _activityRepository;

        public GetHeatmapHandler(
            IGitHubContributionsService gitHubContributions,
            IActivityRepository activityRepository)
        {
            _gitHubContributions = gitHubContributions;
            _activityRepository = activityRepository;
        }

        public async Task<Heatmap> Handle(GetHeatmapQuery request, CancellationToken cancellationToken)
        {
            var fromUtc = new DateTimeOffset(request.From.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc));
            var toUtc = new DateTimeOffset(request.To.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc));

            var calendar = await _gitHubContributions.GetContributionsAsync(
                request.GitHubUsername,
                fromUtc,
                toUtc,
                cancellationToken);

            var stravaDates = await _activityRepository.GetActiveLocalDatesAsync(
                request.AthleteId,
                request.From,
                request.To,
                cancellationToken);

            var stravaDateSet = new HashSet<DateOnly>(stravaDates);

            var contributionsByDate = new Dictionary<DateOnly, GitHubContributionDay>();
            foreach (var day in calendar.Days)
            {
                contributionsByDate[day.Date] = day;
            }

            var days = new List<HeatmapDay>();

            for (var date = request.From; date <= request.To; date = date.AddDays(1))
            {
                contributionsByDate.TryGetValue(date, out var contribution);

                days.Add(new HeatmapDay(
                    date,
                    contribution?.ContributionCount ?? 0,
                    contribution?.Level ?? GitHubContributionLevel.None,
                    stravaDateSet.Contains(date)));
            }

            return new Heatmap(
                request.GitHubUsername,
                request.AthleteId,
                request.From,
                request.To,
                calendar.TotalContributions,
                stravaDateSet.Count,
                days);
        }
    }
}
