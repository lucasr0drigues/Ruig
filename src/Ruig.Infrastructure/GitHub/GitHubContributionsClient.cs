using Microsoft.Extensions.Options;
using Ruig.Application.Common.Interfaces.GitHub;
using Ruig.Application.Common.Interfaces.GitHub.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Ruig.Infrastructure.GitHub
{
    public sealed class GitHubContributionsClient : IGitHubContributionsClient
    {
        private const string ContributionsQuery = @"query($username: String!, $from: DateTime!, $to: DateTime!) {
  user(login: $username) {
    contributionsCollection(from: $from, to: $to) {
      contributionCalendar {
        totalContributions
        weeks {
          contributionDays {
            date
            contributionCount
            contributionLevel
          }
        }
      }
    }
  }
}";

        private readonly HttpClient _httpClient;
        private readonly GitHubOptions _options;

        public GitHubContributionsClient(HttpClient httpClient, IOptions<GitHubOptions> options)
        {
            _httpClient = httpClient;
            _options = options.Value;
        }

        public async Task<GitHubContributionCalendar> GetContributionsAsync(
            string username,
            DateTimeOffset fromUtc,
            DateTimeOffset toUtc,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(username))
                throw new ArgumentException("GitHub username is required", nameof(username));

            if (string.IsNullOrWhiteSpace(_options.AccessToken))
                throw new InvalidOperationException("GitHub access token is not configured.");

            if (toUtc < fromUtc)
                throw new ArgumentException("'toUtc' must be greater than or equal to 'fromUtc'.", nameof(toUtc));

            var requestBody = new
            {
                query = ContributionsQuery,
                variables = new
                {
                    username,
                    from = fromUtc.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture),
                    to = toUtc.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture)
                }
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, _options.GraphQLUrl)
            {
                Content = JsonContent.Create(requestBody)
            };

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.AccessToken);

            if (!string.IsNullOrWhiteSpace(_options.UserAgent))
                request.Headers.UserAgent.ParseAdd(_options.UserAgent);

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var payload = await response.Content.ReadFromJsonAsync<GitHubGraphQLResponse>(
                cancellationToken: cancellationToken);

            if (payload is null)
                throw new InvalidOperationException("GitHub GraphQL response was empty.");

            if (payload.Errors is { Count: > 0 })
            {
                var message = string.Join("; ", payload.Errors.Select(e => e.Message ?? "unknown"));
                throw new InvalidOperationException($"GitHub GraphQL returned errors: {message}");
            }

            var calendar = payload.Data?.User?.ContributionsCollection?.ContributionCalendar
                ?? throw new InvalidOperationException(
                    $"GitHub GraphQL response did not contain a contribution calendar for user '{username}'.");

            var days = new List<GitHubContributionDay>();

            foreach (var week in calendar.Weeks)
            {
                foreach (var day in week.ContributionDays)
                {
                    if (!DateOnly.TryParseExact(day.Date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
                        continue;

                    days.Add(new GitHubContributionDay(
                        date,
                        day.ContributionCount,
                        ParseLevel(day.ContributionLevel)));
                }
            }

            days.Sort((a, b) => a.Date.CompareTo(b.Date));

            return new GitHubContributionCalendar(username, calendar.TotalContributions, days);
        }

        private static GitHubContributionLevel ParseLevel(string? value)
        {
            return value?.ToUpperInvariant() switch
            {
                "NONE" => GitHubContributionLevel.None,
                "FIRST_QUARTILE" => GitHubContributionLevel.First,
                "SECOND_QUARTILE" => GitHubContributionLevel.Second,
                "THIRD_QUARTILE" => GitHubContributionLevel.Third,
                "FOURTH_QUARTILE" => GitHubContributionLevel.Fourth,
                _ => GitHubContributionLevel.None
            };
        }
    }
}
