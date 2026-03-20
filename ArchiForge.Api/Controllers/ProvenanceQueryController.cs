using ArchiForge.Api.Auth.Models;
using ArchiForge.Core.Scoping;
using ArchiForge.Persistence.Provenance;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ArchiForge.Api.Controllers;

[ApiController]
[Authorize(Policy = ArchiForgePolicies.ReadAuthority)]
[ApiVersion("1.0")]
[Route("api/authority")]
[EnableRateLimiting("fixed")]
public sealed class ProvenanceQueryController : ControllerBase
{
    private readonly IProvenanceSnapshotRepository _repo;
    private readonly IScopeContextProvider _scopeProvider;

    public ProvenanceQueryController(
        IProvenanceSnapshotRepository repo,
        IScopeContextProvider scopeProvider)
    {
        _repo = repo;
        _scopeProvider = scopeProvider;
    }

    [HttpGet("runs/{runId:guid}/provenance")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProvenance(Guid runId, CancellationToken ct = default)
    {
        var scope = _scopeProvider.GetCurrentScope();
        var snapshot = await _repo.GetByRunIdAsync(scope, runId, ct);
        if (snapshot is null)
            return NotFound();

        return Ok(snapshot);
    }
}
