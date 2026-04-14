using ArchLucid.Core.Authorization;
using ArchLucid.Api.Models;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ArchLucid.Api.Controllers;

/// <summary>
/// Diagnostic endpoint for inspecting the caller's authenticated identity and claims.
/// </summary>
/// <remarks>
/// Requires <see cref="ArchLucidPolicies.ReadAuthority"/>. Useful for debugging token claims in development and staging.
/// Intentionally does not use <c>[EnableRateLimiting]</c>: unversioned <c>api/auth</c> diagnostic with minimal payload;
/// use API gateway or reverse-proxy throttling in shared environments if abuse is a concern.
/// </remarks>
[Authorize(Policy = ArchLucidPolicies.ReadAuthority)]
[ApiController]
[Route("api/auth")]
public sealed class AuthDebugController : ControllerBase
{
    /// <summary>Returns the caller's identity name and full claims list.</summary>
    /// <returns>200 with a <see cref="CallerIdentityResponse"/> containing the caller's name and claims.</returns>
    [HttpGet("me")]
    [ProducesResponseType(typeof(CallerIdentityResponse), StatusCodes.Status200OK)]
    public IActionResult Me()
    {
        CallerIdentityResponse response = new()
        {
            Name = User.Identity?.Name,
            Claims = User.Claims
                .Select(x => new CallerClaimResponse { Type = x.Type, Value = x.Value })
                .ToList()
        };

        return Ok(response);
    }
}
