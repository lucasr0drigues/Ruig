using Ruig.Application.Common.Interfaces.Strava.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ruig.Application.Common.Interfaces.Strava
{
    public interface IStravaApiClient
    {
        Task<StravaAthleteResponse> GetCurrentAthleteAsync(string accessToken, CancellationToken cancellationToken);
    }
}
