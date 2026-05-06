using Ruig.Application.Common.Interfaces;
using Ruig.Application.Common.Interfaces.Strava;
using Ruig.Application.Common.Interfaces.Strava.Models;
using Ruig.Domain.Entities;
using Ruig.Domain.Enums;
using System.Globalization;

namespace Ruig.Infrastructure.Strava
{
    public sealed class StravaActivitySyncService : IStravaActivitySyncService
    {
        private readonly IStravaTokenStore _tokenStore;
        private readonly IStravaApiClient _apiClient;
        private readonly IActivityRepository _activityRepository;
        private readonly IAthleteRepository _athleteRepository;
        private readonly IDateTimeProvider _dateTimeProvider;

        public StravaActivitySyncService(
            IStravaTokenStore tokenStore,
            IStravaApiClient apiClient,
            IActivityRepository activityRepository,
            IAthleteRepository athleteRepository,
            IDateTimeProvider dateTimeProvider)
        {
            _tokenStore = tokenStore;
            _apiClient = apiClient;
            _activityRepository = activityRepository;
            _athleteRepository = athleteRepository;
            _dateTimeProvider = dateTimeProvider;
        }

        public async Task InitialBackfillAsync(Guid athleteId, CancellationToken cancellationToken)
        {
            var now = GetUtcNow();
            var from = new DateTimeOffset(now.Year - 1, 1, 1, 0, 0, 0, TimeSpan.Zero);

            await SyncRangeAsync(athleteId, from, now, cancellationToken);
        }

        public async Task SyncRecentActivitiesAsync(Guid athleteId, TimeSpan lookback, CancellationToken cancellationToken)
        {
            var now = GetUtcNow();
            await SyncRangeAsync(athleteId, now.Subtract(lookback), now, cancellationToken);
        }

        public async Task SyncActivityAsync(Guid athleteId, long externalActivityId, CancellationToken cancellationToken)
        {
            var accessToken = await GetAccessTokenOrThrowAsync(athleteId, cancellationToken);
            var activity = await _apiClient.GetActivityAsync(accessToken, externalActivityId, cancellationToken);

            await _activityRepository.UpsertAsync(MapToDomain(athleteId, activity), cancellationToken);
            await _activityRepository.SaveChangesAsync(cancellationToken);
        }

        public async Task MarkActivityDeletedAsync(Guid athleteId, long externalActivityId, CancellationToken cancellationToken)
        {
            var activity = await _activityRepository.GetByExternalIdAsync(
                athleteId,
                externalActivityId.ToString(CultureInfo.InvariantCulture),
                cancellationToken);

            if (activity is null)
                return;

            activity.MarkDeleted(GetUtcNow());
            await _activityRepository.SaveChangesAsync(cancellationToken);
        }

        private async Task SyncRangeAsync(
            Guid athleteId,
            DateTimeOffset afterUtc,
            DateTimeOffset beforeUtc,
            CancellationToken cancellationToken)
        {
            var accessToken = await GetAccessTokenOrThrowAsync(athleteId, cancellationToken);
            var activities = await _apiClient.ListAthleteActivitiesAsync(accessToken, afterUtc, beforeUtc, cancellationToken);

            foreach (var activity in activities)
            {
                await _activityRepository.UpsertAsync(MapToDomain(athleteId, activity), cancellationToken);
            }

            await _activityRepository.SaveChangesAsync(cancellationToken);
            await _athleteRepository.MarkActivitySyncCompletedAsync(athleteId, GetUtcNow(), cancellationToken);
        }

        private async Task<string> GetAccessTokenOrThrowAsync(Guid athleteId, CancellationToken cancellationToken)
        {
            var accessToken = await _tokenStore.GetAccessTokenAsync(athleteId, cancellationToken);

            if (string.IsNullOrWhiteSpace(accessToken))
                throw new InvalidOperationException($"No Strava token found for athlete '{athleteId}'.");

            return accessToken;
        }

        private static Activity MapToDomain(Guid athleteId, StravaActivityResponse dto)
        {
            return new Activity(
                athleteId,
                dto.Id.ToString(CultureInfo.InvariantCulture),
                dto.Name,
                ParseSport(dto.SportType),
                dto.DistanceMeters,
                dto.MovingTimeSeconds,
                dto.ElapsedTimeSeconds,
                dto.TotalElevationGainMeters,
                ParseStartDate(dto.StartDate),
                ParseUtcOffset(dto.UtcOffsetSeconds),
                ParseVisibility(dto),
                dto.DeviceName,
                dto.Map?.Id,
                dto.Map?.SummaryPolyline);
        }

        private static ActivitySport? ParseSport(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            return Enum.TryParse<ActivitySport>(value, ignoreCase: true, out var sport)
                ? sport
                : null;
        }

        private static DateTimeOffset? ParseStartDate(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            return DateTimeOffset.TryParse(
                value,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out var startedAt)
                ? startedAt
                : null;
        }

        private static TimeSpan? ParseUtcOffset(double? utcOffsetSeconds)
        {
            return utcOffsetSeconds is null
                ? null
                : TimeSpan.FromSeconds(utcOffsetSeconds.Value);
        }

        private static ActivityVisibility ParseVisibility(StravaActivityResponse dto)
        {
            if (dto.IsPrivate == true)
                return ActivityVisibility.OnlyMe;

            return dto.Visibility?.ToLowerInvariant() switch
            {
                "everyone" => ActivityVisibility.Everyone,
                "followers_only" or "followers" => ActivityVisibility.FollowersOnly,
                "only_me" => ActivityVisibility.OnlyMe,
                _ => ActivityVisibility.Unknown
            };
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
