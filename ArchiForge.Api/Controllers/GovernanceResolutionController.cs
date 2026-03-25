using System.Text.Json;

using ArchiForge.Api.Auth.Models;
using ArchiForge.Core.Audit;
using ArchiForge.Core.Scoping;
using ArchiForge.Decisioning.Governance.PolicyPacks;
using ArchiForge.Decisioning.Governance.Resolution;

using Asp.Versioning;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ArchiForge.Api.Controllers;

/// <summary>
/// HTTP surface for <strong>full governance resolution</strong>: effective merged content, per-item decisions, and conflict records for the current scope.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Why:</strong> Operators need the same explainability the resolver produces without pulling only <see cref="IEffectiveGovernanceLoader"/>
/// (which drops decisions/conflicts). Complements <c>GET …/policy-packs/effective-content</c>.
/// </para>
/// <para>
/// <strong>Auth:</strong> <see cref="ArchiForgePolicies.ReadAuthority"/>; uses fixed window rate limiting like other governance reads.
/// </para>
/// </remarks>
/// <param name="scopeProvider">Ambient tenant/workspace/project from JWT or dev bypass headers.</param>
/// <param name="resolver">Decisioning implementation (<see cref="EffectiveGovernanceResolver"/>).</param>
/// <param name="auditService">Emits <c>GovernanceResolutionExecuted</c> and optionally <c>GovernanceConflictDetected</c>.</param>
[ApiController]
[Authorize(Policy = ArchiForgePolicies.ReadAuthority)]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/governance-resolution")]
[EnableRateLimiting("fixed")]
public sealed class GovernanceResolutionController(
    IScopeContextProvider scopeProvider,
    IEffectiveGovernanceResolver resolver,
    IAuditService auditService) : ControllerBase
{
    /// <summary>
    /// Runs hierarchical governance resolution for the caller’s scope and returns the full <see cref="EffectiveGovernanceResolutionResult"/> JSON.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>200 with body containing <c>effectiveContent</c>, <c>decisions</c>, <c>conflicts</c>, <c>notes</c>.</returns>
    /// <remarks>
    /// Always logs <c>GovernanceResolutionExecuted</c> with decision/conflict counts. When <c>conflicts</c> is non-empty, also logs
    /// <c>GovernanceConflictDetected</c> with a compact projection of conflict keys (avoid huge payloads).
    /// </remarks>
    [HttpGet]
    [ProducesResponseType(typeof(EffectiveGovernanceResolutionResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> Resolve(CancellationToken ct = default)
    {
        ScopeContext scope = scopeProvider.GetCurrentScope();

        EffectiveGovernanceResolutionResult result = await resolver.ResolveAsync(
            scope.TenantId,
            scope.WorkspaceId,
            scope.ProjectId,
            ct).ConfigureAwait(false);

        await auditService.LogAsync(
            new AuditEvent
            {
                EventType = AuditEventTypes.GovernanceResolutionExecuted,
                DataJson = JsonSerializer.Serialize(
                    new
                    {
                        scope.TenantId,
                        scope.WorkspaceId,
                        scope.ProjectId,
                        decisionCount = result.Decisions.Count,
                        conflictCount = result.Conflicts.Count,
                    }),
            },
            ct).ConfigureAwait(false);

        if (result.Conflicts.Count > 0)
        {
            await auditService.LogAsync(
                new AuditEvent
                {
                    EventType = AuditEventTypes.GovernanceConflictDetected,
                    DataJson = JsonSerializer.Serialize(
                        new
                        {
                            scope.TenantId,
                            scope.WorkspaceId,
                            scope.ProjectId,
                            conflictCount = result.Conflicts.Count,
                            conflicts = result.Conflicts.Select(c => new
                            {
                                c.ItemType,
                                c.ItemKey,
                                c.ConflictType,
                            }),
                        }),
                },
                ct).ConfigureAwait(false);
        }

        return Ok(result);
    }
}
