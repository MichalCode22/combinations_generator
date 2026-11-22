namespace CombinationsGenerator.Infrastructure.SessionStore;

public sealed class SessionStoreOptions
{
    public int MaximumSessions { get; set; } = 10_000;

    public TimeSpan SlidingExpiration { get; set; } = TimeSpan.FromMinutes(30);
}
