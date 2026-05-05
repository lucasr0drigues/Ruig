using Ruig.Application.Common.Interfaces.Strava.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ruig.Application.Common.Interfaces.Strava
{
    public interface IStravaAuthClient
    {
        string BuildAuthorizeUrl(string state);

        Task<StravaTokenResponse> ExchangeCodeAsync(string code, CancellationToken cancellationToken);

        Task<StravaRefreshTokenResponse> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken);
    }
}
