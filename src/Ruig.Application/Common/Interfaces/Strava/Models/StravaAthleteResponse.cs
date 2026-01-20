using System;
using System.Collections.Generic;
using System.Text;

namespace Ruig.Application.Common.Interfaces.Strava.Models
{
    public sealed record StravaAthleteResponse(
        long Id,
        string? Username,
        string? FirstName,
        string? LastName,
        string? Bio,
        string? City,
        string? State,
        string? Country,
        string? Sex,
        string? ProfileMedium,
        string? Profile,
        string? CreatedAt,
        string? UpdatedAt);
}
