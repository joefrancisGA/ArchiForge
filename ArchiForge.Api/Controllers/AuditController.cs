using ArchiForge.Api.Auth.Models;
using ArchiForge.Core.Audit;
using ArchiForge.Core.Scoping;
using ArchiForge.Persistence.Audit;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ArchiForge.Api.Controllers;

[ApiController]
[Authorize(Policy = ArchiForgePolicies.ReadAuthority)]
[ApiVersion("1.0")]
[Route("api/audit")]
[EnableRateLimiting("fixed")]
public sealed class AuditController : ControllerBase
{
    private readonly IAuditRepository _repo;
    private readonly IScopeContextProvider _scopeProvider;

    public AuditController(IAuditRepository repo, IScopeContextProvider scopeProvider)
    {
        _repo = repo;
        _scopeProvider = scopeProvider;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<AuditEvent>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAudit([FromQuery] int take = 100, CancellationToken ct = default)
    {
        var scope = _scopeProvider.GetCurrentScope();

        var events = await _repo.GetByScopeAsync(
            scope.TenantId,
            scope.WorkspaceId,
            scope.ProjectId,
            take,
            ct);

        return Ok(events);
    }
}
