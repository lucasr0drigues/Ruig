using Microsoft.Extensions.Options;
using Ruig.Infrastructure.Strava;
using System.Net;
using System.Text;

namespace Ruig.Application.Tests;

public sealed class StravaAuthClientTests
{
    [Fact]
    public void BuildAuthorizeUrl_RequestsVisibleActivityScopeOnly()
    {
        var client = CreateClient(new HttpClient(new StaticResponseHandler("{}")));

        var url = client.BuildAuthorizeUrl("state-token");
        var decodedUrl = Uri.UnescapeDataString(url);

        Assert.Contains("scope=read,activity:read", decodedUrl);
        Assert.DoesNotContain("activity:read_all", decodedUrl);
        Assert.DoesNotContain("profile:read_all", decodedUrl);
    }

    [Fact]
    public async Task ExchangeCodeAsync_MapsTokenResponse()
    {
        var responseJson = """
        {
          "access_token": "access-token",
          "refresh_token": "refresh-token",
          "expires_at": 1800000000,
          "scope": "read,activity:read",
          "athlete": { "id": 123 }
        }
        """;

        var client = CreateClient(new HttpClient(new StaticResponseHandler(responseJson)));

        var token = await client.ExchangeCodeAsync("code", CancellationToken.None);

        Assert.Equal("access-token", token.AccessToken);
        Assert.Equal("refresh-token", token.RefreshToken);
        Assert.Equal(1_800_000_000, token.ExpiresAtUnixSeconds);
        Assert.Equal(123, token.StravaAthleteId);
        Assert.Equal("read,activity:read", token.Scope);
    }

    private static StravaAuthClient CreateClient(HttpClient httpClient)
    {
        var options = Options.Create(new StravaOptions
        {
            ClientId = "client-id",
            ClientSecret = "client-secret",
            RedirectUri = "https://example.test/auth/strava/callback",
            AuthorizeBaseUrl = "https://www.strava.com/oauth/authorize",
            TokenUrl = "https://www.strava.com/oauth/token",
            ApiBaseUrl = "https://www.strava.com/api/v3"
        });

        return new StravaAuthClient(httpClient, options);
    }

    private sealed class StaticResponseHandler : HttpMessageHandler
    {
        private readonly string _responseJson;

        public StaticResponseHandler(string responseJson)
        {
            _responseJson = responseJson;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(_responseJson, Encoding.UTF8, "application/json")
            };

            return Task.FromResult(response);
        }
    }
}
