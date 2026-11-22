namespace CombinationsGenerator.Domain;

public sealed class PermutationGenerator : IPermutationGenerator
{
    private readonly FactorialTable _factorials = new();

    public ulong Factorial(int n) => _factorials.Get(n);

    public bool NextPermutation(int[] permutation)
    {
        ArgumentNullException.ThrowIfNull(permutation);
        ValidatePermutation(permutation);

        var i = permutation.Length - 2;
        while (i >= 0 && permutation[i] >= permutation[i + 1])
        {
            i--;
        }

        if (i < 0)
        {
            return false;
        }

        var j = permutation.Length - 1;
        while (permutation[j] <= permutation[i])
        {
            j--;
        }

        (permutation[i], permutation[j]) = (permutation[j], permutation[i]);
        Array.Reverse(permutation, i + 1, permutation.Length - i - 1);
        return true;
    }

    public int[] Unrank(ulong zeroBasedIndex, int n)
    {
        if (n is < 1 or > 20)
        {
            throw new ArgumentOutOfRangeException(nameof(n), "n must be between 1 and 20.");
        }

        var total = Factorial(n);
        if (zeroBasedIndex >= total)
        {
            throw new ArgumentOutOfRangeException(nameof(zeroBasedIndex), "Index must be less than n!.");
        }

        var available = new int[n];
        for (var i = 0; i < n; i++)
        {
            available[i] = i + 1;
        }

        var result = new int[n];
        for (var i = 0; i < n; i++)
        {
            var blockSize = Factorial(n - i - 1);
            var digit = (int)(zeroBasedIndex / blockSize);
            zeroBasedIndex %= blockSize;

            result[i] = available[digit];
            Array.Copy(available, digit + 1, available, digit, available.Length - digit - 1);
            Array.Resize(ref available, n - i - 1);
        }

        return result;
    }

    public ulong Rank(int[] permutation)
    {
        ArgumentNullException.ThrowIfNull(permutation);
        ValidatePermutation(permutation);

        var rank = 0UL;
        var available = new int[permutation.Length];
        for (var i = 0; i < available.Length; i++)
        {
            available[i] = i + 1;
        }

        for (var i = 0; i < permutation.Length; i++)
        {
            var digit = Array.IndexOf(available, permutation[i]);
            if (digit < 0)
            {
                throw new ArgumentException("Permutation contains invalid values.", nameof(permutation));
            }

            rank += (ulong)digit * Factorial(permutation.Length - i - 1);
            Array.Copy(available, digit + 1, available, digit, available.Length - digit - 1);
            Array.Resize(ref available, available.Length - 1);
        }

        return rank;
    }

    private static void ValidatePermutation(int[] permutation)
    {
        if (permutation.Length is < 1 or > 20)
        {
            throw new ArgumentOutOfRangeException(nameof(permutation), "Permutation length must be between 1 and 20.");
        }

        var seen = new bool[permutation.Length];
        foreach (var value in permutation)
        {
            if (value < 1 || value > permutation.Length)
            {
                throw new ArgumentException("Permutation values must be in the range 1..n.", nameof(permutation));
            }

            var index = value - 1;
            if (seen[index])
            {
                throw new ArgumentException("Permutation values must be unique.", nameof(permutation));
            }

            seen[index] = true;
        }
    }
}
