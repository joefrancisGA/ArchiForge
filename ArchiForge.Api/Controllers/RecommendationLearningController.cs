using System.Text.Json;

using ArchiForge.Api.Auth.Models;
using ArchiForge.Core.Audit;
using ArchiForge.Core.Scoping;
using ArchiForge.Decisioning.Advisory.Learning;

using Asp.Versioning;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ArchiForge.Api.Controllers;

/// <summary>
/// Reads and rebuilds <see cref="RecommendationLearningProfile"/> aggregates for the caller’s scope (acceptance/rejection patterns by category, urgency, etc.).
/// </summary>
/// <remarks>
/// Profiles feed composite alert metrics (acceptance rate via <c>AlertMetricSnapshotBuilder</c>) and advisory UX. Rebuild scans recent recommendation rows via
/// <c>RecommendationLearningService</c>. Routes: <c>api/recommendation-learning</c>.
/// </remarks>
[ApiController]
[Authorize(Policy = ArchiForgePolicies.ReadAuthority)]
[ApiVersion("1.0")]
[Route("api/recommendation-learning")]
[EnableRateLimiting("fixed")]
public sealed class RecommendationLearningController(
    IRecommendationLearningService learningService,
    IScopeContextProvider scopeProvider,
    IAuditService auditService)
    : ControllerBase
{
    /// <summary>Returns the newest stored profile for the scope, or 404 if none exists.</summary>
    [HttpGet("latest")]
    [ProducesResponseType(typeof(RecommendationLearningProfile), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RecommendationLearningProfile>> GetLatest(CancellationToken ct = default)
    {
        ScopeContext scope = scopeProvider.GetCurrentScope();

        RecommendationLearningProfile? profile = await learningService.GetLatestProfileAsync(
            scope.TenantId,
            scope.WorkspaceId,
            scope.ProjectId,
            ct);

        if (profile is null)
            return NotFound();

        return Ok(profile);
    }

    /// <summary>Recomputes the recommendation learning profile from history, persists it, and records an audit event.</summary>
    /// <remarks>
    /// Scans recent recommendation acceptance/rejection rows for the current scope via
    /// <c>RebuildProfileAsync</c> and overwrites the stored profile. An audit event of type
    /// <c>RecommendationLearningProfileRebuilt</c> is written after a successful rebuild.
    /// Requires <see cref="ArchiForgePolicies.ExecuteAuthority"/>.
    /// </remarks>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The newly rebuilt <see cref="RecommendationLearningProfile"/>.</returns>
    [HttpPost("rebuild")]
    [Authorize(Policy = ArchiForgePolicies.ExecuteAuthority)]
    [ProducesResponseType(typeof(RecommendationLearningProfile), StatusCodes.Status200OK)]
    public async Task<ActionResult<RecommendationLearningProfile>> Rebuild(CancellationToken ct = default)
    {
        ScopeContext scope = scopeProvider.GetCurrentScope();

        RecommendationLearningProfile profile = await learningService.RebuildProfileAsync(
            scope.TenantId,
            scope.WorkspaceId,
            scope.ProjectId,
            ct);

        await auditService.LogAsync(
            new AuditEvent
            {
                EventType = AuditEventTypes.RecommendationLearningProfileRebuilt,
                DataJson = JsonSerializer.Serialize(new { generatedUtc = profile.GeneratedUtc }),
            },
            ct);

        return Ok(profile);
    }
}
