using System.Collections.Generic;

namespace Ruig.Application.Common.Interfaces.GitHub.Models
{
    public sealed record GitHubContributionCalendar(
        string Username,
        int TotalContributions,
        IReadOnlyList<GitHubContributionDay> Days);
}
