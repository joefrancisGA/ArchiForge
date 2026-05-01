using System.Text.Json;

using ArchLucid.Application.Common;
using ArchLucid.Core.Audit;
using ArchLucid.Core.Tenancy;

using Microsoft.Extensions.Logging;

namespace ArchLucid.Application.Tenancy;

/// <inheritdoc cref="ITenantProvisioningService" />
public sealed class TenantProvisioningService(
    ITenantRepository tenantRepository,
    IActorContext actorContext,
    IAuditService auditService,
    ILogger<TenantProvisioningService> logger) : ITenantProvisioningService
{
    private readonly IActorContext
        _actorContext = actorContext ?? throw new ArgumentNullException(nameof(actorContext));

    private readonly IAuditService
        _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));

    private readonly ILogger<TenantProvisioningService> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

    private readonly ITenantRepository _tenantRepository =
        tenantRepository ?? throw new ArgumentNullException(nameof(tenantRepository));

    /// <inheritdoc />
    public async Task<TenantProvisioningResult> ProvisionAsync(TenantProvisioningRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ArgumentException("Tenant name is required.", nameof(request));

        if (string.IsNullOrWhiteSpace(request.AdminEmail) ||
            !request.AdminEmail.Contains('@', StringComparison.Ordinal))
            throw new ArgumentException("Admin email is required.", nameof(request));

        string slug = TenantSlugNormalizer.FromName(request.Name);

        TenantRecord? existing = await _tenantRepository.GetBySlugAsync(slug, ct);

        if (existing is not null)
        {
            TenantWorkspaceLink? link = await _tenantRepository.GetFirstWorkspaceAsync(existing.Id, ct);

            if (link is null)

                throw new InvalidOperationException(
                    $"Tenant '{existing.Id:D}' exists without a workspace row; data is inconsistent.");

            return new TenantProvisioningResult
            {
                TenantId = existing.Id,
                DefaultWorkspaceId = link.WorkspaceId,
                DefaultProjectId = link.DefaultProjectId,
                WasAlreadyProvisioned = true
            };
        }

        Guid tenantId = Guid.NewGuid();
        Guid workspaceId = Guid.NewGuid();
        Guid projectId = Guid.NewGuid();

        await _tenantRepository.InsertTenantAsync(
            tenantId,
            request.Name.Trim(),
            slug,
            request.Tier,
            request.EntraTenantId,
            ct);

        try
        {
            await _tenantRepository.InsertWorkspaceAsync(
                workspaceId,
                tenantId,
                "Default",
                projectId,
                ct);
        }
        catch (Exception ex)
        {
            if (_logger.IsEnabled(LogLevel.Critical))

                _logger.LogCritical(
                    ex,
                    "Tenant {TenantId} inserted but default workspace insert failed; manual cleanup may be required.",
                    tenantId);

            throw;
        }

        string actor = string.IsNullOrWhiteSpace(request.AuditActorOverride)
            ? _actorContext.GetActor()
            : request.AuditActorOverride.Trim();

        await _auditService.LogAsync(
            new AuditEvent
            {
                EventType = AuditEventTypes.TenantProvisioned,
                ActorUserId = actor,
                ActorUserName = actor,
                TenantId = tenantId,
                WorkspaceId = workspaceId,
                ProjectId = projectId,
                DataJson = JsonSerializer.Serialize(
                    new { slug, request.AdminEmail, tier = request.Tier.ToString() })
            },
            ct);

        return new TenantProvisioningResult
        {
            TenantId = tenantId,
            DefaultWorkspaceId = workspaceId,
            DefaultProjectId = projectId,
            WasAlreadyProvisioned = false
        };
    }
}
