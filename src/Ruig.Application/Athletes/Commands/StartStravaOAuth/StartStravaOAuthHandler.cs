using MediatR;
using Ruig.Application.Common.Interfaces.Strava;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace Ruig.Application.Athletes.Commands.StartStravaOAuth
{
    public sealed class StartStravaOAuthHandler : IRequestHandler<StartStravaOAuthCommand, StartStravaOAuthResult>
    {
        private static readonly TimeSpan StateTtl = TimeSpan.FromMinutes(10);

        private readonly IStravaAuthClient _authClient;
        private readonly IStravaOAuthStateStore _stateStore;

        public StartStravaOAuthHandler(IStravaAuthClient authClient, IStravaOAuthStateStore stateStore)
        {
            _authClient = authClient;
            _stateStore = stateStore;
        }

        public async Task<StartStravaOAuthResult> Handle(StartStravaOAuthCommand request, CancellationToken cancellationToken)
        {
            var state = CreateState();
            await _stateStore.StoreAsync(state, StateTtl, cancellationToken);

            var url = _authClient.BuildAuthorizeUrl(state);

            return new StartStravaOAuthResult(url, state);
        }

        private static string CreateState()
        {
            var bytes = RandomNumberGenerator.GetBytes(32);

            return Convert.ToBase64String(bytes)
                .Replace("+", "-")
                .Replace("/", "_")
                .TrimEnd('=');
        }
    }
}
