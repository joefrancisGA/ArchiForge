using System.Text.Json;

using ArchLucid.Application.Bootstrap;
using ArchLucid.Application.Identity;
using ArchLucid.Core.Audit;
using ArchLucid.Core.Diagnostics;
using ArchLucid.Core.Scoping;
using ArchLucid.Core.Tenancy;

using Microsoft.Extensions.Logging;

namespace ArchLucid.Application.Tenancy;

/// <inheritdoc cref="ITrialTenantBootstrapService"/>
public sealed class TrialTenantBootstrapService(
    IDemoSeedService demoSeedService,
    ITenantRepository tenantRepository,
    IAuditService auditService,
    ITrialBootstrapEmailVerificationPolicy emailVerificationPolicy,
    ILogger<TrialTenantBootstrapService> logger) : ITrialTenantBootstrapService
{
    private readonly IDemoSeedService _demoSeedService =
        demoSeedService ?? throw new ArgumentNullException(nameof(demoSeedService));

    private readonly ITenantRepository _tenantRepository =
        tenantRepository ?? throw new ArgumentNullException(nameof(tenantRepository));

    private readonly IAuditService _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));

    private readonly ITrialBootstrapEmailVerificationPolicy _emailVerificationPolicy =
        emailVerificationPolicy ?? throw new ArgumentNullException(nameof(emailVerificationPolicy));

    private readonly ILogger<TrialTenantBootstrapService> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

    /// <inheritdoc />
    public async Task TryBootstrapAfterSelfRegistrationAsync(
        TenantProvisioningResult result,
        string auditActorEmail,
        TrialSignupBaselineReviewCycleCapture? baselineReviewCycle,
        TrialSignupCompanyProfileCapture? companyProfile,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(result);

        if (string.IsNullOrWhiteSpace(auditActorEmail))
            throw new ArgumentException("Audit actor email is required.", nameof(auditActorEmail));

        if (result.WasAlreadyProvisioned)
            return;

        if (!await _emailVerificationPolicy.CanProvisionTrialForRegisteredEmailAsync(auditActorEmail, cancellationToken))
        {
            if (_logger.IsEnabled(LogLevel.Information))

                _logger.LogInformation(
                    "Skipping trial bootstrap for tenant {TenantId}: email verification policy blocked provisioning for {Email}.",
                    result.TenantId,
                    auditActorEmail);


            ArchLucidInstrumentation.RecordTrialSignupFailure("email_verification", "policy_blocked");

            await _auditService.LogAsync(
                new AuditEvent
                {
                    EventType = AuditEventTypes.TrialSignupFailed,
                    ActorUserId = auditActorEmail.Trim(),
                    ActorUserName = auditActorEmail.Trim(),
                    TenantId = result.TenantId,
                    WorkspaceId = result.DefaultWorkspaceId,
                    ProjectId = result.DefaultProjectId,
                    DataJson = JsonSerializer.Serialize(new { stage = "email_verification", reason = "policy_blocked" }),
                },
                cancellationToken);

            return;
        }

        ContosoRetailDemoIds demoIds = ContosoRetailDemoIds.ForTenant(result.TenantId);

        ScopeContext scope = new()
        {
            TenantId = result.TenantId,
            WorkspaceId = result.DefaultWorkspaceId,
            ProjectId = result.DefaultProjectId,
        };

        using (SqlRowLevelSecurityBypassAmbient.Enter())
        using (AmbientScopeContext.Push(scope))

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
                    baselineReviewCycle?.Hours,
                    baselineReviewCycle?.SourceNote,
                    baselineReviewCycle?.CapturedUtc,
                    companyProfile?.CompanySize,
                    companyProfile?.ArchitectureTeamSize,
                    companyProfile?.IndustryVertical,
                    companyProfile?.IndustryVerticalOther,
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

                await _tenantRepository.EnqueueTrialArchitecturePreseedAsync(result.TenantId, cancellationToken);

                ArchLucidInstrumentation.RecordTrialSignup("self_service", "trial_provisioned");
            }
            catch (Exception ex)
            {
                if (_logger.IsEnabled(LogLevel.Error))

                    _logger.LogError(
                        ex,
                        "Trial bootstrap failed for tenant {TenantId}; tenant row exists without trial metadata.",
                        result.TenantId);


                ArchLucidInstrumentation.RecordTrialSignupFailure("trial_bootstrap", ex.GetType().Name);

                await _auditService.LogAsync(
                    new AuditEvent
                    {
                        EventType = AuditEventTypes.TrialSignupFailed,
                        ActorUserId = auditActorEmail.Trim(),
                        ActorUserName = auditActorEmail.Trim(),
                        TenantId = result.TenantId,
                        WorkspaceId = result.DefaultWorkspaceId,
                        ProjectId = result.DefaultProjectId,
                        DataJson = JsonSerializer.Serialize(new { stage = "trial_bootstrap", reason = ex.GetType().Name }),
                    },
                    cancellationToken);
            }

    }
}
