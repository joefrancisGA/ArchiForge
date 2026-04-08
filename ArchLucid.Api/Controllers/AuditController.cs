using ArchLucid.Api.Auth.Models;
using ArchLucid.Core.Audit;
using ArchLucid.Core.Scoping;
using ArchLucid.Persistence.Audit;

using Asp.Versioning;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ArchLucid.Api.Controllers;

/// <summary>
/// Returns a pageable audit event log for the caller's tenant/workspace/project scope.
/// </summary>
/// <remarks>
/// Events are appended by all mutating operations across the ArchLucid API (run creation, governance promotion, alert delivery, etc.).
/// Results are ordered newest-first and capped by the <c>take</c> parameter (max 500).
/// </remarks>
[ApiController]
[Authorize(Policy = ArchLucidPolicies.ReadAuthority)]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/audit")]
[EnableRateLimiting("fixed")]
public sealed class AuditController(IAuditRepository repo, IScopeContextProvider scopeProvider) : ControllerBase
{
    /// <summary>Returns recent audit events for the current scope, newest first.</summary>
    /// <param name="take">Maximum events to return (1–500, default 100).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of <see cref="AuditEvent"/> rows ordered by most-recent first.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<AuditEvent>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAudit([FromQuery] int take = 100, CancellationToken ct = default)
    {
        take = Math.Clamp(take, 1, 500);
        ScopeContext scope = scopeProvider.GetCurrentScope();

        IReadOnlyList<AuditEvent> events = await repo.GetByScopeAsync(
            scope.TenantId,
            scope.WorkspaceId,
            scope.ProjectId,
            take,
            ct);

        return Ok(events);
    }
}
