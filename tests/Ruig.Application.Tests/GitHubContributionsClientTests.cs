using Microsoft.Extensions.Options;
using Ruig.Application.Common.Interfaces.GitHub.Models;
using Ruig.Infrastructure.GitHub;
using System.Net;
using System.Text;

namespace Ruig.Application.Tests;

public sealed class GitHubContributionsClientTests
{
    [Fact]
    public async Task GetContributionsAsync_PostsAuthorizedRequestAndMapsCalendar()
    {
        const string responseJson = """
            {
              "data": {
                "user": {
                  "contributionsCollection": {
                    "contributionCalendar": {
                      "totalContributions": 7,
                      "weeks": [
                        {
                          "contributionDays": [
                            { "date": "2026-04-30", "contributionCount": 0, "contributionLevel": "NONE" },
                            { "date": "2026-05-01", "contributionCount": 3, "contributionLevel": "FIRST_QUARTILE" }
                          ]
                        },
                        {
                          "contributionDays": [
                            { "date": "2026-05-02", "contributionCount": 4, "contributionLevel": "FOURTH_QUARTILE" }
                          ]
                        }
                      ]
                    }
                  }
                }
              }
            }
            """;

        var handler = new RecordingHandler(responseJson, HttpStatusCode.OK);
        using var httpClient = new HttpClient(handler);

        var options = Options.Create(new GitHubOptions
        {
            GraphQLUrl = "https://api.github.test/graphql",
            AccessToken = "ghp_test",
            UserAgent = "Ruig-Test"
        });

        var client = new GitHubContributionsClient(httpClient, options);

        var calendar = await client.GetContributionsAsync(
            "lucas",
            new DateTimeOffset(2026, 4, 30, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 5, 5, 0, 0, 0, TimeSpan.Zero),
            CancellationToken.None);

        Assert.Equal("lucas", calendar.Username);
        Assert.Equal(7, calendar.TotalContributions);
        Assert.Collection(
            calendar.Days,
            day =>
            {
                Assert.Equal(new DateOnly(2026, 4, 30), day.Date);
                Assert.Equal(0, day.ContributionCount);
                Assert.Equal(GitHubContributionLevel.None, day.Level);
            },
            day =>
            {
                Assert.Equal(new DateOnly(2026, 5, 1), day.Date);
                Assert.Equal(3, day.ContributionCount);
                Assert.Equal(GitHubContributionLevel.First, day.Level);
            },
            day =>
            {
                Assert.Equal(new DateOnly(2026, 5, 2), day.Date);
                Assert.Equal(4, day.ContributionCount);
                Assert.Equal(GitHubContributionLevel.Fourth, day.Level);
            });

        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal("https://api.github.test/graphql", request.RequestUri?.ToString());
        Assert.Equal("Bearer", request.Headers.Authorization?.Scheme);
        Assert.Equal("ghp_test", request.Headers.Authorization?.Parameter);
        Assert.Contains("Ruig-Test", request.Headers.UserAgent.ToString());
        Assert.NotNull(handler.LastBody);
        Assert.Contains("\"username\":\"lucas\"", handler.LastBody);
        Assert.Contains("2026-04-30T00:00:00Z", handler.LastBody);
        Assert.Contains("2026-05-05T00:00:00Z", handler.LastBody);
    }

    [Fact]
    public async Task GetContributionsAsync_ThrowsOnGraphQLErrors()
    {
        const string responseJson = """
            {
              "errors": [ { "message": "Could not resolve to a User with the login of 'ghost'." } ]
            }
            """;

        var handler = new RecordingHandler(responseJson, HttpStatusCode.OK);
        using var httpClient = new HttpClient(handler);

        var options = Options.Create(new GitHubOptions
        {
            AccessToken = "ghp_test"
        });

        var client = new GitHubContributionsClient(httpClient, options);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => client.GetContributionsAsync(
            "ghost",
            new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 12, 31, 0, 0, 0, TimeSpan.Zero),
            CancellationToken.None));

        Assert.Contains("Could not resolve", ex.Message);
    }

    [Fact]
    public async Task GetContributionsAsync_ThrowsWhenAccessTokenMissing()
    {
        var handler = new RecordingHandler("{}", HttpStatusCode.OK);
        using var httpClient = new HttpClient(handler);

        var client = new GitHubContributionsClient(
            httpClient,
            Options.Create(new GitHubOptions { AccessToken = "" }));

        await Assert.ThrowsAsync<InvalidOperationException>(() => client.GetContributionsAsync(
            "lucas",
            DateTimeOffset.UnixEpoch,
            DateTimeOffset.UnixEpoch.AddDays(1),
            CancellationToken.None));
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        private readonly string _response;
        private readonly HttpStatusCode _statusCode;

        public RecordingHandler(string response, HttpStatusCode statusCode)
        {
            _response = response;
            _statusCode = statusCode;
        }

        public List<HttpRequestMessage> Requests { get; } = new();
        public string? LastBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.Content is not null)
                LastBody = await request.Content.ReadAsStringAsync(cancellationToken);

            Requests.Add(CloneRequest(request));

            return new HttpResponseMessage(_statusCode)
            {
                Content = new StringContent(_response, Encoding.UTF8, "application/json")
            };
        }

        private static HttpRequestMessage CloneRequest(HttpRequestMessage request)
        {
            var clone = new HttpRequestMessage(request.Method, request.RequestUri);

            foreach (var header in request.Headers)
            {
                clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            return clone;
        }
    }
}
