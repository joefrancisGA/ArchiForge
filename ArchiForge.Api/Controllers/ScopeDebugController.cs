using ArchiForge.Api.Auth.Models;
using ArchiForge.Core.Scoping;

using Asp.Versioning;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ArchiForge.Api.Controllers;

/// <summary>
/// Returns the resolved <see cref="ScopeContext"/> for the current request (claims, headers, or ambient override).
/// </summary>
/// <remarks>
/// Intended for development and troubleshooting multi-tenant routing. Same resolution path as governance, compliance, and scoped repositories.
/// </remarks>
[ApiController]
[Authorize(Policy = ArchiForgePolicies.ReadAuthority)]
[ApiVersion("1.0")]
[Route("api/scope")]
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
