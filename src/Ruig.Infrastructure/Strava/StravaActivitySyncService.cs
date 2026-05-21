using Ruig.Application.Common.Interfaces;
using Ruig.Application.Common.Interfaces.Strava;
using Ruig.Application.Common.Interfaces.Strava.Models;
using Ruig.Domain.Entities;
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
            var localDate = GetLocalDate(activity);

            if (localDate is null)
                return;

            await _activityRepository.UpsertAsync(new Activity(athleteId, localDate.Value), cancellationToken);
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
            var localDates = activities.Select(GetLocalDate).OfType<DateOnly>();

            var fromLocalDate = DateOnly.FromDateTime(afterUtc.UtcDateTime.AddDays(-1));
            var toLocalDate = DateOnly.FromDateTime(beforeUtc.UtcDateTime.AddDays(1));

            await _activityRepository.ReplaceLocalDatesAsync(
                athleteId,
                fromLocalDate,
                toLocalDate,
                localDates,
                cancellationToken);

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

        private static DateOnly? GetLocalDate(StravaActivityResponse dto)
        {
            if (!string.IsNullOrWhiteSpace(dto.StartDateLocal)
                && DateTime.TryParse(
                    dto.StartDateLocal,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeLocal,
                    out var localStart))
            {
                return DateOnly.FromDateTime(localStart);
            }

            if (string.IsNullOrWhiteSpace(dto.StartDate)
                || !DateTimeOffset.TryParse(
                    dto.StartDate,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                    out var startedAt))
            {
                return null;
            }

            var localDateTime = dto.UtcOffsetSeconds is null
                ? startedAt.UtcDateTime
                : startedAt.ToOffset(TimeSpan.FromSeconds(dto.UtcOffsetSeconds.Value)).DateTime;

            return DateOnly.FromDateTime(localDateTime);
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
