using System.Text.Json.Serialization;

namespace Ruig.Infrastructure.Strava
{
    internal sealed record RefreshTokenDto
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; init; } = default!;

        [JsonPropertyName("refresh_token")]
        public string RefreshToken { get; init; } = default!;

        [JsonPropertyName("expires_at")]
        public long ExpiresAt { get; init; }
    }
}
