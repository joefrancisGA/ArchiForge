using ArchiForge.Api.Auth.Models;
using ArchiForge.Api.Learning;
using ArchiForge.Api.Models.Learning;
using ArchiForge.Api.ProblemDetails;
using ArchiForge.Api.Services.Learning;
using ArchiForge.Contracts.ProductLearning;
using ArchiForge.Core.Scoping;

using Asp.Versioning;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ArchiForge.Api.Controllers;

/// <summary>
/// 59R learning-to-planning read APIs: themes, improvement plans, priority scores, and evidence-style counts.
/// </summary>
[ApiController]
[Authorize(Policy = ArchiForgePolicies.ReadAuthority)]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/learning")]
[EnableRateLimiting("fixed")]
public sealed class LearningController(
    ILearningPlanningReadService learningReadService,
    IScopeContextProvider scopeProvider)
    : ControllerBase
{
    /// <summary>Lists improvement themes for the current scope (newest first).</summary>
    [HttpGet("themes")]
    [ProducesResponseType(typeof(LearningThemesListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetThemes(
        [FromQuery] string? maxThemes,
        CancellationToken cancellationToken)
    {
        if (!LearningPlanningQueryParser.TryParseMaxItems(maxThemes, "maxThemes", out int take, out string? maxError))

            return this.BadRequestProblem(maxError!, ProblemTypes.ValidationFailed);


        ProductLearningScope scope = ToProductLearningScope(scopeProvider.GetCurrentScope());
        LearningThemesListResponse body = await learningReadService.GetThemesAsync(scope, take, cancellationToken);

        return Ok(body);
    }

    /// <summary>Lists improvement plans for the current scope (newest first), with theme evidence counts when resolvable.</summary>
    [HttpGet("plans")]
    [ProducesResponseType(typeof(LearningPlansListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetPlans(
        [FromQuery] string? maxPlans,
        CancellationToken cancellationToken)
    {
        if (!LearningPlanningQueryParser.TryParseMaxItems(maxPlans, "maxPlans", out int take, out string? maxError))

            return this.BadRequestProblem(maxError!, ProblemTypes.ValidationFailed);


        ProductLearningScope scope = ToProductLearningScope(scopeProvider.GetCurrentScope());
        LearningPlansListResponse body = await learningReadService.GetPlansAsync(scope, take, cancellationToken);

        return Ok(body);
    }

    /// <summary>Loads a single improvement plan with action steps, link counts, and optional parent theme.</summary>
    [HttpGet("plans/{id}")]
    [ProducesResponseType(typeof(LearningPlanDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPlanById(string id, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(id))

            return this.BadRequestProblem("Path parameter 'id' is required.", ProblemTypes.ValidationFailed);


        if (!Guid.TryParse(id.Trim(), out Guid planId))

            return this.BadRequestProblem("Path parameter 'id' must be a valid GUID.", ProblemTypes.ValidationFailed);


        ProductLearningScope scope = ToProductLearningScope(scopeProvider.GetCurrentScope());
        LearningPlanDetailResponse? plan =
            await learningReadService.GetPlanByIdAsync(planId, scope, cancellationToken);

        if (plan is null)

            return this.NotFoundProblem(
                $"Improvement plan '{planId}' was not found in the current scope.",
                ProblemTypes.LearningImprovementPlanNotFound);


        return Ok(plan);
    }

    /// <summary>Aggregated KPIs: theme/plan counts, theme evidence totals, max plan priority, linked signal totals.</summary>
    [HttpGet("summary")]
    [ProducesResponseType(typeof(LearningSummaryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetSummary(
        [FromQuery] string? maxThemes,
        [FromQuery] string? maxPlans,
        CancellationToken cancellationToken)
    {
        if (!LearningPlanningQueryParser.TryParseMaxItems(maxThemes, "maxThemes", out int themeTake, out string? themeError))

            return this.BadRequestProblem(themeError!, ProblemTypes.ValidationFailed);


        if (!LearningPlanningQueryParser.TryParseMaxItems(maxPlans, "maxPlans", out int planTake, out string? planError))

            return this.BadRequestProblem(planError!, ProblemTypes.ValidationFailed);


        ProductLearningScope scope = ToProductLearningScope(scopeProvider.GetCurrentScope());
        LearningSummaryResponse body =
            await learningReadService.GetSummaryAsync(scope, themeTake, planTake, cancellationToken);

        return Ok(body);
    }

    private static ProductLearningScope ToProductLearningScope(ScopeContext scopeContext)
    {
        if (scopeContext is null)

            throw new ArgumentNullException(nameof(scopeContext));


        return new ProductLearningScope
        {
            TenantId = scopeContext.TenantId,
            WorkspaceId = scopeContext.WorkspaceId,
            ProjectId = scopeContext.ProjectId,
        };
    }
}
