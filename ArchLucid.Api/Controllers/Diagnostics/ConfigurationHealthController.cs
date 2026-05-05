using ArchLucid.Core.Authorization;

using Asp.Versioning;

using ArchLucid.Host.Core.Diagnostics;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ArchLucid.Api.Controllers.Diagnostics;

/// <summary>Admin connectivity probes for pilot SQL / OIDC / Key Vault setup.</summary>
[ApiController]
[Authorize(Policy = ArchLucidPolicies.RequireAdmin)]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/diagnostics")]
[EnableRateLimiting("expensive")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public sealed class ConfigurationHealthController(IConfigurationHealthProbe probe) : ControllerBase
{
    private readonly IConfigurationHealthProbe _probe = probe ?? throw new ArgumentNullException(nameof(probe));

    /// <summary>Runs non-mutating configuration probes (no secret values returned).</summary>
    [HttpGet("configuration-health")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(ConfigurationHealthReport), StatusCodes.Status200OK)]
    public async Task<ActionResult<ConfigurationHealthReport>> GetAsync(CancellationToken cancellationToken)
    {
        ConfigurationHealthReport report = await _probe.ProbeAsync(cancellationToken).ConfigureAwait(false);

        return Ok(report);
    }
}
