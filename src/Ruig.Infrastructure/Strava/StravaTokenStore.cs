using Microsoft.EntityFrameworkCore;
using Ruig.Application.Common.Interfaces.Strava;
using Ruig.Infrastructure.Common.Persistance;

namespace Ruig.Infrastructure.Strava
{
    public sealed class StravaTokenStore : IStravaTokenStore
    {
        private readonly AppDbContext _dbContext;

        public StravaTokenStore(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task SaveOrUpdateAsync(
            Guid athleteId,
            long stravaAthleteId,
            string accessToken,
            string refreshToken,
            DateTimeOffset expiresAtUtc,
            string scope,
            CancellationToken cancellationToken)
        {
            var token = await _dbContext.StravaTokens
                .FirstOrDefaultAsync(t => t.AthleteId == athleteId, cancellationToken);

            if (token is null)
            {
                token = new StravaToken
                {
                    Id = Guid.NewGuid(),
                    AthleteId = athleteId,
                    StravaAthleteId = stravaAthleteId,
                    AccessToken = accessToken,
                    RefreshToken = refreshToken,
                    ExpiresAtUtc = expiresAtUtc,
                    Scope = scope
                };

                await _dbContext.StravaTokens.AddAsync(token, cancellationToken);
            }
            else
            {
                token.StravaAthleteId = stravaAthleteId;
                token.AccessToken = accessToken;
                token.RefreshToken = refreshToken;
                token.ExpiresAtUtc = expiresAtUtc;
                token.Scope = scope;
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        public Task<string?> GetAccessTokenAsync(Guid athleteId, CancellationToken cancellationToken)
        {
            return _dbContext.StravaTokens
                .Where(t => t.AthleteId == athleteId)
                .Select(t => t.AccessToken)
                .FirstOrDefaultAsync(cancellationToken);
        }
    }
}
