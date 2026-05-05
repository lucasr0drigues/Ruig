using Ruig.Application.Common.Interfaces.Strava.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ruig.Application.Common.Interfaces.Strava
{
    public interface IStravaApiClient
    {
        Task<StravaAthleteResponse> GetCurrentAthleteAsync(string accessToken, CancellationToken cancellationToken);

        Task<IReadOnlyList<StravaActivityResponse>> ListAthleteActivitiesAsync(
            string accessToken,
            DateTimeOffset? afterUtc,
            DateTimeOffset? beforeUtc,
            CancellationToken cancellationToken);

        Task<StravaActivityResponse> GetActivityAsync(
            string accessToken,
            long activityId,
            CancellationToken cancellationToken);
    }
}
