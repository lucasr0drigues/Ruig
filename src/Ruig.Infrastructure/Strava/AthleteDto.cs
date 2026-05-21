using System.Text.Json.Serialization;

namespace Ruig.Infrastructure.Strava
{
    internal sealed record AthleteDto
    {
        [JsonPropertyName("id")]
        public long Id { get; init; }

        [JsonPropertyName("firstname")]
        public string? FirstName { get; init; }

        [JsonPropertyName("lastname")]
        public string? LastName { get; init; }
    }
}
