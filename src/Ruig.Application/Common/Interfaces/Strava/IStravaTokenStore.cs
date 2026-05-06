using System;
using System.Collections.Generic;
using System.Text;

namespace Ruig.Application.Common.Interfaces.Strava
{
    public interface IStravaTokenStore
    {
        Task SaveOrUpdateAsync(Guid athleteId, long stravaAthleteId, string accessToken, string refreshToken, DateTimeOffset expiresAtUtc, string scope, CancellationToken cancellationToken);
        Task<string?> GetAccessTokenAsync(Guid athleteId, CancellationToken cancellationToken);
        Task RevokeByStravaAthleteIdAsync(long stravaAthleteId, DateTimeOffset revokedAtUtc, CancellationToken cancellationToken);
        Task<IReadOnlyList<Guid>> ListActiveAthleteIdsAsync(CancellationToken cancellationToken);
    }
}
