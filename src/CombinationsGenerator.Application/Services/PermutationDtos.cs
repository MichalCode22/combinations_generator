namespace CombinationsGenerator.Application.Services;

public sealed record StartRequest(int N);

public sealed record StartResponse(Guid SessionId, string Total);

public sealed record NextResponse(
    bool HasMore,
    string? Index,
    int[]? Permutation,
    string? Message);

public sealed record AllPermutationItem(string Index, int[] Permutation);

public sealed record AllResponse(
    IReadOnlyList<AllPermutationItem> Items,
    string Page,
    string TotalPages,
    string? StartIndex,
    string Total,
    string? Message);

public sealed record CurrentResponse(string? Index, int[] Permutation);
