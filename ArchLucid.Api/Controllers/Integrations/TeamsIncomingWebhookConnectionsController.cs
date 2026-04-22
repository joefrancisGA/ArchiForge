using System.Text.Json;

using ArchLucid.Api.ProblemDetails;
using ArchLucid.Contracts.Integrations;
using ArchLucid.Core.Audit;
using ArchLucid.Core.Authorization;
using ArchLucid.Core.Notifications.Teams;
using ArchLucid.Core.Scoping;
using ArchLucid.Persistence.Data.Repositories;

using Asp.Versioning;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ArchLucid.Api.Controllers.Integrations;

/// <summary>Per-tenant Microsoft Teams notification connector configuration (Key Vault secret name only).</summary>
[ApiController]
[Authorize]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/integrations/teams")]
[EnableRateLimiting("fixed")]
public sealed class TeamsIncomingWebhookConnectionsController(
    IScopeContextProvider scopeProvider,
    ITenantTeamsIncomingWebhookConnectionRepository connectionRepository,
    IAuditService auditService) : ControllerBase
{
    private readonly IScopeContextProvider _scopeProvider =
        scopeProvider ?? throw new ArgumentNullException(nameof(scopeProvider));

    private readonly ITenantTeamsIncomingWebhookConnectionRepository _connectionRepository =
        connectionRepository ?? throw new ArgumentNullException(nameof(connectionRepository));

    private readonly IAuditService _auditService =
        auditService ?? throw new ArgumentNullException(nameof(auditService));

    /// <summary>Returns the Key Vault reference (never the webhook URL) for the caller's tenant.</summary>
    [HttpGet("connections")]
    [Authorize(Policy = ArchLucidPolicies.ReadAuthority)]
    [ProducesResponseType(typeof(TeamsIncomingWebhookConnectionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetConnection(CancellationToken cancellationToken)
    {
        ScopeContext scope = _scopeProvider.GetCurrentScope();

        TeamsIncomingWebhookConnectionResponse? row =
            await _connectionRepository.GetAsync(scope.TenantId, cancellationToken);

        if (row is null)
        {
            return Ok(
                new TeamsIncomingWebhookConnectionResponse
                {
                    TenantId = scope.TenantId,
                    IsConfigured = false,
                    Label = null,
                    KeyVaultSecretName = null,
                    EnabledTriggers = TeamsNotificationTriggerCatalog.All,
                    UpdatedUtc = DateTimeOffset.UtcNow,
                });
        }

        return Ok(row);
    }

    /// <summary>Returns the canonical v1 catalog of Teams notification triggers an operator can opt in to.</summary>
    [HttpGet("triggers")]
    [Authorize(Policy = ArchLucidPolicies.ReadAuthority)]
    [ProducesResponseType(typeof(IReadOnlyList<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public IActionResult GetTriggerCatalog() => Ok(TeamsNotificationTriggerCatalog.All);

    /// <summary>Upserts the Key Vault secret name used to resolve the Teams incoming webhook URL at delivery time.</summary>
    [HttpPost("connections")]
    [Authorize(Policy = ArchLucidPolicies.ExecuteAuthority)]
    [ProducesResponseType(typeof(TeamsIncomingWebhookConnectionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpsertConnection(
        [FromBody] TeamsIncomingWebhookConnectionUpsertRequest? body,
        CancellationToken cancellationToken)
    {
        if (body is null)
        {
            return this.BadRequestProblem(
                "Request body is required.",
                ProblemTypes.ValidationFailed);
        }

        if (string.IsNullOrWhiteSpace(body.KeyVaultSecretName))
        {
            return this.BadRequestProblem(
                "KeyVaultSecretName is required.",
                ProblemTypes.ValidationFailed);
        }

        string trimmed = body.KeyVaultSecretName.Trim();

        if (trimmed.Contains("://", StringComparison.Ordinal))
        {
            return this.BadRequestProblem(
                "KeyVaultSecretName must be a Key Vault secret name or id reference — raw webhook URLs are not stored in ArchLucid SQL.",
                ProblemTypes.ValidationFailed);
        }

        if (body.EnabledTriggers is not null)
        {
            IReadOnlyList<string> unknown = TeamsNotificationTriggerCatalog.Unknown(body.EnabledTriggers);

            if (unknown.Count > 0)
            {
                return this.BadRequestProblem(
                    $"EnabledTriggers contains unknown trigger names: {string.Join(", ", unknown)}. Allowed values: {string.Join(", ", TeamsNotificationTriggerCatalog.All)}.",
                    ProblemTypes.ValidationFailed);
            }
        }

        ScopeContext scope = _scopeProvider.GetCurrentScope();

        TeamsIncomingWebhookConnectionResponse? saved = await _connectionRepository.UpsertAsync(
            scope.TenantId,
            trimmed,
            string.IsNullOrWhiteSpace(body.Label) ? null : body.Label.Trim(),
            body.EnabledTriggers,
            cancellationToken);

        if (saved is null)
        {
            return NotFound();
        }

        await _auditService.LogAsync(
            new AuditEvent
            {
                EventType = AuditEventTypes.TenantTeamsIncomingWebhookConnectionUpserted,
                ActorUserId = User.Identity?.Name ?? "operator",
                ActorUserName = User.Identity?.Name ?? "operator",
                TenantId = scope.TenantId,
                WorkspaceId = scope.WorkspaceId,
                ProjectId = scope.ProjectId,
                DataJson = JsonSerializer.Serialize(new
                {
                    keyVaultSecretNameLength = trimmed.Length,
                    enabledTriggerCount = saved.EnabledTriggers.Count,
                }),
            },
            cancellationToken);

        return Ok(saved);
    }

    /// <summary>Removes the Teams webhook Key Vault reference for the caller's tenant.</summary>
    [HttpDelete("connections")]
    [Authorize(Policy = ArchLucidPolicies.ExecuteAuthority)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteConnection(CancellationToken cancellationToken)
    {
        ScopeContext scope = _scopeProvider.GetCurrentScope();

        bool removed = await _connectionRepository.DeleteAsync(scope.TenantId, cancellationToken);

        if (removed)
        {
            await _auditService.LogAsync(
                new AuditEvent
                {
                    EventType = AuditEventTypes.TenantTeamsIncomingWebhookConnectionRemoved,
                    ActorUserId = User.Identity?.Name ?? "operator",
                    ActorUserName = User.Identity?.Name ?? "operator",
                    TenantId = scope.TenantId,
                    WorkspaceId = scope.WorkspaceId,
                    ProjectId = scope.ProjectId,
                    DataJson = "{}",
                },
                cancellationToken);
        }

        return NoContent();
    }
}
