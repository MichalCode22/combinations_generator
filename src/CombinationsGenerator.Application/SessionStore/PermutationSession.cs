namespace CombinationsGenerator.Application.SessionStore;

public sealed record PermutationSession(
    int N,
    ulong Total,
    ulong LastShownIndex,
    int[] Current,
    ulong? AllStart)
{
    public static PermutationSession Empty() => new(0, 0, 0, [], null);

    public PermutationSession Copy() => this with { Current = (int[])Current.Clone() };
}

public sealed record SessionUpdate<T>(PermutationSession State, T Result);

public interface ISessionStore
{
    Task<Guid> CreateAsync(PermutationSession session, CancellationToken cancellationToken = default);

    Task<T> UpdateAsync<T>(
        Guid sessionId,
        Func<PermutationSession, SessionUpdate<T>> update,
        CancellationToken cancellationToken = default);

    Task<bool> RemoveAsync(Guid sessionId, CancellationToken cancellationToken = default);
}

public sealed class SessionNotFoundException(Guid sessionId)
    : Exception($"Session '{sessionId}' was not found or has expired.")
{
    public Guid SessionId { get; } = sessionId;
}

public sealed class SessionLimitExceededException(int maximumSessions)
    : Exception($"The maximum number of active sessions ({maximumSessions}) has been reached.")
{
    public int MaximumSessions { get; } = maximumSessions;
}
