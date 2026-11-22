using System.Globalization;
using CombinationsGenerator.Application.SessionStore;
using CombinationsGenerator.Domain;

namespace CombinationsGenerator.Application.Services;

public interface IStartService
{
    Task<StartResponse> StartAsync(StartRequest request, CancellationToken cancellationToken = default);
}

public interface INextService
{
    Task<NextResponse> NextAsync(Guid sessionId, CancellationToken cancellationToken = default);
}

public interface IAllService
{
    Task<AllResponse> GetAllAsync(
        Guid sessionId,
        int? pageSize = null,
        ulong page = 1,
        bool enter = false,
        CancellationToken cancellationToken = default);
}

public interface ICurrentService
{
    Task<CurrentResponse> GetCurrentAsync(Guid sessionId, CancellationToken cancellationToken = default);
}

public interface IResetService
{
    Task ResetAsync(Guid sessionId, CancellationToken cancellationToken = default);
}

public sealed class StartService(ISessionStore sessions, IPermutationGenerator permutations) : IStartService
{
    public async Task<StartResponse> StartAsync(StartRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.N is < 1 or > 20)
        {
            throw new ApplicationValidationException("n must be between 1 and 20.");
        }

        var total = permutations.Factorial(request.N);
        var initial = Enumerable.Range(1, request.N).ToArray();
        var sessionId = await sessions.CreateAsync(
            new PermutationSession(request.N, total, 0, initial, null),
            cancellationToken);

        return new StartResponse(sessionId, total.ToString(CultureInfo.InvariantCulture));
    }
}

public sealed class NextService(ISessionStore sessions, IPermutationGenerator permutations) : INextService
{
    private const string ExhaustedMessage = "אין יותר קומבינציות";

    public Task<NextResponse> NextAsync(Guid sessionId, CancellationToken cancellationToken = default) =>
        sessions.UpdateAsync(
            sessionId,
            state =>
            {
                if (state.N == 0)
                {
                    throw new ApplicationValidationException("The session has not been started.");
                }

                if (state.LastShownIndex >= state.Total)
                {
                    return new SessionUpdate<NextResponse>(
                        state,
                        new NextResponse(false, null, null, ExhaustedMessage));
                }

                var next = (int[])state.Current.Clone();
                if (state.LastShownIndex != 0 && !permutations.NextPermutation(next))
                {
                    throw new InvalidOperationException("The session permutation state is inconsistent.");
                }

                var index = state.LastShownIndex + 1;
                var updated = state with
                {
                    LastShownIndex = index,
                    Current = next
                };
                return new SessionUpdate<NextResponse>(
                    updated,
                    new NextResponse(true, index.ToString(CultureInfo.InvariantCulture), next, null));
            },
            cancellationToken);
}

public sealed class AllService(ISessionStore sessions, IPermutationGenerator permutations) : IAllService
{
    private const int DefaultPageSize = 10;
    private const int MaximumPageSize = 100;
    private const string ExhaustedMessage = "אין יותר קומבינציות";

    public Task<AllResponse> GetAllAsync(
        Guid sessionId,
        int? pageSize = null,
        ulong page = 1,
        bool enter = false,
        CancellationToken cancellationToken = default)
    {
        var size = pageSize ?? DefaultPageSize;
        if (size is < 1 or > MaximumPageSize)
        {
            throw new ApplicationValidationException("pageSize must be between 1 and 100.");
        }

        if (page < 1)
        {
            throw new ApplicationValidationException("page must be at least 1.");
        }

        return sessions.UpdateAsync(
            sessionId,
            state =>
            {
                if (state.N == 0)
                {
                    throw new ApplicationValidationException("The session has not been started.");
                }

                var startIndex = enter
                    ? state.LastShownIndex + 1
                    : state.AllStart ?? state.LastShownIndex + 1;

                var updatedState = state with { AllStart = startIndex };
                var remaining = startIndex <= state.Total ? state.Total - startIndex + 1 : 0;
                var totalPages = remaining == 0 ? 0 : (remaining - 1) / (ulong)size + 1;

                if (remaining == 0)
                {
                    return new SessionUpdate<AllResponse>(
                        updatedState,
                        new AllResponse(
                            [],
                            page.ToString(CultureInfo.InvariantCulture),
                            "0",
                            startIndex.ToString(CultureInfo.InvariantCulture),
                            state.Total.ToString(CultureInfo.InvariantCulture),
                            ExhaustedMessage));
                }

                if (page > totalPages)
                {
                    throw new ApplicationValidationException("page is outside the available page range.");
                }

                var pageOffset = checked((page - 1) * (ulong)size);
                var firstIndex = checked(startIndex + pageOffset);
                var itemCount = (int)Math.Min((ulong)size, state.Total - firstIndex + 1);
                var items = new List<AllPermutationItem>(itemCount);
                for (var offset = 0; offset < itemCount; offset++)
                {
                    var index = firstIndex + (ulong)offset;
                    items.Add(new AllPermutationItem(
                        index.ToString(CultureInfo.InvariantCulture),
                        permutations.Unrank(index - 1, state.N)));
                }

                var last = items[^1];
                var newState = updatedState with
                {
                    LastShownIndex = ulong.Parse(last.Index, CultureInfo.InvariantCulture),
                    Current = (int[])last.Permutation.Clone()
                };
                return new SessionUpdate<AllResponse>(
                    newState,
                    new AllResponse(
                        items,
                        page.ToString(CultureInfo.InvariantCulture),
                        totalPages.ToString(CultureInfo.InvariantCulture),
                        startIndex.ToString(CultureInfo.InvariantCulture),
                        state.Total.ToString(CultureInfo.InvariantCulture),
                        null));
            },
            cancellationToken);
    }
}

public sealed class CurrentService(ISessionStore sessions) : ICurrentService
{
    public Task<CurrentResponse> GetCurrentAsync(Guid sessionId, CancellationToken cancellationToken = default) =>
        sessions.UpdateAsync(
            sessionId,
            state =>
            {
                if (state.N == 0)
                {
                    throw new ApplicationValidationException("The session has not been started.");
                }

                return new SessionUpdate<CurrentResponse>(
                    state,
                    new CurrentResponse(
                        state.LastShownIndex == 0
                            ? null
                            : state.LastShownIndex.ToString(CultureInfo.InvariantCulture),
                        state.LastShownIndex == 0 ? [] : (int[])state.Current.Clone()));
            },
            cancellationToken);
}

public sealed class ResetService(ISessionStore sessions) : IResetService
{
    public async Task ResetAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        var removed = await sessions.RemoveAsync(sessionId, cancellationToken);
        if (!removed)
        {
            throw new SessionNotFoundException(sessionId);
        }
    }
}
