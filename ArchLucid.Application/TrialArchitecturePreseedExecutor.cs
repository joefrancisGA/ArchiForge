using ArchLucid.Application.Runs.Orchestration;
using ArchLucid.Contracts.Common;
using ArchLucid.Contracts.Requests;
using ArchLucid.Core.Scoping;
using ArchLucid.Core.Tenancy;
using Microsoft.Extensions.Logging;

namespace ArchLucid.Application;
/// <summary>Creates, executes (simulator), and commits one authority run for trial welcome UX.</summary>
public sealed class TrialArchitecturePreseedExecutor(ITenantRepository tenantRepository, IArchitectureRunCreateOrchestrator architectureRunCreateOrchestrator, IArchitectureRunExecuteOrchestrator architectureRunExecuteOrchestrator, IArchitectureRunCommitOrchestrator architectureRunCommitOrchestrator, ILogger<TrialArchitecturePreseedExecutor> logger)
{
    private readonly byte __primaryConstructorArgumentValidation = __ValidatePrimaryConstructorArguments(tenantRepository, architectureRunCreateOrchestrator, architectureRunExecuteOrchestrator, architectureRunCommitOrchestrator, logger);
    private static byte __ValidatePrimaryConstructorArguments(ArchLucid.Core.Tenancy.ITenantRepository tenantRepository, ArchLucid.Application.Runs.Orchestration.IArchitectureRunCreateOrchestrator architectureRunCreateOrchestrator, ArchLucid.Application.Runs.Orchestration.IArchitectureRunExecuteOrchestrator architectureRunExecuteOrchestrator, ArchLucid.Application.Runs.Orchestration.IArchitectureRunCommitOrchestrator architectureRunCommitOrchestrator, Microsoft.Extensions.Logging.ILogger<ArchLucid.Application.TrialArchitecturePreseedExecutor> logger)
    {
        ArgumentNullException.ThrowIfNull(tenantRepository);
        ArgumentNullException.ThrowIfNull(architectureRunCreateOrchestrator);
        ArgumentNullException.ThrowIfNull(architectureRunExecuteOrchestrator);
        ArgumentNullException.ThrowIfNull(architectureRunCommitOrchestrator);
        ArgumentNullException.ThrowIfNull(logger);
        return (byte)0;
    }

    private readonly IArchitectureRunCommitOrchestrator _architectureRunCommitOrchestrator = architectureRunCommitOrchestrator ?? throw new ArgumentNullException(nameof(architectureRunCommitOrchestrator));
    private readonly IArchitectureRunCreateOrchestrator _architectureRunCreateOrchestrator = architectureRunCreateOrchestrator ?? throw new ArgumentNullException(nameof(architectureRunCreateOrchestrator));
    private readonly IArchitectureRunExecuteOrchestrator _architectureRunExecuteOrchestrator = architectureRunExecuteOrchestrator ?? throw new ArgumentNullException(nameof(architectureRunExecuteOrchestrator));
    private readonly ILogger<TrialArchitecturePreseedExecutor> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly ITenantRepository _tenantRepository = tenantRepository ?? throw new ArgumentNullException(nameof(tenantRepository));
    public async Task TryProcessTenantAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        TenantWorkspaceLink? link = await _tenantRepository.GetFirstWorkspaceAsync(tenantId, cancellationToken);
        if (link is null)
        {
            if (_logger.IsEnabled(LogLevel.Warning))
                _logger.LogWarning("Trial pre-seed skipped: no workspace for tenant {TenantId}.", tenantId);
            return;
        }

        ScopeContext scope = new()
        {
            TenantId = tenantId,
            WorkspaceId = link.WorkspaceId,
            ProjectId = link.DefaultProjectId
        };
        using (SqlRowLevelSecurityBypassAmbient.Enter())
        using (AmbientScopeContext.Push(scope))
        {
            ArchitectureRequest request = BuildRequest(tenantId);
            CreateRunResult created = await _architectureRunCreateOrchestrator.CreateRunAsync(request, null, cancellationToken);
            string runId = created.Run.RunId;
            await _architectureRunExecuteOrchestrator.ExecuteRunAsync(runId, cancellationToken);
            CommitRunResult committed = await _architectureRunCommitOrchestrator.CommitRunAsync(runId, cancellationToken);
            if (!Guid.TryParseExact(runId, "N", out Guid welcomeRunId))
            {
                if (_logger.IsEnabled(LogLevel.Error))
                    _logger.LogError("Trial pre-seed produced non-Guid run id {RunId} for tenant {TenantId}.", runId, tenantId);
                return;
            }

            await _tenantRepository.MarkTrialArchitecturePreseedCompletedAsync(tenantId, welcomeRunId, cancellationToken);
            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation("Trial architecture pre-seed completed for tenant {TenantId}: run {RunId}, manifest {Version}.", tenantId, runId, committed.Manifest.Metadata.ManifestVersion);
            }
        }
    }

    private static ArchitectureRequest BuildRequest(Guid tenantId)
    {
        string requestId = $"trial-welcome-{tenantId:N}".ToLowerInvariant();
        return new ArchitectureRequest
        {
            RequestId = requestId.Length > 64 ? requestId[..64] : requestId,
            Description = "Design a minimal secure Azure web API with private SQL connectivity and managed identity for secrets — trial welcome pre-seed.",
            SystemName = "TrialWelcomeApi",
            Environment = "prod",
            CloudProvider = CloudProvider.Azure,
            Constraints = ["Private connectivity", "Managed identity"],
            RequiredCapabilities = ["Azure SQL", "App Service or Container Apps"]
        };
    }
}