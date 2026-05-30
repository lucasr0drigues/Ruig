using Microsoft.EntityFrameworkCore;
using Ruig.Application.Common.Interfaces;
using Ruig.Application.Common.Interfaces.Strava;
using Ruig.Infrastructure.Common.Persistance;
using Ruig.Infrastructure.Security;

namespace Ruig.Infrastructure.Strava
{
    public sealed class StravaTokenStore : IStravaTokenStore
    {
        private static readonly TimeSpan RefreshSkew = TimeSpan.FromMinutes(5);

        private readonly AppDbContext _dbContext;
        private readonly IStravaAuthClient _authClient;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly ITokenEncryptor _tokenEncryptor;

        public StravaTokenStore(
            AppDbContext dbContext,
            IStravaAuthClient authClient,
            IDateTimeProvider dateTimeProvider,
            ITokenEncryptor tokenEncryptor)
        {
            _dbContext = dbContext;
            _authClient = authClient;
            _dateTimeProvider = dateTimeProvider;
            _tokenEncryptor = tokenEncryptor;
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
                    AccessToken = _tokenEncryptor.Encrypt(accessToken),
                    RefreshToken = _tokenEncryptor.Encrypt(refreshToken),
                    ExpiresAtUtc = expiresAtUtc,
                    Scope = scope
                };

                await _dbContext.StravaTokens.AddAsync(token, cancellationToken);
            }
            else
            {
                token.StravaAthleteId = stravaAthleteId;
                token.AccessToken = _tokenEncryptor.Encrypt(accessToken);
                token.RefreshToken = _tokenEncryptor.Encrypt(refreshToken);
                token.ExpiresAtUtc = expiresAtUtc;
                token.Scope = scope;
                token.RevokedAtUtc = null;
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

                return _tokenEncryptor.Decrypt(token.AccessToken);
            }

            if (token.RevokedAtUtc is not null)
                return null;

            var refreshToken = _tokenEncryptor.Decrypt(token.RefreshToken);
            var refreshed = await _authClient.RefreshTokenAsync(refreshToken, cancellationToken);

            token.AccessToken = _tokenEncryptor.Encrypt(refreshed.AccessToken);
            token.RefreshToken = _tokenEncryptor.Encrypt(refreshed.RefreshToken);
            token.ExpiresAtUtc = DateTimeOffset.FromUnixTimeSeconds(refreshed.ExpiresAtUnixSeconds);

            await _dbContext.SaveChangesAsync(cancellationToken);

            return refreshed.AccessToken;
        }

        public async Task<Guid?> GetAthleteIdByStravaAthleteIdAsync(
            long stravaAthleteId,
            CancellationToken cancellationToken)
        {
            return await _dbContext.StravaTokens
                .AsNoTracking()
                .Where(t => t.StravaAthleteId == stravaAthleteId)
                .Select(t => (Guid?)t.AthleteId)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<Guid?> GetActiveAthleteIdByStravaAthleteIdAsync(
            long stravaAthleteId,
            CancellationToken cancellationToken)
        {
            return await _dbContext.StravaTokens
                .AsNoTracking()
                .Where(t => t.StravaAthleteId == stravaAthleteId && t.RevokedAtUtc == null)
                .Select(t => (Guid?)t.AthleteId)
                .FirstOrDefaultAsync(cancellationToken);
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

        public async Task<IReadOnlyList<Guid>> ListActiveAthleteIdsAsync(CancellationToken cancellationToken)
        {
            return await _dbContext.StravaTokens
                .AsNoTracking()
                .Where(t => t.RevokedAtUtc == null)
                .OrderBy(t => t.AthleteId)
                .Select(t => t.AthleteId)
                .ToListAsync(cancellationToken);
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
