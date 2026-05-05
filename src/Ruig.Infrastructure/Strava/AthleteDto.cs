using System.Text.Json.Serialization;

namespace Ruig.Infrastructure.Strava
{
    internal sealed record AthleteDto
    {
        [JsonPropertyName("id")]
        public long Id { get; init; }

        [JsonPropertyName("username")]
        public string? Username { get; init; }

        [JsonPropertyName("firstname")]
        public string? FirstName { get; init; }

        [JsonPropertyName("lastname")]
        public string? LastName { get; init; }

        [JsonPropertyName("bio")]
        public string? Bio { get; init; }

        [JsonPropertyName("city")]
        public string? City { get; init; }

        [JsonPropertyName("state")]
        public string? State { get; init; }

        [JsonPropertyName("country")]
        public string? Country { get; init; }

        [JsonPropertyName("sex")]
        public string? Sex { get; init; }

        [JsonPropertyName("profile_medium")]
        public string? ProfileMedium { get; init; }

        [JsonPropertyName("profile")]
        public string? Profile { get; init; }

        [JsonPropertyName("created_at")]
        public string? CreatedAt { get; init; }

        [JsonPropertyName("updated_at")]
        public string? UpdatedAt { get; init; }
    }
}
