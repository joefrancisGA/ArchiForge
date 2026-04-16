using System.Text.Json;

using ArchLucid.Core.Authorization;
using ArchLucid.Api.ProblemDetails;
using ArchLucid.Core.Audit;
using ArchLucid.Core.Scoping;
using ArchLucid.Decisioning.Advisory.Delivery;
using ArchLucid.Decisioning.Advisory.Scheduling;
using ArchLucid.Persistence;

using Asp.Versioning;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ArchLucid.Api.Controllers;

/// <summary>
/// Manages <see cref="DigestSubscription"/> routes for architecture digests (email/webhook delivery after advisory scans).
/// </summary>
/// <remarks>
/// Parallels alert routing: create/list/toggle subscriptions and inspect <see cref="DigestDeliveryAttempt"/> history.
/// Invoked after <see cref="IArchitectureDigestRepository"/> persistence from <c>AdvisoryScanRunner</c> via <see cref="IDigestDeliveryDispatcher"/>.
/// </remarks>
[ApiController]
[Authorize(Policy = ArchLucidPolicies.ReadAuthority)]
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
    [Authorize(Policy = ArchLucidPolicies.ExecuteAuthority)]
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
    [Authorize(Policy = ArchLucidPolicies.ExecuteAuthority)]
    [ProducesResponseType(typeof(DigestSubscription), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Toggle(
        Guid subscriptionId,
        CancellationToken ct = default)
    {
        DigestSubscription? subscription = await subscriptionRepository.GetByIdAsync(subscriptionId, ct);
        if (subscription is null)
            return this.NotFoundProblem($"Digest subscription '{subscriptionId}' was not found.", ProblemTypes.ResourceNotFound);

        ScopeContext scope = scopeProvider.GetCurrentScope();
        if (!MatchesScope(subscription, scope))
            return this.NotFoundProblem($"Digest subscription '{subscriptionId}' was not found in the current scope.", ProblemTypes.ResourceNotFound);

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
    public async Task<IActionResult> GetAttempts(
        Guid subscriptionId,
        [FromQuery] int take = 50,
        CancellationToken ct = default)
    {
        DigestSubscription? subscription = await subscriptionRepository.GetByIdAsync(subscriptionId, ct);
        if (subscription is null)
            return this.NotFoundProblem($"Digest subscription '{subscriptionId}' was not found.", ProblemTypes.ResourceNotFound);

        ScopeContext scope = scopeProvider.GetCurrentScope();
        if (!MatchesScope(subscription, scope))
            return this.NotFoundProblem($"Digest subscription '{subscriptionId}' was not found in the current scope.", ProblemTypes.ResourceNotFound);

        IReadOnlyList<DigestDeliveryAttempt> attempts = await attemptRepository.ListBySubscriptionAsync(subscriptionId, Math.Clamp(take, 1, 200), ct);
        return Ok(attempts);
    }

    /// <summary>All delivery attempts recorded for a digest that belongs to the current scope.</summary>
    [HttpGet("digests/{digestId:guid}/attempts")]
    [ProducesResponseType(typeof(IReadOnlyList<DigestDeliveryAttempt>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAttemptsForDigest(
        Guid digestId,
        CancellationToken ct = default)
    {
        ScopeContext scope = scopeProvider.GetCurrentScope();
        ArchitectureDigest? digest = await digestRepository.GetByIdAsync(digestId, ct);
        if (digest is null)
            return this.NotFoundProblem($"Digest '{digestId}' was not found.", ProblemTypes.ResourceNotFound);
        if (digest.TenantId != scope.TenantId ||
            digest.WorkspaceId != scope.WorkspaceId ||
            digest.ProjectId != scope.ProjectId)
            return this.NotFoundProblem($"Digest '{digestId}' was not found in the current scope.", ProblemTypes.ResourceNotFound);

        IReadOnlyList<DigestDeliveryAttempt> attempts = await attemptRepository.ListByDigestAsync(digestId, ct);
        return Ok(attempts);
    }

    private static bool MatchesScope(DigestSubscription subscription, ScopeContext scope) =>
        subscription.TenantId == scope.TenantId &&
        subscription.WorkspaceId == scope.WorkspaceId &&
        subscription.ProjectId == scope.ProjectId;
}
