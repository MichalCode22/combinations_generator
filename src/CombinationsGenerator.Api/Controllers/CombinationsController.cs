using CombinationsGenerator.Application.Services;
using CombinationsGenerator.Application.SessionStore;
using Microsoft.AspNetCore.Mvc;

namespace CombinationsGenerator.Api.Controllers;

[ApiController]
[Route("api/combinations")]
public sealed class CombinationsController(
    IStartService startService,
    INextService nextService,
    IAllService allService,
    ICurrentService currentService,
    IResetService resetService) : ControllerBase
{
    [HttpPost("start")]
    [ProducesResponseType<StartResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<StartResponse>> Start(
        [FromBody] StartRequest request,
        CancellationToken cancellationToken)
    {
        var response = await startService.StartAsync(request, cancellationToken);
        return Ok(response);
    }

    [HttpPost("next")]
    [ProducesResponseType<NextResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<NextResponse>> Next(CancellationToken cancellationToken)
    {
        var response = await nextService.NextAsync(GetSessionId(), cancellationToken);
        return Ok(response);
    }

    [HttpGet("all")]
    [ProducesResponseType<AllResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AllResponse>> GetAll(
        [FromQuery] int? pageSize,
        [FromQuery] ulong page = 1,
        [FromQuery] bool enter = false,
        CancellationToken cancellationToken = default)
    {
        var response = await allService.GetAllAsync(
            GetSessionId(),
            pageSize,
            page,
            enter,
            cancellationToken);
        return Ok(response);
    }

    [HttpGet("current")]
    [ProducesResponseType<CurrentResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CurrentResponse>> GetCurrent(CancellationToken cancellationToken)
    {
        var response = await currentService.GetCurrentAsync(GetSessionId(), cancellationToken);
        return Ok(response);
    }

    [HttpDelete]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Reset(CancellationToken cancellationToken)
    {
        await resetService.ResetAsync(GetSessionId(), cancellationToken);
        return NoContent();
    }

    private Guid GetSessionId()
    {
        if (Request.Headers.TryGetValue("X-Session-Id", out var value)
            && Guid.TryParse(value, out var sessionId))
        {
            return sessionId;
        }

        throw new SessionNotFoundException(Guid.Empty);
    }
}
