using System.Text.Json;

using ArchLucid.Application.Bootstrap;
using ArchLucid.Core.Audit;
using ArchLucid.Core.Scoping;
using ArchLucid.Core.Tenancy;

using Microsoft.Extensions.Logging;

namespace ArchLucid.Application.Tenancy;

/// <inheritdoc cref="ITrialTenantBootstrapService"/>
public sealed class TrialTenantBootstrapService(
    IDemoSeedService demoSeedService,
    ITenantRepository tenantRepository,
    IAuditService auditService,
    ILogger<TrialTenantBootstrapService> logger) : ITrialTenantBootstrapService
{
    private readonly IDemoSeedService _demoSeedService =
        demoSeedService ?? throw new ArgumentNullException(nameof(demoSeedService));

    private readonly ITenantRepository _tenantRepository =
        tenantRepository ?? throw new ArgumentNullException(nameof(tenantRepository));

    private readonly IAuditService _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));

    private readonly ILogger<TrialTenantBootstrapService> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

    /// <inheritdoc />
    public async Task TryBootstrapAfterSelfRegistrationAsync(
        TenantProvisioningResult result,
        string auditActorEmail,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(result);

        if (string.IsNullOrWhiteSpace(auditActorEmail))
            throw new ArgumentException("Audit actor email is required.", nameof(auditActorEmail));

        if (result.WasAlreadyProvisioned)
            return;

        ContosoRetailDemoIds demoIds = ContosoRetailDemoIds.ForTenant(result.TenantId);

        ScopeContext scope = new()
        {
            TenantId = result.TenantId,
            WorkspaceId = result.DefaultWorkspaceId,
            ProjectId = result.DefaultProjectId,
        };

        using (SqlRowLevelSecurityBypassAmbient.Enter())
        using (AmbientScopeContext.Push(scope))
        {
            try
            {
                await _demoSeedService.SeedAsync(cancellationToken);

                DateTimeOffset start = DateTimeOffset.UtcNow;
                DateTimeOffset expires = start.AddDays(14);

                await _tenantRepository.CommitSelfServiceTrialAsync(
                    result.TenantId,
                    start,
                    expires,
                    runsLimit: 10,
                    seatsLimit: 3,
                    demoIds.AuthorityRunBaselineId,
                    cancellationToken);

                string actor = auditActorEmail.Trim();

                await _auditService.LogAsync(
                    new AuditEvent
                    {
                        EventType = AuditEventTypes.TrialProvisioned,
                        ActorUserId = actor,
                        ActorUserName = actor,
                        TenantId = result.TenantId,
                        WorkspaceId = result.DefaultWorkspaceId,
                        ProjectId = result.DefaultProjectId,
                        DataJson = JsonSerializer.Serialize(
                            new
                            {
                                trialExpiresUtc = expires,
                                sampleRunId = demoIds.AuthorityRunBaselineId,
                            }),
                    },
                    cancellationToken);
            }
            catch (Exception ex)
            {
                if (_logger.IsEnabled(LogLevel.Error))
                {
                    _logger.LogError(
                        ex,
                        "Trial bootstrap failed for tenant {TenantId}; tenant row exists without trial metadata.",
                        result.TenantId);
                }
            }
        }
    }
}
