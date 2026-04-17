using ArchLucid.Core.Authorization;

using Asp.Versioning;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ArchLucid.Api.Controllers.Admin;

/// <summary>
/// Template controller — move to the correct area namespace (e.g. <c>Planning</c>, <c>Governance</c>)
/// and replace the route with your resource. Register new services in <c>ArchLucid.Host.Composition</c> when needed.
/// </summary>
[ApiController]
[Authorize(Policy = ArchLucidPolicies.ReadAuthority)]
[Route("v{version:apiVersion}/feature-stub")]
[ApiVersion("1.0")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public sealed class FeatureStubController : ControllerBase
{
    /// <summary>Sample read endpoint — delete or replace.</summary>
    [HttpGet("sample")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public IActionResult GetSample()
    {
        return Ok(new { message = "Replace FeatureStub with your feature; see docs/GOLDEN_CHANGE_PATH.md." });
    }
}
