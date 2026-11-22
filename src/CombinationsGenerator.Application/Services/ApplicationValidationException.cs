namespace CombinationsGenerator.Application.Services;

public sealed class ApplicationValidationException(string message) : Exception(message)
{
}
