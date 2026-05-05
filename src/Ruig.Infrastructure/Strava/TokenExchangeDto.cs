using System.Text.Json.Serialization;

namespace Ruig.Infrastructure.Strava
{
    internal sealed record TokenExchangeDto
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; init; } = default!;

        [JsonPropertyName("refresh_token")]
        public string RefreshToken { get; init; } = default!;

        [JsonPropertyName("expires_at")]
        public long ExpiresAt { get; init; }

        [JsonPropertyName("scope")]
        public string? Scope { get; init; }

        [JsonPropertyName("athlete")]
        public AthleteDto Athlete { get; init; } = default!;
    }
}
