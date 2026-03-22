using System.Text.Json;
using ArchiForge.Api.Auth.Models;
using ArchiForge.Core.Audit;
using ArchiForge.Core.Scoping;
using ArchiForge.Decisioning.Advisory.Delivery;
using ArchiForge.Persistence.Advisory;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ArchiForge.Api.Controllers;

[ApiController]
[Authorize(Policy = ArchiForgePolicies.ReadAuthority)]
[ApiVersion("1.0")]
[Route("api/digest-subscriptions")]
[EnableRateLimiting("fixed")]
public sealed class DigestSubscriptionsController : ControllerBase
{
    private readonly IScopeContextProvider _scopeProvider;
    private readonly IDigestSubscriptionRepository _subscriptionRepository;
    private readonly IDigestDeliveryAttemptRepository _attemptRepository;
    private readonly IArchitectureDigestRepository _digestRepository;
    private readonly IAuditService _auditService;

    public DigestSubscriptionsController(
        IScopeContextProvider scopeProvider,
        IDigestSubscriptionRepository subscriptionRepository,
        IDigestDeliveryAttemptRepository attemptRepository,
        IArchitectureDigestRepository digestRepository,
        IAuditService auditService)
    {
        _scopeProvider = scopeProvider;
        _subscriptionRepository = subscriptionRepository;
        _attemptRepository = attemptRepository;
        _digestRepository = digestRepository;
        _auditService = auditService;
    }

    [HttpPost]
    [Authorize(Policy = ArchiForgePolicies.ExecuteAuthority)]
    [ProducesResponseType(typeof(DigestSubscription), StatusCodes.Status200OK)]
    public async Task<ActionResult<DigestSubscription>> Create(
        [FromBody] DigestSubscription subscription,
        CancellationToken ct = default)
    {
        var scope = _scopeProvider.GetCurrentScope();

        subscription.SubscriptionId = Guid.NewGuid();
        subscription.TenantId = scope.TenantId;
        subscription.WorkspaceId = scope.WorkspaceId;
        subscription.ProjectId = scope.ProjectId;
        subscription.CreatedUtc = DateTime.UtcNow;
        if (string.IsNullOrWhiteSpace(subscription.MetadataJson))
            subscription.MetadataJson = "{}";

        await _subscriptionRepository.CreateAsync(subscription, ct);

        await _auditService.LogAsync(
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

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<DigestSubscription>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<DigestSubscription>>> List(CancellationToken ct = default)
    {
        var scope = _scopeProvider.GetCurrentScope();

        var result = await _subscriptionRepository.ListByScopeAsync(
            scope.TenantId,
            scope.WorkspaceId,
            scope.ProjectId,
            ct);

        return Ok(result);
    }

    [HttpPost("{subscriptionId:guid}/toggle")]
    [Authorize(Policy = ArchiForgePolicies.ExecuteAuthority)]
    [ProducesResponseType(typeof(DigestSubscription), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DigestSubscription>> Toggle(
        Guid subscriptionId,
        CancellationToken ct = default)
    {
        var subscription = await _subscriptionRepository.GetByIdAsync(subscriptionId, ct);
        if (subscription is null)
            return NotFound();

        var scope = _scopeProvider.GetCurrentScope();
        if (!MatchesScope(subscription, scope))
            return NotFound();

        subscription.IsEnabled = !subscription.IsEnabled;
        await _subscriptionRepository.UpdateAsync(subscription, ct);

        await _auditService.LogAsync(
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

    [HttpGet("{subscriptionId:guid}/attempts")]
    [ProducesResponseType(typeof(IReadOnlyList<DigestDeliveryAttempt>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<DigestDeliveryAttempt>>> GetAttempts(
        Guid subscriptionId,
        [FromQuery] int take = 50,
        CancellationToken ct = default)
    {
        var subscription = await _subscriptionRepository.GetByIdAsync(subscriptionId, ct);
        if (subscription is null)
            return NotFound();

        var scope = _scopeProvider.GetCurrentScope();
        if (!MatchesScope(subscription, scope))
            return NotFound();

        var attempts = await _attemptRepository.ListBySubscriptionAsync(subscriptionId, take, ct);
        return Ok(attempts);
    }

    [HttpGet("digests/{digestId:guid}/attempts")]
    [ProducesResponseType(typeof(IReadOnlyList<DigestDeliveryAttempt>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<DigestDeliveryAttempt>>> GetAttemptsForDigest(
        Guid digestId,
        CancellationToken ct = default)
    {
        var scope = _scopeProvider.GetCurrentScope();
        var digest = await _digestRepository.GetByIdAsync(digestId, ct);
        if (digest is null)
            return NotFound();
        if (digest.TenantId != scope.TenantId ||
            digest.WorkspaceId != scope.WorkspaceId ||
            digest.ProjectId != scope.ProjectId)
            return NotFound();

        var attempts = await _attemptRepository.ListByDigestAsync(digestId, ct);
        return Ok(attempts);
    }

    private static bool MatchesScope(DigestSubscription subscription, ScopeContext scope) =>
        subscription.TenantId == scope.TenantId &&
        subscription.WorkspaceId == scope.WorkspaceId &&
        subscription.ProjectId == scope.ProjectId;
}
