namespace CombinationsGenerator.Domain;

public interface IPermutationGenerator
{
    ulong Factorial(int n);
    bool NextPermutation(int[] permutation);
    int[] Unrank(ulong zeroBasedIndex, int n);
    ulong Rank(int[] permutation);
}
