using ArchLucid.Api.Demo;
using ArchLucid.Api.ProblemDetails;
using ArchLucid.Application;

using Asp.Versioning;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ArchLucid.Api.Controllers.Demo;

/// <summary>Anonymous simulator-only onboarding path (<c>/v1/demo/quickstart</c>).</summary>
[ApiController]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/demo")]
[EnableRateLimiting("expensive")]
[AllowAnonymous]
public sealed class QuickStartController(QuickStartService quickStartService) : ControllerBase
{
    private readonly QuickStartService _quickStartService =
        quickStartService ?? throw new ArgumentNullException(nameof(quickStartService));

    /// <summary>Runs a deterministic simulator pipeline scoped to demo tenant/workspace/project.</summary>
    [HttpPost("quickstart")]
    [ProducesResponseType(typeof(DemoQuickStartResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> QuickStartAsync(
        [FromBody] DemoQuickStartRequest? request,
        CancellationToken cancellationToken)
    {
        if (request is null)
            return this.BadRequestProblem("Request body is required.", ProblemTypes.ValidationFailed);

        try
        {
            DemoQuickStartResponse body = await _quickStartService.RunAsync(request, cancellationToken)
                .ConfigureAwait(false);

            return Ok(body);
        }
        catch (ConflictException ex)
        {
            return this.ConflictProblem(ex.Message, ProblemTypes.Conflict);
        }
        catch (RunNotFoundException ex)
        {
            return this.NotFoundProblem(ex.Message, ProblemTypes.RunNotFound);
        }
        catch (InvalidOperationException ex)
        {
            return this.BadRequestProblem(ex.Message, ProblemTypes.ValidationFailed);
        }
    }
}
