using MediatR;
using Ruig.Application.Common.Interfaces;
using Ruig.Application.Common.Interfaces.Strava;
using Ruig.Application.Common.Interfaces.Strava.Models;
using Ruig.Domain.Entities;
using Ruig.Domain.Enums;
using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace Ruig.Application.Athletes.Commands.CompleteStravaOAuth
{
    public sealed class CompleteStravaOAuthHandler : IRequestHandler<CompleteStravaOAuthCommand, CompleteStravaOAuthResult>
    {
        private const int SlugUniquenessAttempts = 5;

        private readonly IStravaAuthClient _authClient;
        private readonly IStravaApiClient _apiClient;
        private readonly IStravaTokenStore _tokenStore;
        private readonly IStravaOAuthStateStore _stateStore;
        private readonly IStravaActivitySyncService _activitySyncService;
        private readonly IAthleteRepository _athleteRepository;
        private readonly IBadgeRepository _badgeRepository;
        private readonly IBadgeSlugGenerator _badgeSlugGenerator;

        public CompleteStravaOAuthHandler(
            IStravaAuthClient authClient,
            IStravaApiClient apiClient,
            IStravaTokenStore tokenStore,
            IStravaOAuthStateStore stateStore,
            IStravaActivitySyncService activitySyncService,
            IAthleteRepository athleteRepository,
            IBadgeRepository badgeRepository,
            IBadgeSlugGenerator badgeSlugGenerator)
        {
            _authClient = authClient;
            _apiClient = apiClient;
            _tokenStore = tokenStore;
            _stateStore = stateStore;
            _activitySyncService = activitySyncService;
            _athleteRepository = athleteRepository;
            _badgeRepository = badgeRepository;
            _badgeSlugGenerator = badgeSlugGenerator;
        }

        public async Task<CompleteStravaOAuthResult> Handle(CompleteStravaOAuthCommand request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Code))
                throw new ArgumentException("OAuth code is required", nameof(request.Code));

            if (string.IsNullOrWhiteSpace(request.State))
                throw new ArgumentException("OAuth state is required", nameof(request.State));

            var stateData = await _stateStore.ConsumeAsync(request.State, cancellationToken);
            if (stateData is null)
                throw new InvalidOperationException("OAuth state is invalid or expired");

            var token = await _authClient.ExchangeCodeAsync(request.Code, cancellationToken);

            var athleteDto = await _apiClient.GetCurrentAthleteAsync(token.AccessToken, cancellationToken);

            var athlete = MapToDomain(athleteDto);

            var existing = await _athleteRepository.GetByExternalIdAsync(athlete.ExternalAthleteId, cancellationToken);

            Guid athleteId;

            if (existing is null)
            {
                athleteId = athlete.Id;
                await _athleteRepository.AddAsync(athlete, cancellationToken);
            }
            else
            {
                athleteId = existing.Id;
                await _athleteRepository.UpdateFromExternalAsync(existing.Id, athlete, cancellationToken);
            }

            var expiresAtUtc = DateTimeOffset.FromUnixTimeSeconds(token.ExpiresAtUnixSeconds);

            await _tokenStore.SaveOrUpdateAsync(
                athleteId,
                token.StravaAthleteId,
                token.AccessToken,
                token.RefreshToken,
                expiresAtUtc,
                token.Scope,
                cancellationToken);

            await _activitySyncService.InitialBackfillAsync(athleteId, cancellationToken);

            var badgeSlug = await EnsureBadgeAsync(
                athleteId,
                stateData.GitHubUsername,
                stateData.Theme,
                stateData.AccentColor,
                cancellationToken);

            return new CompleteStravaOAuthResult(athleteId, stateData.GitHubUsername, badgeSlug);
        }

        private async Task<string> EnsureBadgeAsync(
            Guid athleteId,
            string gitHubUsername,
            string theme,
            string accentColor,
            CancellationToken cancellationToken)
        {
            var existing = await _badgeRepository.GetByAthleteIdAsync(athleteId, cancellationToken);

            if (existing is not null)
            {
                if (!string.Equals(existing.GitHubUsername, gitHubUsername, StringComparison.OrdinalIgnoreCase))
                    existing.UpdateGitHubUsername(gitHubUsername);

                if (!string.Equals(existing.Theme, theme, StringComparison.OrdinalIgnoreCase) ||
                    !string.Equals(existing.AccentColor, accentColor, StringComparison.OrdinalIgnoreCase))
                {
                    existing.UpdateAppearance(theme, accentColor);
                }

                if (!existing.IsEnabled)
                    existing.Enable();

                await _badgeRepository.SaveChangesAsync(cancellationToken);
                return existing.Slug;
            }

            string slug;
            for (var attempt = 0; ; attempt++)
            {
                slug = _badgeSlugGenerator.Generate();

                if (!await _badgeRepository.SlugExistsAsync(slug, cancellationToken))
                    break;

                if (attempt >= SlugUniquenessAttempts)
                    throw new InvalidOperationException("Could not generate a unique badge slug.");
            }

            var badge = new Badge(athleteId, slug, gitHubUsername, theme, accentColor);
            await _badgeRepository.AddAsync(badge, cancellationToken);
            await _badgeRepository.SaveChangesAsync(cancellationToken);

            return slug;
        }

        private static Athlete MapToDomain(StravaAthleteResponse dto)
        {
            Sex? sex = dto.Sex?.ToLowerInvariant() switch
            {
                "m" => Sex.M,
                "f" => Sex.F,
                _ => null
            };

            DateTime createdAt = ParseStravaDate(dto.CreatedAt) ?? DateTime.UtcNow;
            DateTime updatedAt = ParseStravaDate(dto.UpdatedAt) ?? DateTime.UtcNow;

            return new Athlete(
                dto.Id.ToString(CultureInfo.InvariantCulture),
                dto.Username,
                dto.FirstName,
                dto.LastName,
                dto.Bio,
                dto.City,
                dto.State,
                dto.Country,
                sex,
                createdAt,
                updatedAt,
                dto.ProfileMedium ?? string.Empty,
                dto.Profile ?? string.Empty);
        }

        private static DateTime? ParseStravaDate(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;

            if (DateTime.TryParse(
                value,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal,
                out var dt))
                return dt;

            return null;
        }
    }
}
