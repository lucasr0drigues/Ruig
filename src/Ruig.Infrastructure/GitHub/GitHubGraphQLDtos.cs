using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Ruig.Infrastructure.GitHub
{
    internal sealed class GitHubGraphQLResponse
    {
        [JsonPropertyName("data")]
        public GitHubGraphQLData? Data { get; set; }

        [JsonPropertyName("errors")]
        public List<GitHubGraphQLError>? Errors { get; set; }
    }

    internal sealed class GitHubGraphQLError
    {
        [JsonPropertyName("message")]
        public string? Message { get; set; }
    }

    internal sealed class GitHubGraphQLData
    {
        [JsonPropertyName("user")]
        public GitHubGraphQLUser? User { get; set; }
    }

    internal sealed class GitHubGraphQLUser
    {
        [JsonPropertyName("contributionsCollection")]
        public GitHubContributionsCollectionDto? ContributionsCollection { get; set; }
    }

    internal sealed class GitHubContributionsCollectionDto
    {
        [JsonPropertyName("contributionCalendar")]
        public GitHubContributionCalendarDto? ContributionCalendar { get; set; }
    }

    internal sealed class GitHubContributionCalendarDto
    {
        [JsonPropertyName("totalContributions")]
        public int TotalContributions { get; set; }

        [JsonPropertyName("weeks")]
        public List<GitHubContributionWeekDto> Weeks { get; set; } = new();
    }

    internal sealed class GitHubContributionWeekDto
    {
        [JsonPropertyName("contributionDays")]
        public List<GitHubContributionDayDto> ContributionDays { get; set; } = new();
    }

    internal sealed class GitHubContributionDayDto
    {
        [JsonPropertyName("date")]
        public string Date { get; set; } = string.Empty;

        [JsonPropertyName("contributionCount")]
        public int ContributionCount { get; set; }

        [JsonPropertyName("contributionLevel")]
        public string ContributionLevel { get; set; } = string.Empty;
    }
}
