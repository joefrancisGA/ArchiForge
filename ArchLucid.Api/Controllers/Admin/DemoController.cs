using ArchLucid.Core.Authorization;
using ArchLucid.Host.Core.Configuration;
using ArchLucid.Api.ProblemDetails;
using ArchLucid.Application.Bootstrap;

using Asp.Versioning;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace ArchLucid.Api.Controllers;

/// <summary>Development-only endpoints for deterministic trusted-baseline demo data (Corrected 50R / 49R pass 2).</summary>
[ApiController]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/demo")]
[EnableRateLimiting("expensive")]
public sealed class DemoController(
    IDemoSeedService demoSeedService,
    IOptions<DemoOptions> demoOptions,
    IWebHostEnvironment environment) : ControllerBase
{
    /// <summary>Runs the Contoso Retail Modernization demo seed. No-op for missing rows; safe to repeat.</summary>
    /// <remarks>Available only when <c>Demo:Enabled</c> is true and the host environment is Development.</remarks>
    [HttpPost("seed")]
    [Authorize(Policy = ArchLucidPolicies.ExecuteAuthority)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SeedAsync(CancellationToken cancellationToken = default)
    {
        if (!environment.IsDevelopment())
        {
            return this.NotFoundProblem(
                "Demo seed is available only in Development environment.",
                ProblemTypes.ResourceNotFound);
        }

        if (!demoOptions.Value.Enabled)
        {
            return this.BadRequestProblem(
                "Demo seeding is disabled. Set Demo:Enabled to true in configuration.",
                ProblemTypes.BadRequest);
        }

        await demoSeedService.SeedAsync(cancellationToken);
        return NoContent();
    }
}
