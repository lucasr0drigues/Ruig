using Microsoft.EntityFrameworkCore;
using Ruig.Application.Common.Interfaces;
using Ruig.Application.Common.Interfaces.Strava;
using Ruig.Infrastructure.Common.Persistance;

namespace Ruig.Infrastructure.Strava
{
    public sealed class StravaTokenStore : IStravaTokenStore
    {
        private static readonly TimeSpan RefreshSkew = TimeSpan.FromMinutes(5);

        private readonly AppDbContext _dbContext;
        private readonly IStravaAuthClient _authClient;
        private readonly IDateTimeProvider _dateTimeProvider;

        public StravaTokenStore(
            AppDbContext dbContext,
            IStravaAuthClient authClient,
            IDateTimeProvider dateTimeProvider)
        {
            _dbContext = dbContext;
            _authClient = authClient;
            _dateTimeProvider = dateTimeProvider;
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

        public async Task<string?> GetAccessTokenAsync(Guid athleteId, CancellationToken cancellationToken)
        {
            var token = await _dbContext.StravaTokens
                .FirstOrDefaultAsync(t => t.AthleteId == athleteId, cancellationToken);

            if (token is null)
                return null;

            if (token.ExpiresAtUtc > GetUtcNow().Add(RefreshSkew))
            {
                if (token.RevokedAtUtc is not null)
                    return null;

                return token.AccessToken;
            }

            if (token.RevokedAtUtc is not null)
                return null;

            var refreshed = await _authClient.RefreshTokenAsync(token.RefreshToken, cancellationToken);

            token.AccessToken = refreshed.AccessToken;
            token.RefreshToken = refreshed.RefreshToken;
            token.ExpiresAtUtc = DateTimeOffset.FromUnixTimeSeconds(refreshed.ExpiresAtUnixSeconds);

            await _dbContext.SaveChangesAsync(cancellationToken);

            return token.AccessToken;
        }

        public async Task RevokeByStravaAthleteIdAsync(
            long stravaAthleteId,
            DateTimeOffset revokedAtUtc,
            CancellationToken cancellationToken)
        {
            var token = await _dbContext.StravaTokens
                .FirstOrDefaultAsync(t => t.StravaAthleteId == stravaAthleteId, cancellationToken);

            if (token is null)
                return;

            token.RevokedAtUtc = revokedAtUtc;
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        private DateTimeOffset GetUtcNow()
        {
            var utcNow = _dateTimeProvider.UtcNow.Kind == DateTimeKind.Utc
                ? _dateTimeProvider.UtcNow
                : DateTime.SpecifyKind(_dateTimeProvider.UtcNow, DateTimeKind.Utc);

            return new DateTimeOffset(utcNow);
        }
    }
}
