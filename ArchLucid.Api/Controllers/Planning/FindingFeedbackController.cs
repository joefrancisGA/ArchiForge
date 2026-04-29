using ArchLucid.Api.Attributes;
using ArchLucid.Api.Models;
using ArchLucid.Api.ProblemDetails;
using ArchLucid.Core.Authorization;
using ArchLucid.Core.Feedback;
using ArchLucid.Core.Scoping;
using ArchLucid.Core.Tenancy;
using ArchLucid.Persistence.Queries;

using Asp.Versioning;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ArchLucid.Api.Controllers.Planning;

/// <summary>Records per-finding thumbs feedback for operator instrumentation (tenant-scoped).</summary>
[ApiController]
[Authorize(Policy = ArchLucidPolicies.ExecuteAuthority)]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/explain")]
[EnableRateLimiting("fixed")]
[RequiresCommercialTenantTier(TenantTier.Standard)]
public sealed class FindingFeedbackController(
    IAuthorityQueryService authorityQuery,
    IFindingFeedbackRepository findingFeedbackRepository,
    IScopeContextProvider scopeProvider,
    ILogger<FindingFeedbackController> logger) : ControllerBase
{
    private readonly IAuthorityQueryService _authorityQuery =
        authorityQuery ?? throw new ArgumentNullException(nameof(authorityQuery));

    private readonly IFindingFeedbackRepository _findingFeedbackRepository =
        findingFeedbackRepository ?? throw new ArgumentNullException(nameof(findingFeedbackRepository));

    private readonly ILogger<FindingFeedbackController> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

    private readonly IScopeContextProvider _scopeProvider =
        scopeProvider ?? throw new ArgumentNullException(nameof(scopeProvider));

    /// <summary>Append-only thumbs vote for one finding on a run.</summary>
    [HttpPost("runs/{runId:guid}/findings/{findingId}/feedback")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PostFindingFeedbackAsync(
        Guid runId,
        string findingId,
        [FromBody] FindingFeedbackPostRequest? request,
        CancellationToken cancellationToken)
    {
        if (request is null)
            return this.BadRequestProblem("Request body is required.", ProblemTypes.RequestBodyRequired);

        if (string.IsNullOrWhiteSpace(findingId))
            return this.BadRequestProblem("Finding id is required.", ProblemTypes.ValidationFailed);

        if (request.Score is not -1 and not 1)
            return this.BadRequestProblem("Score must be -1 or 1.", ProblemTypes.ValidationFailed);

        ScopeContext scope = _scopeProvider.GetCurrentScope();
        RunDetailDto? detail = await _authorityQuery.GetRunDetailAsync(scope, runId, cancellationToken);

        if (detail?.FindingsSnapshot?.Findings is not { Count: > 0 } list)
            return this.NotFoundProblem(
                $"Run '{runId}' has no findings snapshot in the current scope.",
                ProblemTypes.RunNotFound);

        bool found = list.Any(f => string.Equals(f.FindingId, findingId, StringComparison.OrdinalIgnoreCase));

        if (!found)
            return this.NotFoundProblem(
                $"Finding '{findingId}' was not found on run '{runId}'.",
                ProblemTypes.ResourceNotFound);

        FindingFeedbackSubmission submission = new()
        {
            TenantId = scope.TenantId,
            WorkspaceId = scope.WorkspaceId,
            ProjectId = scope.ProjectId,
            RunId = runId,
            FindingId = findingId.Trim(),
            Score = request.Score
        };

        await _findingFeedbackRepository.InsertAsync(submission, cancellationToken);

        _logger.LogInformation(
            "Finding feedback recorded for run {RunId} score {Score}.",
            runId,
            request.Score);

        return NoContent();
    }
}
