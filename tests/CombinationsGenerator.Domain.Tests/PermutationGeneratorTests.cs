using FluentAssertions;

namespace CombinationsGenerator.Domain.Tests;

public class PermutationGeneratorTests
{
    private readonly PermutationGenerator _generator = new();

    [Theory]
    [InlineData(0, 1UL)]
    [InlineData(1, 1UL)]
    [InlineData(2, 2UL)]
    [InlineData(3, 6UL)]
    [InlineData(4, 24UL)]
    [InlineData(5, 120UL)]
    [InlineData(6, 720UL)]
    [InlineData(7, 5040UL)]
    [InlineData(8, 40320UL)]
    [InlineData(9, 362880UL)]
    [InlineData(10, 3628800UL)]
    [InlineData(11, 39916800UL)]
    [InlineData(12, 479001600UL)]
    [InlineData(13, 6227020800UL)]
    [InlineData(14, 87178291200UL)]
    [InlineData(15, 1307674368000UL)]
    [InlineData(16, 20922789888000UL)]
    [InlineData(17, 355687428096000UL)]
    [InlineData(18, 6402373705728000UL)]
    [InlineData(19, 121645100408832000UL)]
    [InlineData(20, 2432902008176640000UL)]
    public void FactorialReturnsExpectedValue(int n, ulong expected)
    {
        _generator.Factorial(n).Should().Be(expected);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(21)]
    [InlineData(int.MaxValue)]
    public void FactorialRejectsOutsideRange(int n)
    {
        Action act = () => _generator.Factorial(n);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    [InlineData(8)]
    public void NextPermutationMatchesBruteForceLexicographicList(int n)
    {
        var bruteForce = BruteForce(n);
        var permutation = Enumerable.Range(1, n).ToArray();
        var generated = new List<int[]>();

        do
        {
            generated.Add(permutation.ToArray());
        }
        while (_generator.NextPermutation(permutation));

        generated.Count.Should().Be(bruteForce.Count);
        generated.Should().BeEquivalentTo(bruteForce, options => options.WithStrictOrdering());
        generated.First().Should().Equal(Enumerable.Range(1, n));
        generated.Last().Should().Equal(Enumerable.Range(1, n).Reverse());
        generated.Select(item => string.Join(',', item)).Should().OnlyHaveUniqueItems();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    [InlineData(8)]
    public void UnrankAndRankMatchBruteForceForSmallNs(int n)
    {
        var bruteForce = BruteForce(n);

        for (ulong k = 0; k < (ulong)bruteForce.Count; k++)
        {
            var unranked = _generator.Unrank(k, n);
            unranked.Should().Equal(bruteForce[(int)k]);
            _generator.Rank(unranked).Should().Be(k);
        }
    }

    [Theory]
    [InlineData(9)]
    [InlineData(10)]
    [InlineData(12)]
    [InlineData(15)]
    [InlineData(20)]
    public void LargeNUnrankRankRoundTripAndNextIndex(int n)
    {
        var random = new Random(123456 + n);
        var total = _generator.Factorial(n);
        var boundary = new ulong[] { 0, 1, total - 2, total - 1 };
        var indices = new HashSet<ulong>(boundary);

        for (var i = 0; i < 20; i++)
        {
            indices.Add((ulong)random.NextInt64(0, (long)total));
        }

        var block = _generator.Factorial(n - 1);
        for (ulong k = 0; k < total; k += block)
        {
            indices.Add(k);
            if (indices.Count >= 35)
            {
                break;
            }
        }

        foreach (var k in indices)
        {
            var permutation = _generator.Unrank(k, n);
            permutation.Should().HaveCount(n);
            permutation.Should().OnlyContain(value => value >= 1 && value <= n);
            permutation.Distinct().Should().HaveCount(n);
            _generator.Rank(permutation).Should().Be(k);

            var copy = permutation.ToArray();
            if (k + 1 < total)
            {
                var next = _generator.Unrank(k + 1, n);
                _generator.NextPermutation(copy).Should().BeTrue();
                copy.Should().Equal(next);
            }
        }
    }

    [Fact]
    public void NextPermutationOnLastReturnsFalseWithoutMutation()
    {
        var permutation = new[] { 3, 2, 1 };
        var before = permutation.ToArray();

        _generator.NextPermutation(permutation).Should().BeFalse();
        permutation.Should().Equal(before);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public void NextPermutationSupportsSmallNs(int n)
    {
        var permutation = Enumerable.Range(1, n).ToArray();
        var bruteForce = BruteForce(n);
        for (var i = 1; i < bruteForce.Count; i++)
        {
            _generator.NextPermutation(permutation).Should().BeTrue();
            permutation.Should().Equal(bruteForce[i]);
        }

        _generator.NextPermutation(permutation).Should().BeFalse();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(18446744073709551615UL)]
    public void UnrankRejectsNegativeAndOutOfRangeIndices(ulong k)
    {
        Action act = () => _generator.Unrank(k, 1);
        if (k == 0)
        {
            act.Should().NotThrow();
        }
        else
        {
            act.Should().Throw<ArgumentOutOfRangeException>();
        }
    }

    [Fact]
    public void UnrankRejectsOutOfRangeIndex()
    {
        Action act = () => _generator.Unrank(24, 4);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(21)]
    public void UnrankRejectsInvalidN(int n)
    {
        Action act = () => _generator.Unrank(0, n);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void RankRejectsInvalidPermutation()
    {
        Action duplicate = () => _generator.Rank([1, 1]);
        Action outOfRange = () => _generator.Rank([1, 3]);
        Action empty = () => _generator.Rank([]);

        duplicate.Should().Throw<ArgumentException>();
        outOfRange.Should().Throw<ArgumentException>();
        empty.Should().Throw<ArgumentOutOfRangeException>();
    }

    private static List<int[]> BruteForce(int n)
    {
        var result = new List<int[]>();
        var values = Enumerable.Range(1, n).ToArray();
        Generate(values, 0, result);
        return result;
    }

    private static void Generate(int[] values, int start, ICollection<int[]> result)
    {
        if (start == values.Length)
        {
            result.Add(values.ToArray());
            return;
        }

        for (var i = 0; i < values.Length; i++)
        {
            if (Array.IndexOf(values, i + 1, 0, start) >= 0)
            {
                continue;
            }

            values[start] = i + 1;
            Generate(values, start + 1, result);
        }
    }
}
