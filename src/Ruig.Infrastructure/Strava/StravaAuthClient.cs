using Microsoft.Extensions.Options;
using Ruig.Application.Common.Interfaces.Strava;
using Ruig.Application.Common.Interfaces.Strava.Models;
using System;
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Text;
using System.Text.Encodings.Web;

namespace Ruig.Infrastructure.Strava
{
    public sealed class StravaAuthClient : IStravaAuthClient
    {
        private readonly HttpClient _httpClient;
        private readonly StravaOptions _options;

        public StravaAuthClient(HttpClient httpClient, IOptions<StravaOptions> options)
        {
            _httpClient = httpClient;
            _options = options.Value;
        }

        public string BuildAuthorizeUrl(string state)
        {
            var scope = UrlEncoder.Default.Encode("read,activity:read");
            var redirect = UrlEncoder.Default.Encode(_options.RedirectUri);

            return $"{_options.AuthorizeBaseUrl}" +
                   $"?client_id={_options.ClientId}" +
                   $"&response_type=code" +
                   $"&redirect_uri={redirect}" +
                   $"&approval_prompt=auto" +
                   $"&scope={scope}" +
                   $"&state={UrlEncoder.Default.Encode(state)}";
        }

        public async Task<StravaTokenResponse> ExchangeCodeAsync(string code, CancellationToken cancellationToken)
        {
            var payload = new Dictionary<string, string>
            {
                ["client_id"] = _options.ClientId,
                ["client_secret"] = _options.ClientSecret,
                ["code"] = code,
                ["grant_type"] = "authorization_code"
            };

            using var content = new FormUrlEncodedContent(payload);

            using var response = await _httpClient.PostAsync(_options.TokenUrl, content, cancellationToken);
            response.EnsureSuccessStatusCode();

            var dto = await response.Content.ReadFromJsonAsync<TokenExchangeDto>(cancellationToken);

            if (dto is null)
                throw new InvalidOperationException("Strava token response was empty");

            return new StravaTokenResponse(
                dto.AccessToken,
                dto.RefreshToken,
                dto.ExpiresAt,
                dto.Athlete.Id,
                dto.Scope ?? string.Empty);
        }
    }
}
