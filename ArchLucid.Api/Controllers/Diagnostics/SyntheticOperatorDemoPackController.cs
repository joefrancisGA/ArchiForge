using System.Text.Json;

using ArchLucid.Api.Models.Diagnostics;
using ArchLucid.Api.ProblemDetails;
using ArchLucid.Application.Common;
using ArchLucid.Application.Diagnostics;
using ArchLucid.Core.Audit;
using ArchLucid.Core.Authorization;
using ArchLucid.Core.Configuration;
using ArchLucid.Core.Scoping;

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
    IWebHostEnvironment environment,
    IAuditService auditService,
    IActorContext actorContext,
    IScopeContextProvider scopeContextProvider) : ControllerBase
{
    private readonly ISyntheticOperatorDemoPackWriter _writer =
        writer ?? throw new ArgumentNullException(nameof(writer));

    private readonly IOptions<DemoOptions> _demoOptions =
        demoOptions ?? throw new ArgumentNullException(nameof(demoOptions));

    private readonly IWebHostEnvironment _environment =
        environment ?? throw new ArgumentNullException(nameof(environment));

    private readonly IAuditService _auditService =
        auditService ?? throw new ArgumentNullException(nameof(auditService));

    private readonly IActorContext _actorContext =
        actorContext ?? throw new ArgumentNullException(nameof(actorContext));

    private readonly IScopeContextProvider _scopeContextProvider =
        scopeContextProvider ?? throw new ArgumentNullException(nameof(scopeContextProvider));

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

        ScopeContext scope = _scopeContextProvider.GetCurrentScope();
        string auditActor = _actorContext.GetActor();

        await _auditService.LogAsync(
            new AuditEvent
            {
                EventType = AuditEventTypes.SyntheticOperatorDemoPackInvoked,
                ActorUserId = auditActor,
                ActorUserName = auditActor,
                TenantId = scope.TenantId,
                WorkspaceId = scope.WorkspaceId,
                ProjectId = scope.ProjectId,
                CorrelationId = HttpContext.TraceIdentifier,
                DataJson = JsonSerializer.Serialize(
                    new
                    {
                        syntheticDemoPack = true,
                        auditEventsWritten = n,
                        demoEnabled = _demoOptions.Value.Enabled,
                        hostIsDevelopment = _environment.IsDevelopment()
                    })
            },
            cancellationToken);

        return Ok(new SyntheticOperatorDemoPackResponse { AuditEventsWritten = n });
    }
}
