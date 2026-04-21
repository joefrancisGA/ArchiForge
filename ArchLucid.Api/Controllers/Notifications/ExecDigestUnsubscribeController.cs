using ArchLucid.Api.ProblemDetails;
using ArchLucid.Application.Notifications.Email;
using ArchLucid.Persistence.Data.Repositories;

using Asp.Versioning;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ArchLucid.Api.Controllers.Notifications;

/// <summary>Public unsubscribe endpoint for weekly executive digest (signed token, no interactive auth).</summary>
[ApiController]
[AllowAnonymous]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/notifications/exec-digest")]
public sealed class ExecDigestUnsubscribeController(
    IExecDigestUnsubscribeTokenFactory tokenFactory,
    ITenantExecDigestPreferencesRepository preferencesRepository) : ControllerBase
{
    private readonly IExecDigestUnsubscribeTokenFactory _tokenFactory =
        tokenFactory ?? throw new ArgumentNullException(nameof(tokenFactory));

    private readonly ITenantExecDigestPreferencesRepository _preferencesRepository =
        preferencesRepository ?? throw new ArgumentNullException(nameof(preferencesRepository));

    [HttpGet("unsubscribe")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UnsubscribeAsync([FromQuery] string? token, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return this.BadRequestProblem(
                "token query parameter is required.",
                ProblemTypes.ValidationFailed);
        }

        if (!_tokenFactory.TryParseTenant(token, out Guid tenantId))
        {
            return this.BadRequestProblem(
                "Unsubscribe token is invalid or expired.",
                ProblemTypes.ValidationFailed);
        }

        await _preferencesRepository.TryDisableEmailAsync(tenantId, cancellationToken);

        return Content(
            "Executive digest email has been turned off for this tenant.",
            "text/plain",
            System.Text.Encoding.UTF8);
    }
}
