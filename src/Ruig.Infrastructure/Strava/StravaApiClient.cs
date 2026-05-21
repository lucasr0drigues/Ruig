using Ruig.Application.Common.Interfaces.Strava;
using Ruig.Application.Common.Interfaces.Strava.Models;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Ruig.Infrastructure.Strava
{
    public sealed class StravaApiClient : IStravaApiClient
    {
        private const int MaxPerPage = 200;

        private readonly HttpClient _httpClient;

        public StravaApiClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<StravaAthleteResponse> GetCurrentAthleteAsync(string accessToken, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(accessToken))
                throw new ArgumentException("Strava access token is required", nameof(accessToken));

            using var request = CreateAuthorizedRequest(HttpMethod.Get, "athlete", accessToken);

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var dto = await response.Content.ReadFromJsonAsync<AthleteDto>(cancellationToken);

            if (dto is null)
                throw new InvalidOperationException("Strava athlete response was empty");

            return new StravaAthleteResponse(
                dto.Id,
                dto.FirstName,
                dto.LastName);
        }

        public async Task<IReadOnlyList<StravaActivityResponse>> ListAthleteActivitiesAsync(
            string accessToken,
            DateTimeOffset? afterUtc,
            DateTimeOffset? beforeUtc,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(accessToken))
                throw new ArgumentException("Strava access token is required", nameof(accessToken));

            var activities = new List<StravaActivityResponse>();
            var page = 1;

            while (true)
            {
                var url = BuildActivitiesUrl(afterUtc, beforeUtc, page);
                using var request = CreateAuthorizedRequest(HttpMethod.Get, url, accessToken);

                using var response = await _httpClient.SendAsync(request, cancellationToken);
                response.EnsureSuccessStatusCode();

                var dtos = await response.Content.ReadFromJsonAsync<List<ActivityDto>>(cancellationToken);

                if (dtos is null)
                    throw new InvalidOperationException("Strava activities response was empty");

                if (dtos.Count == 0)
                    break;

                activities.AddRange(dtos.Select(MapActivity));
                page++;
            }

            return activities;
        }

        public async Task<StravaActivityResponse> GetActivityAsync(
            string accessToken,
            long activityId,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(accessToken))
                throw new ArgumentException("Strava access token is required", nameof(accessToken));

            using var request = CreateAuthorizedRequest(HttpMethod.Get, $"activities/{activityId}", accessToken);

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var dto = await response.Content.ReadFromJsonAsync<ActivityDto>(cancellationToken);

            if (dto is null)
                throw new InvalidOperationException("Strava activity response was empty");

            return MapActivity(dto);
        }

        private static HttpRequestMessage CreateAuthorizedRequest(HttpMethod method, string url, string accessToken)
        {
            var request = new HttpRequestMessage(method, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            return request;
        }

        private static string BuildActivitiesUrl(DateTimeOffset? afterUtc, DateTimeOffset? beforeUtc, int page)
        {
            var query = new List<string>
            {
                $"page={page}",
                $"per_page={MaxPerPage}"
            };

            if (afterUtc is not null)
                query.Add($"after={afterUtc.Value.ToUnixTimeSeconds()}");

            if (beforeUtc is not null)
                query.Add($"before={beforeUtc.Value.ToUnixTimeSeconds()}");

            return "athlete/activities?" + string.Join("&", query);
        }

        private static StravaActivityResponse MapActivity(ActivityDto dto)
        {
            return new StravaActivityResponse(
                dto.StartDate,
                dto.StartDateLocal,
                dto.UtcOffsetSeconds);
        }
    }
}
