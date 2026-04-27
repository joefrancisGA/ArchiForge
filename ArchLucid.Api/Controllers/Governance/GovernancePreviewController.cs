using ArchLucid.Api.Attributes;
using ArchLucid.Api.Models;
using ArchLucid.Api.ProblemDetails;
using ArchLucid.Application;
using ArchLucid.Application.Governance.Preview;
using ArchLucid.Contracts.Governance.Preview;
using ArchLucid.Core.Authorization;
using ArchLucid.Core.Tenancy;

using Asp.Versioning;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ArchLucid.Api.Controllers.Governance;

/// <summary>
///     Read-only governance preview: compare manifest governance for hypothetical activation or between environments.
/// </summary>
[ApiController]
[Authorize(Policy = ArchLucidPolicies.ReadAuthority)]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/governance-preview")]
[EnableRateLimiting("fixed")]
[RequiresCommercialTenantTier(TenantTier.Standard)]
[ProducesResponseType(StatusCodes.Status404NotFound)]
public sealed class GovernancePreviewController(
    IGovernancePreviewService previewService,
    ILogger<GovernancePreviewController> logger) : ControllerBase
{
    /// <summary>Preview governance diff if the given run/manifest were activated into an environment (no persistence).</summary>
    [HttpPost("preview")]
    [ProducesResponseType(typeof(GovernancePreviewResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Preview(
        [FromBody] CreateGovernancePreviewRequest? body,
        CancellationToken cancellationToken)
    {
        if (body is null)
            return this.BadRequestProblem("Request body is required.", ProblemTypes.RequestBodyRequired);

        try
        {
            GovernancePreviewRequest request = new()
            {
                RunId = body.RunId, ManifestVersion = body.ManifestVersion, Environment = body.Environment
            };
            GovernancePreviewResult result = await previewService.PreviewActivationAsync(request, cancellationToken);
            return Ok(result);
        }
        catch (RunNotFoundException ex)
        {
            logger.LogWarning(ex, "Preview failed: run not found.");
            return this.NotFoundProblem(ex.Message, ProblemTypes.RunNotFound);
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Preview failed: validation error.");
            return this.BadRequestProblem(ex.Message, ProblemTypes.ValidationFailed);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Preview failed: invalid operation.");
            return this.BadRequestProblem(ex.Message, ProblemTypes.BadRequest);
        }
    }

    /// <summary>Compare governance between the currently active manifests in two environments (read-only).</summary>
    [HttpPost("compare-environments")]
    [ProducesResponseType(typeof(GovernanceEnvironmentComparisonResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CompareEnvironments(
        [FromBody] CreateGovernanceEnvironmentComparisonRequest? body,
        CancellationToken cancellationToken)
    {
        if (body is null)
            return this.BadRequestProblem("Request body is required.", ProblemTypes.RequestBodyRequired);

        try
        {
            GovernanceEnvironmentComparisonRequest request = new()
            {
                SourceEnvironment = body.SourceEnvironment, TargetEnvironment = body.TargetEnvironment
            };
            GovernanceEnvironmentComparisonResult result =
                await previewService.CompareEnvironmentsAsync(request, cancellationToken);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "CompareEnvironments failed: validation error.");
            return this.BadRequestProblem(ex.Message, ProblemTypes.ValidationFailed);
        }
    }
}
