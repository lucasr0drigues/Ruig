using Ruig.Application.Common.Interfaces.Strava;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Ruig.Infrastructure.Strava
{
    internal sealed class InMemoryStravaOAuthStateStore : IStravaOAuthStateStore
    {
        private readonly ConcurrentDictionary<string, StoredState> _states = new();

        public Task StoreAsync(string state, StravaOAuthStateData data, TimeSpan ttl, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(state))
                throw new ArgumentException("OAuth state is required", nameof(state));

            ArgumentNullException.ThrowIfNull(data);

            _states[state] = new StoredState(data, DateTimeOffset.UtcNow.Add(ttl));
            return Task.CompletedTask;
        }

        public Task<StravaOAuthStateData?> ConsumeAsync(string state, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(state))
                return Task.FromResult<StravaOAuthStateData?>(null);

            if (!_states.TryRemove(state, out var stored))
                return Task.FromResult<StravaOAuthStateData?>(null);

            if (stored.ExpiresAtUtc <= DateTimeOffset.UtcNow)
                return Task.FromResult<StravaOAuthStateData?>(null);

            return Task.FromResult<StravaOAuthStateData?>(stored.Data);
        }

        private sealed record StoredState(StravaOAuthStateData Data, DateTimeOffset ExpiresAtUtc);
    }
}
