using System;
using System.Collections.Generic;
using System.Text;

namespace Ruig.Application.Common.Interfaces.Strava.Models
{
    public sealed record StravaRefreshTokenResponse(
        string AccessToken,
        string RefreshToken,
        long ExpiresAtUnixSeconds);
}
