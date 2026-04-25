using ArchLucid.Api.ProblemDetails;
using ArchLucid.Contracts.Findings;
using ArchLucid.Core.Authorization;
using ArchLucid.Core.Scoping;
using ArchLucid.Persistence.Interfaces;

using Asp.Versioning;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ArchLucid.Api.Controllers.Findings;

/// <summary>Read-only finding inspector (deterministic persisted explainability).</summary>
[ApiController]
[Authorize(Policy = ArchLucidPolicies.ReadAuthority)]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/findings")]
[EnableRateLimiting("fixed")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public sealed class FindingInspectController(
    IFindingInspectReadRepository findingInspectReadRepository,
    IScopeContextProvider scopeContextProvider) : ControllerBase
{
    private readonly IFindingInspectReadRepository _findingInspectReadRepository =
        findingInspectReadRepository ?? throw new ArgumentNullException(nameof(findingInspectReadRepository));

    private readonly IScopeContextProvider _scopeContextProvider =
        scopeContextProvider ?? throw new ArgumentNullException(nameof(scopeContextProvider));

    /// <summary>Returns persisted payload, rule linkage, evidence citations, and best-effort audit correlation.</summary>
    [HttpGet("{findingId}/inspect")]
    [ProducesResponseType(typeof(FindingInspectResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetInspectAsync(string findingId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(findingId))
            return this.BadRequestProblem("Finding id is required.", ProblemTypes.ValidationFailed);

        ScopeContext scope = _scopeContextProvider.GetCurrentScope();
        FindingInspectResponse? body = await _findingInspectReadRepository.GetInspectAsync(scope, findingId, ct);

        if (body is null)
            return this.NotFoundProblem(
                $"Finding '{findingId.Trim()}' was not found in the current scope.",
                ProblemTypes.ResourceNotFound);

        return Ok(body);
    }
}
