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
        private readonly IStravaAuthClient _authClient;

        public StartStravaOAuthHandler(IStravaAuthClient authClient)
        {
            _authClient = authClient;
        }

        public Task<StartStravaOAuthResult> Handle(StartStravaOAuthCommand request, CancellationToken cancellationToken)
        {
            var state = CreateState();
            var url = _authClient.BuildAuthorizeUrl(state);

            return Task.FromResult(new StartStravaOAuthResult(url, state));
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
