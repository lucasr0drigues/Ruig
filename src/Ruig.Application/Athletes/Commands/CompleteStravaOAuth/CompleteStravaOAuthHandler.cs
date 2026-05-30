using Ruig.Application.Common.Dispatching;
using Ruig.Application.Common.Interfaces;
using Ruig.Application.Common.Interfaces.Strava;
using Ruig.Application.Common.Interfaces.Strava.Models;
using Ruig.Domain.Entities;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Ruig.Application.Athletes.Commands.CompleteStravaOAuth
{
    public sealed class CompleteStravaOAuthHandler : IRuigRequestHandler<CompleteStravaOAuthCommand, CompleteStravaOAuthResult>
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

            var existingAthleteId = await _tokenStore.GetAthleteIdByStravaAthleteIdAsync(
                token.StravaAthleteId,
                cancellationToken);

            Guid athleteId;

            if (existingAthleteId is null)
            {
                athleteId = athlete.Id;
                await _athleteRepository.AddAsync(athlete, cancellationToken);
            }
            else
            {
                athleteId = existingAthleteId.Value;
                await _athleteRepository.UpdateFromExternalAsync(athleteId, athlete, cancellationToken);
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
                cancellationToken);

            return new CompleteStravaOAuthResult(athleteId, stateData.GitHubUsername, badgeSlug);
        }

        private async Task<string> EnsureBadgeAsync(
            Guid athleteId,
            string gitHubUsername,
            CancellationToken cancellationToken)
        {
            var existing = await _badgeRepository.GetByAthleteIdAsync(athleteId, cancellationToken);

            if (existing is not null)
            {
                if (!string.Equals(existing.GitHubUsername, gitHubUsername, StringComparison.OrdinalIgnoreCase))
                    existing.UpdateGitHubUsername(gitHubUsername);

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

            var badge = new Badge(athleteId, slug, gitHubUsername);
            await _badgeRepository.AddAsync(badge, cancellationToken);
            await _badgeRepository.SaveChangesAsync(cancellationToken);

            return slug;
        }

        private static Athlete MapToDomain(StravaAthleteResponse dto)
        {
            return new Athlete(dto.FirstName, dto.LastName);
        }
    }
}
