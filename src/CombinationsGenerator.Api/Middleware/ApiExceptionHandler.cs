using CombinationsGenerator.Application.Services;
using CombinationsGenerator.Application.SessionStore;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace CombinationsGenerator.Api.Middleware;

public sealed class ApiExceptionHandler(ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (status, title, detail) = exception switch
        {
            SessionNotFoundException =>
                (StatusCodes.Status404NotFound, "Not Found", "The requested session was not found or has expired."),
            ApplicationValidationException validation =>
                (StatusCodes.Status400BadRequest, "Bad Request", validation.Message),
            SessionLimitExceededException =>
                (StatusCodes.Status503ServiceUnavailable, "Service Unavailable", "The session limit has been reached."),
            _ => (0, string.Empty, string.Empty)
        };

        if (status == 0)
        {
            return false;
        }

        logger.LogWarning(exception, "Handled API exception with status {StatusCode}.", status);
        httpContext.Response.StatusCode = status;
        httpContext.Response.ContentType = "application/problem+json";
        await JsonSerializer.SerializeAsync(
            httpContext.Response.Body,
            new ProblemDetails
            {
                Type = "about:blank",
                Status = status,
                Title = title,
                Detail = detail,
                Instance = httpContext.Request.Path
            },
            new JsonSerializerOptions(JsonSerializerDefaults.Web),
            cancellationToken: cancellationToken);
        return true;
    }
}
