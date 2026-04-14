using ArchLucid.Core.Authorization;
using ArchLucid.Core.Scoping;

using Asp.Versioning;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ArchLucid.Api.Controllers;

/// <summary>
/// Returns the resolved <see cref="ScopeContext"/> for the current request (claims, headers, or ambient override).
/// </summary>
/// <remarks>
/// Intended for development and troubleshooting multi-tenant routing. Same resolution path as governance, compliance, and scoped repositories.
/// </remarks>
[ApiController]
[Authorize(Policy = ArchLucidPolicies.ReadAuthority)]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/scope")]
[EnableRateLimiting("fixed")]
public sealed class ScopeDebugController(IScopeContextProvider scopeProvider) : ControllerBase
{
    /// <summary>GET current tenant, workspace, and project ids.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ScopeContext), StatusCodes.Status200OK)]
    public IActionResult GetScope()
    {
        ScopeContext scope = scopeProvider.GetCurrentScope();
        return Ok(scope);
    }
}
