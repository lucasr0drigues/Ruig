using Ruig.Infrastructure.Strava;
using System.Net;
using System.Text;

namespace Ruig.Application.Tests;

public sealed class StravaApiClientTests
{
    [Fact]
    public async Task ListAthleteActivitiesAsync_PagesUntilEmptyAndMapsActivities()
    {
        var handler = new QueueResponseHandler(
            """
            [
              {
                "id": 11,
                "name": "Morning Run",
                "sport_type": "Run",
                "distance": 5000.5,
                "moving_time": 1800,
                "elapsed_time": 1900,
                "total_elevation_gain": 42.2,
                "start_date": "2026-05-01T10:00:00Z",
                "start_date_local": "2026-05-01T07:00:00Z",
                "utc_offset": -10800,
                "timezone": "(GMT-03:00) America/Sao_Paulo",
                "device_name": "Watch",
                "private": false,
                "visibility": "everyone",
                "map": {
                  "id": "a11",
                  "summary_polyline": "abc"
                }
              }
            ]
            """,
            """
            [
              {
                "id": 12,
                "name": "Evening Ride",
                "type": "Ride",
                "distance": 10000,
                "start_date": "2026-05-02T20:00:00Z"
              }
            ]
            """,
            "[]");

        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://strava.test/api/v3/")
        };

        var client = new StravaApiClient(httpClient);

        var activities = await client.ListAthleteActivitiesAsync(
            "access-token",
            DateTimeOffset.FromUnixTimeSeconds(100),
            DateTimeOffset.FromUnixTimeSeconds(200),
            CancellationToken.None);

        Assert.Equal(2, activities.Count);
        Assert.Equal(11, activities[0].Id);
        Assert.Equal("Run", activities[0].SportType);
        Assert.Equal("abc", activities[0].Map?.SummaryPolyline);
        Assert.Equal("Ride", activities[1].SportType);

        Assert.Collection(
            handler.Requests,
            request =>
            {
                Assert.Equal("Bearer", request.Headers.Authorization?.Scheme);
                Assert.Equal("access-token", request.Headers.Authorization?.Parameter);
                Assert.EndsWith("/athlete/activities?page=1&per_page=200&after=100&before=200", request.RequestUri?.ToString());
            },
            request => Assert.EndsWith("/athlete/activities?page=2&per_page=200&after=100&before=200", request.RequestUri?.ToString()),
            request => Assert.EndsWith("/athlete/activities?page=3&per_page=200&after=100&before=200", request.RequestUri?.ToString()));
    }

    [Fact]
    public async Task GetActivityAsync_MapsSingleActivity()
    {
        var handler = new QueueResponseHandler(
            """
            {
              "id": 99,
              "name": "Lunch Walk",
              "sport_type": "Walk",
              "start_date": "2026-05-03T15:00:00Z"
            }
            """);

        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://strava.test/api/v3/")
        };

        var client = new StravaApiClient(httpClient);

        var activity = await client.GetActivityAsync("access-token", 99, CancellationToken.None);

        Assert.Equal(99, activity.Id);
        Assert.Equal("Lunch Walk", activity.Name);
        Assert.Equal("Walk", activity.SportType);
        Assert.Single(handler.Requests);
        Assert.EndsWith("/activities/99", handler.Requests[0].RequestUri?.ToString());
    }

    private sealed class QueueResponseHandler : HttpMessageHandler
    {
        private readonly Queue<string> _responses;

        public QueueResponseHandler(params string[] responses)
        {
            _responses = new Queue<string>(responses);
        }

        public List<HttpRequestMessage> Requests { get; } = new();

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(CloneRequest(request));

            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(_responses.Dequeue(), Encoding.UTF8, "application/json")
            };

            return Task.FromResult(response);
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
