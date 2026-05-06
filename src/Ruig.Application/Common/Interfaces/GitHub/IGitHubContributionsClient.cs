using Ruig.Application.Common.Interfaces.GitHub.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Ruig.Application.Common.Interfaces.GitHub
{
    public interface IGitHubContributionsClient
    {
        Task<GitHubContributionCalendar> GetContributionsAsync(
            string username,
            DateTimeOffset fromUtc,
            DateTimeOffset toUtc,
            CancellationToken cancellationToken);
    }
}
