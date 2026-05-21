namespace Ruig.Application.Common.Interfaces.Strava.Models
{
    public sealed record StravaAthleteResponse(
        long Id,
        string? FirstName,
        string? LastName);
}
