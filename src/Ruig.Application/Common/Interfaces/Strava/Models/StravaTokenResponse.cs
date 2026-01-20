using System;
using System.Collections.Generic;
using System.Text;

namespace Ruig.Application.Common.Interfaces.Strava.Models
{
    public sealed record StravaTokenResponse(
        string AccessToken,
        string RefreshToken,
        long ExpiresAtUnixSeconds,
        long StravaAthleteId,
        string scope);
}
