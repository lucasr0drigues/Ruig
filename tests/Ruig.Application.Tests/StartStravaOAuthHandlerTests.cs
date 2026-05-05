using Ruig.Application.Athletes.Commands.StartStravaOAuth;
using Ruig.Application.Common.Interfaces.Strava;
using Ruig.Application.Common.Interfaces.Strava.Models;

namespace Ruig.Application.Tests;

public sealed class StartStravaOAuthHandlerTests
{
    [Fact]
    public async Task Handle_GeneratesState_StoresIt_AndReturnsAuthorizeUrl()
    {
        var authClient = new FakeStravaAuthClient();
        var stateStore = new FakeOAuthStateStore();
        var handler = new StartStravaOAuthHandler(authClient, stateStore);

        var result = await handler.Handle(new StartStravaOAuthCommand(), CancellationToken.None);

        Assert.False(string.IsNullOrWhiteSpace(result.State));
        Assert.Equal(43, result.State.Length);
        Assert.Equal(result.State, stateStore.StoredState);
        Assert.Contains(result.State, result.AuthorizationUrl);
    }

    private sealed class FakeStravaAuthClient : IStravaAuthClient
    {
        public string BuildAuthorizeUrl(string state) => $"https://strava.test/oauth?state={state}";

        public Task<StravaTokenResponse> ExchangeCodeAsync(string code, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<StravaRefreshTokenResponse> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class FakeOAuthStateStore : IStravaOAuthStateStore
    {
        public string? StoredState { get; private set; }

        public Task StoreAsync(string state, TimeSpan ttl, CancellationToken cancellationToken)
        {
            StoredState = state;
            return Task.CompletedTask;
        }

        public Task<bool> ConsumeAsync(string state, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }
    }
}
