using MediatR;
using Ruig.Application.Common.Interfaces;
using Ruig.Application.Common.Interfaces.Strava;
using Ruig.Application.Common.Interfaces.Strava.Models;
using Ruig.Domain.Entities;
using Ruig.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Ruig.Application.Athletes.Commands.CompleteStravaOAuth
{
    public sealed class CompleteStravaOAuthHandler : IRequestHandler<CompleteStravaOAuthCommand, Guid>
    {
        private readonly IStravaAuthClient _authClient;
        private readonly IStravaApiClient _apiClient;
        private readonly IStravaTokenStore _tokenStore;
        private readonly IStravaOAuthStateStore _stateStore;
        private readonly IAthleteRepository _athleteRepository;

        public CompleteStravaOAuthHandler(
            IStravaAuthClient authClient,
            IStravaApiClient apiClient,
            IStravaTokenStore tokenStore,
            IStravaOAuthStateStore stateStore,
            IAthleteRepository athleteRepository)
        {
            _authClient = authClient;
            _apiClient = apiClient;
            _tokenStore = tokenStore;
            _stateStore = stateStore;
            _athleteRepository = athleteRepository;
        }

        public async Task<Guid> Handle(CompleteStravaOAuthCommand request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Code))
                throw new ArgumentException("OAuth code is required", nameof(request.Code));

            if (string.IsNullOrWhiteSpace(request.State))
                throw new ArgumentException("OAuth state is required", nameof(request.State));

            var stateIsValid = await _stateStore.ConsumeAsync(request.State, cancellationToken);
            if (!stateIsValid)
                throw new InvalidOperationException("OAuth state is invalid or expired");

            var token = await _authClient.ExchangeCodeAsync(request.Code, cancellationToken);

            var athleteDto = await _apiClient.GetCurrentAthleteAsync(token.AccessToken, cancellationToken);

            var athlete = MapToDomain(athleteDto);

            var existing = await _athleteRepository.GetByExternalIdAsync(athlete.ExternalAthleteId, cancellationToken);

            Guid athleteId;

            if(existing is null)
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

            return athleteId;
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
            if(string.IsNullOrWhiteSpace(value)) return null;

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
