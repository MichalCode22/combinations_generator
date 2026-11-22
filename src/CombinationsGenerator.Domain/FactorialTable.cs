namespace CombinationsGenerator.Domain;

public sealed class FactorialTable : IFactorialTable
{
    private static readonly ulong[] Values =
    [
        1UL,
        1UL,
        2UL,
        6UL,
        24UL,
        120UL,
        720UL,
        5040UL,
        40320UL,
        362880UL,
        3628800UL,
        39916800UL,
        479001600UL,
        6227020800UL,
        87178291200UL,
        1307674368000UL,
        20922789888000UL,
        355687428096000UL,
        6402373705728000UL,
        121645100408832000UL,
        2432902008176640000UL
    ];

    public ulong Get(int n)
    {
        if (n is < 0 or > 20)
        {
            throw new ArgumentOutOfRangeException(nameof(n), "n must be between 0 and 20.");
        }

        return Values[n];
    }
}
