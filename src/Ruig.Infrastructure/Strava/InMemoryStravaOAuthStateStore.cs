using Ruig.Application.Common.Interfaces.Strava;
using System.Collections.Concurrent;

namespace Ruig.Infrastructure.Strava
{
    internal sealed class InMemoryStravaOAuthStateStore : IStravaOAuthStateStore
    {
        private readonly ConcurrentDictionary<string, DateTimeOffset> _states = new();

        public Task StoreAsync(string state, TimeSpan ttl, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(state))
                throw new ArgumentException("OAuth state is required", nameof(state));

            _states[state] = DateTimeOffset.UtcNow.Add(ttl);
            return Task.CompletedTask;
        }

        public Task<bool> ConsumeAsync(string state, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(state))
                return Task.FromResult(false);

            if (!_states.TryRemove(state, out var expiresAtUtc))
                return Task.FromResult(false);

            return Task.FromResult(expiresAtUtc > DateTimeOffset.UtcNow);
        }
    }
}
