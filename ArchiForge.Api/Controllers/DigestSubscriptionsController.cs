using System.Text.Json;

using ArchiForge.Api.Auth.Models;
using ArchiForge.Api.ProblemDetails;
using ArchiForge.Core.Audit;
using ArchiForge.Core.Scoping;
using ArchiForge.Decisioning.Advisory.Delivery;
using ArchiForge.Decisioning.Advisory.Scheduling;
using ArchiForge.Persistence.Advisory;

using Asp.Versioning;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ArchiForge.Api.Controllers;

/// <summary>
/// Manages <see cref="DigestSubscription"/> routes for architecture digests (email/webhook delivery after advisory scans).
/// </summary>
/// <remarks>
/// Parallels alert routing: create/list/toggle subscriptions and inspect <see cref="DigestDeliveryAttempt"/> history.
/// Invoked after <see cref="IArchitectureDigestRepository"/> persistence from <c>AdvisoryScanRunner</c> via <see cref="IDigestDeliveryDispatcher"/>.
/// </remarks>
[ApiController]
[Authorize(Policy = ArchiForgePolicies.ReadAuthority)]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/digest-subscriptions")]
[EnableRateLimiting("fixed")]
public sealed class DigestSubscriptionsController(
    IScopeContextProvider scopeProvider,
    IDigestSubscriptionRepository subscriptionRepository,
    IDigestDeliveryAttemptRepository attemptRepository,
    IArchitectureDigestRepository digestRepository,
    IAuditService auditService)
    : ControllerBase
{
    /// <summary>Creates a subscription stamped with the current scope; mutating action requires execute authority.</summary>
    [HttpPost]
    [Authorize(Policy = ArchiForgePolicies.ExecuteAuthority)]
    [ProducesResponseType(typeof(DigestSubscription), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create(
        [FromBody] DigestSubscription? subscription,
        CancellationToken ct = default)
    {
        if (subscription is null)
            return this.BadRequestProblem("Request body is required.", ProblemTypes.RequestBodyRequired);

        ScopeContext scope = scopeProvider.GetCurrentScope();

        subscription.SubscriptionId = Guid.NewGuid();
        subscription.TenantId = scope.TenantId;
        subscription.WorkspaceId = scope.WorkspaceId;
        subscription.ProjectId = scope.ProjectId;
        subscription.CreatedUtc = DateTime.UtcNow;
        if (string.IsNullOrWhiteSpace(subscription.MetadataJson))
            subscription.MetadataJson = "{}";

        await subscriptionRepository.CreateAsync(subscription, ct);

        await auditService.LogAsync(
            new AuditEvent
            {
                EventType = AuditEventTypes.DigestSubscriptionCreated,
                DataJson = JsonSerializer.Serialize(new
                {
                    subscriptionId = subscription.SubscriptionId,
                    subscription.Name,
                    subscription.ChannelType
                }),
            },
            ct);

        return Ok(subscription);
    }

    /// <summary>Lists digest subscriptions for the caller’s tenant/workspace/project.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<DigestSubscription>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<DigestSubscription>>> List(CancellationToken ct = default)
    {
        ScopeContext scope = scopeProvider.GetCurrentScope();

        IReadOnlyList<DigestSubscription> result = await subscriptionRepository.ListByScopeAsync(
            scope.TenantId,
            scope.WorkspaceId,
            scope.ProjectId,
            ct);

        return Ok(result);
    }

    /// <summary>Toggles <see cref="DigestSubscription.IsEnabled"/> when the row is in scope.</summary>
    [HttpPost("{subscriptionId:guid}/toggle")]
    [Authorize(Policy = ArchiForgePolicies.ExecuteAuthority)]
    [ProducesResponseType(typeof(DigestSubscription), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DigestSubscription>> Toggle(
        Guid subscriptionId,
        CancellationToken ct = default)
    {
        DigestSubscription? subscription = await subscriptionRepository.GetByIdAsync(subscriptionId, ct);
        if (subscription is null)
            return NotFound();

        ScopeContext scope = scopeProvider.GetCurrentScope();
        if (!MatchesScope(subscription, scope))
            return NotFound();

        subscription.IsEnabled = !subscription.IsEnabled;
        await subscriptionRepository.UpdateAsync(subscription, ct);

        await auditService.LogAsync(
            new AuditEvent
            {
                EventType = AuditEventTypes.DigestSubscriptionToggled,
                DataJson = JsonSerializer.Serialize(new
                {
                    subscriptionId,
                    enabled = subscription.IsEnabled
                }),
            },
            ct);

        return Ok(subscription);
    }

    /// <summary>Recent delivery attempts for a subscription in scope.</summary>
    [HttpGet("{subscriptionId:guid}/attempts")]
    [ProducesResponseType(typeof(IReadOnlyList<DigestDeliveryAttempt>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<DigestDeliveryAttempt>>> GetAttempts(
        Guid subscriptionId,
        [FromQuery] int take = 50,
        CancellationToken ct = default)
    {
        DigestSubscription? subscription = await subscriptionRepository.GetByIdAsync(subscriptionId, ct);
        if (subscription is null)
            return NotFound();

        ScopeContext scope = scopeProvider.GetCurrentScope();
        if (!MatchesScope(subscription, scope))
            return NotFound();

        IReadOnlyList<DigestDeliveryAttempt> attempts = await attemptRepository.ListBySubscriptionAsync(subscriptionId, Math.Clamp(take, 1, 200), ct);
        return Ok(attempts);
    }

    /// <summary>All delivery attempts recorded for a digest that belongs to the current scope.</summary>
    [HttpGet("digests/{digestId:guid}/attempts")]
    [ProducesResponseType(typeof(IReadOnlyList<DigestDeliveryAttempt>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<DigestDeliveryAttempt>>> GetAttemptsForDigest(
        Guid digestId,
        CancellationToken ct = default)
    {
        ScopeContext scope = scopeProvider.GetCurrentScope();
        ArchitectureDigest? digest = await digestRepository.GetByIdAsync(digestId, ct);
        if (digest is null)
            return NotFound();
        if (digest.TenantId != scope.TenantId ||
            digest.WorkspaceId != scope.WorkspaceId ||
            digest.ProjectId != scope.ProjectId)
            return NotFound();

        IReadOnlyList<DigestDeliveryAttempt> attempts = await attemptRepository.ListByDigestAsync(digestId, ct);
        return Ok(attempts);
    }

    private static bool MatchesScope(DigestSubscription subscription, ScopeContext scope) =>
        subscription.TenantId == scope.TenantId &&
        subscription.WorkspaceId == scope.WorkspaceId &&
        subscription.ProjectId == scope.ProjectId;
}
