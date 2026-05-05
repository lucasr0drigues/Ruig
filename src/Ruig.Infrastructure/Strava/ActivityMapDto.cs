using System.Text.Json.Serialization;

namespace Ruig.Infrastructure.Strava
{
    internal sealed record ActivityMapDto
    {
        [JsonPropertyName("id")]
        public string? Id { get; init; }

        [JsonPropertyName("summary_polyline")]
        public string? SummaryPolyline { get; init; }
    }
}
