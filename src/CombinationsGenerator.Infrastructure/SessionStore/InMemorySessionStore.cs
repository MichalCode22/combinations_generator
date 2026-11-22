using System.Collections.Concurrent;
using CombinationsGenerator.Application.SessionStore;
using Microsoft.Extensions.Options;

namespace CombinationsGenerator.Infrastructure.SessionStore;

public sealed class InMemorySessionStore : ISessionStore
{
    private readonly ConcurrentDictionary<Guid, SessionEntry> _sessions = new();
    private readonly SemaphoreSlim _creationLock = new(1, 1);
    private readonly SessionStoreOptions _options;
    private readonly TimeProvider _timeProvider;

    public InMemorySessionStore(IOptions<SessionStoreOptions> options, TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(options);
        _options = options.Value;
        if (_options.MaximumSessions < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "MaximumSessions must be positive.");
        }

        if (_options.SlidingExpiration <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "SlidingExpiration must be positive.");
        }

        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public int Count => _sessions.Count;

    public async Task<Guid> CreateAsync(
        PermutationSession session,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);
        await _creationLock.WaitAsync(cancellationToken);
        try
        {
            await RemoveExpiredSessionsAsync(cancellationToken);
            if (_sessions.Count >= _options.MaximumSessions)
            {
                throw new SessionLimitExceededException(_options.MaximumSessions);
            }

            var id = Guid.NewGuid();
            var entry = new SessionEntry(session.Copy(), _timeProvider.GetUtcNow().UtcTicks);
            if (!_sessions.TryAdd(id, entry))
            {
                throw new InvalidOperationException("A newly generated session identifier was already in use.");
            }

            return id;
        }
        finally
        {
            _creationLock.Release();
        }
    }

    public async Task<T> UpdateAsync<T>(
        Guid sessionId,
        Func<PermutationSession, SessionUpdate<T>> update,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(update);
        var entry = await GetEntryAsync(sessionId, cancellationToken);
        await entry.Gate.WaitAsync(cancellationToken);
        try
        {
            EnsureCurrentAndActive(sessionId, entry);
            var result = update(entry.State.Copy());
            ArgumentNullException.ThrowIfNull(result);
            ArgumentNullException.ThrowIfNull(result.State);
            entry.State = result.State.Copy();
            entry.Touch(_timeProvider.GetUtcNow().UtcTicks);
            return result.Result;
        }
        finally
        {
            entry.Gate.Release();
        }
    }

    public async Task<bool> RemoveAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        if (!_sessions.TryGetValue(sessionId, out var entry))
        {
            return false;
        }

        await entry.Gate.WaitAsync(cancellationToken);
        try
        {
            if (!_sessions.TryGetValue(sessionId, out var current) || !ReferenceEquals(entry, current))
            {
                return false;
            }

            return ((ICollection<KeyValuePair<Guid, SessionEntry>>)_sessions)
                .Remove(new KeyValuePair<Guid, SessionEntry>(sessionId, entry));
        }
        finally
        {
            entry.Gate.Release();
        }
    }

    private async Task<SessionEntry> GetEntryAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        if (!_sessions.TryGetValue(sessionId, out var entry))
        {
            throw new SessionNotFoundException(sessionId);
        }

        await entry.Gate.WaitAsync(cancellationToken);
        try
        {
            EnsureCurrentAndActive(sessionId, entry);
            entry.Touch(_timeProvider.GetUtcNow().UtcTicks);
            return entry;
        }
        finally
        {
            entry.Gate.Release();
        }
    }

    private void EnsureCurrentAndActive(Guid sessionId, SessionEntry entry)
    {
        if (!_sessions.TryGetValue(sessionId, out var current) || !ReferenceEquals(entry, current))
        {
            throw new SessionNotFoundException(sessionId);
        }

        var now = _timeProvider.GetUtcNow().UtcTicks;
        if (now - entry.LastAccessUtcTicks >= _options.SlidingExpiration.Ticks)
        {
            ((ICollection<KeyValuePair<Guid, SessionEntry>>)_sessions)
                .Remove(new KeyValuePair<Guid, SessionEntry>(sessionId, entry));
            throw new SessionNotFoundException(sessionId);
        }
    }

    private async Task RemoveExpiredSessionsAsync(CancellationToken cancellationToken)
    {
        var now = _timeProvider.GetUtcNow().UtcTicks;
        foreach (var (id, entry) in _sessions)
        {
            if (now - entry.LastAccessUtcTicks < _options.SlidingExpiration.Ticks)
            {
                continue;
            }

            await entry.Gate.WaitAsync(cancellationToken);
            try
            {
                now = _timeProvider.GetUtcNow().UtcTicks;
                if (_sessions.TryGetValue(id, out var current)
                    && ReferenceEquals(entry, current)
                    && now - entry.LastAccessUtcTicks >= _options.SlidingExpiration.Ticks)
                {
                    ((ICollection<KeyValuePair<Guid, SessionEntry>>)_sessions)
                        .Remove(new KeyValuePair<Guid, SessionEntry>(id, entry));
                }
            }
            finally
            {
                entry.Gate.Release();
            }
        }
    }

    private sealed class SessionEntry(PermutationSession state, long lastAccessUtcTicks)
    {
        private long _lastAccessUtcTicks = lastAccessUtcTicks;

        public SemaphoreSlim Gate { get; } = new(1, 1);

        public PermutationSession State { get; set; } = state;

        public long LastAccessUtcTicks => Interlocked.Read(ref _lastAccessUtcTicks);

        public void Touch(long utcTicks) => Interlocked.Exchange(ref _lastAccessUtcTicks, utcTicks);
    }
}
