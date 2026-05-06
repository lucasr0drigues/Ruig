using Ruig.Application.Athletes.Commands.StartStravaOAuth;
using Ruig.Application.Common.Interfaces.Strava;
using Ruig.Application.Common.Interfaces.Strava.Models;

namespace Ruig.Application.Tests;

public sealed class StartStravaOAuthHandlerTests
{
    [Fact]
    public async Task Handle_StoresStateWithGitHubUsernameAndReturnsAuthorizeUrl()
    {
        var authClient = new FakeStravaAuthClient();
        var stateStore = new FakeOAuthStateStore();
        var handler = new StartStravaOAuthHandler(authClient, stateStore);

        var result = await handler.Handle(new StartStravaOAuthCommand("lucas"), CancellationToken.None);

        Assert.False(string.IsNullOrWhiteSpace(result.State));
        Assert.Equal(43, result.State.Length);
        Assert.Equal(result.State, stateStore.StoredState);
        Assert.Equal("lucas", stateStore.StoredData?.GitHubUsername);
        Assert.Contains(result.State, result.AuthorizationUrl);
    }

    [Fact]
    public void Validator_RejectsEmptyOrInvalidGitHubUsername()
    {
        var validator = new StartStravaOAuthValidator();

        Assert.False(validator.Validate(new StartStravaOAuthCommand("")).IsValid);
        Assert.False(validator.Validate(new StartStravaOAuthCommand("-bad")).IsValid);
        Assert.False(validator.Validate(new StartStravaOAuthCommand("bad-")).IsValid);
        Assert.False(validator.Validate(new StartStravaOAuthCommand("with space")).IsValid);
        Assert.False(validator.Validate(new StartStravaOAuthCommand("double--hyphen")).IsValid);
        Assert.True(validator.Validate(new StartStravaOAuthCommand("lucas")).IsValid);
        Assert.True(validator.Validate(new StartStravaOAuthCommand("lucas-rodrigues-1")).IsValid);
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
        public StravaOAuthStateData? StoredData { get; private set; }

        public Task StoreAsync(string state, StravaOAuthStateData data, TimeSpan ttl, CancellationToken cancellationToken)
        {
            StoredState = state;
            StoredData = data;
            return Task.CompletedTask;
        }

        public Task<StravaOAuthStateData?> ConsumeAsync(string state, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }
    }
}
