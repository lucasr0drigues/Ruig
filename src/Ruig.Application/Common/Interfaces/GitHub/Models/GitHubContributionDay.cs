using System;

namespace Ruig.Application.Common.Interfaces.GitHub.Models
{
    public sealed record GitHubContributionDay(
        DateOnly Date,
        int ContributionCount,
        GitHubContributionLevel Level);
}
