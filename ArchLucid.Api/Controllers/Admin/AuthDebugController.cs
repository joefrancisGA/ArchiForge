using ArchLucid.Api.Models;
using ArchLucid.Core.Authorization;
using ArchLucid.Core.Authority;
using ArchLucid.Core.Scoping;

using Asp.Versioning;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ArchLucid.Api.Controllers.Admin;

/// <summary>
///     Diagnostic endpoint for inspecting the caller's authenticated identity and claims.
/// </summary>
/// <remarks>
///     Requires <see cref="ArchLucidPolicies.ReadAuthority" />. Useful for debugging token claims in development and
///     staging.
///     Intentionally does not use <c>[EnableRateLimiting]</c>: unversioned <c>api/auth</c> diagnostic with minimal
///     payload;
///     use API gateway or reverse-proxy throttling in shared environments if abuse is a concern.
/// </remarks>
[Authorize(Policy = ArchLucidPolicies.ReadAuthority)]
[ApiController]
[ApiVersionNeutral]
[Route("api/auth")]
public sealed class AuthDebugController(
    IScopeContextProvider scopeProvider,
    ICommittedArchitectureReviewFlagReader committedArchitectureReviewFlagReader) : ControllerBase
{
    private readonly IScopeContextProvider _scopeProvider =
        scopeProvider ?? throw new ArgumentNullException(nameof(scopeProvider));

    private readonly ICommittedArchitectureReviewFlagReader _committedArchitectureReviewFlagReader =
        committedArchitectureReviewFlagReader
        ?? throw new ArgumentNullException(nameof(committedArchitectureReviewFlagReader));

    /// <summary>Returns the caller's identity name and full claims list.</summary>
    /// <returns>200 with a <see cref="CallerIdentityResponse" /> containing the caller's name and claims.</returns>
    [HttpGet("me")]
    [ProducesResponseType(typeof(CallerIdentityResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Me(CancellationToken cancellationToken)
    {
        ScopeContext scope = _scopeProvider.GetCurrentScope();
        bool hasCommitted =
            await _committedArchitectureReviewFlagReader.TenantHasCommittedArchitectureReviewAsync(
                scope,
                cancellationToken);

        CallerIdentityResponse response = new()
        {
            Name = User.Identity?.Name,
            Claims = User.Claims
                .Select(x => new CallerClaimResponse { Type = x.Type, Value = x.Value })
                .ToList(),
            HasCommittedArchitectureReview = hasCommitted
        };

        return Ok(response);
    }
}
