using ArchLucid.Api.Models.Diagnostics;
using ArchLucid.Api.ProblemDetails;
using ArchLucid.Application.Diagnostics;
using ArchLucid.Core.Authorization;
using ArchLucid.Core.Configuration;

using Asp.Versioning;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace ArchLucid.Api.Controllers.Diagnostics;

/// <summary>
///     Writes synthetic audit markers for empty-tenant exploration (Development or Demo:Enabled only).
/// </summary>
[ApiController]
[Authorize(Policy = ArchLucidPolicies.RequireAdmin)]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/diagnostics")]
[EnableRateLimiting("expensive")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public sealed class SyntheticOperatorDemoPackController(
    ISyntheticOperatorDemoPackWriter writer,
    IOptions<DemoOptions> demoOptions,
    IWebHostEnvironment environment) : ControllerBase
{
    private readonly ISyntheticOperatorDemoPackWriter _writer =
        writer ?? throw new ArgumentNullException(nameof(writer));

    private readonly IOptions<DemoOptions> _demoOptions =
        demoOptions ?? throw new ArgumentNullException(nameof(demoOptions));

    private readonly IWebHostEnvironment _environment =
        environment ?? throw new ArgumentNullException(nameof(environment));

    /// <summary>Appends five durable synthetic audit markers (purge via type or JSON flag in payload).</summary>
    [HttpPost("synthetic-operator-demo-pack")]
    [ProducesResponseType(typeof(SyntheticOperatorDemoPackResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PostAsync(CancellationToken cancellationToken = default)
    {
        if (!_environment.IsDevelopment() && !_demoOptions.Value.Enabled)
            return this.NotFoundProblem(
                "Synthetic demo pack is available only when Demo:Enabled is true or the host is Development.",
                ProblemTypes.ResourceNotFound);

        int n = await _writer.WriteMarkerEventsAsync(cancellationToken).ConfigureAwait(false);

        return Ok(new SyntheticOperatorDemoPackResponse { AuditEventsWritten = n });
    }
}
