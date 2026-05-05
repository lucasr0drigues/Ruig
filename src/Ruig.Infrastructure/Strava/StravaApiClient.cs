using Ruig.Application.Common.Interfaces.Strava;
using Ruig.Application.Common.Interfaces.Strava.Models;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Ruig.Infrastructure.Strava
{
    internal sealed class StravaApiClient : IStravaApiClient
    {
        private readonly HttpClient _httpClient;

        public StravaApiClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<StravaAthleteResponse> GetCurrentAthleteAsync(string accessToken, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(accessToken))
                throw new ArgumentException("Strava access token is required", nameof(accessToken));

            using var request = new HttpRequestMessage(HttpMethod.Get, "athlete");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var dto = await response.Content.ReadFromJsonAsync<AthleteDto>(cancellationToken);

            if (dto is null)
                throw new InvalidOperationException("Strava athlete response was empty");

            return new StravaAthleteResponse(
                dto.Id,
                dto.Username,
                dto.FirstName,
                dto.LastName,
                dto.Bio,
                dto.City,
                dto.State,
                dto.Country,
                dto.Sex,
                dto.ProfileMedium,
                dto.Profile,
                dto.CreatedAt,
                dto.UpdatedAt);
        }
    }
}
