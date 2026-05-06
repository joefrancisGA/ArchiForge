using ArchLucid.Contracts.Architecture;
using ArchLucid.Contracts.Findings;
using ArchLucid.Core.Scoping;
using ArchLucid.Persistence.Interfaces;
using ArchLucid.Persistence.Models;

namespace ArchLucid.Application.ExecutiveSummary;
/// <inheritdoc cref = "IExecutiveSummaryService"/>
public sealed class ExecutiveSummaryService(IRunRepository runRepository, IRunDetailQueryService runDetailQueryService) : IExecutiveSummaryService
{
    private readonly byte __primaryConstructorArgumentValidation = __ValidatePrimaryConstructorArguments(runRepository, runDetailQueryService);
    private static byte __ValidatePrimaryConstructorArguments(ArchLucid.Persistence.Interfaces.IRunRepository runRepository, ArchLucid.Application.IRunDetailQueryService runDetailQueryService)
    {
        ArgumentNullException.ThrowIfNull(runRepository);
        ArgumentNullException.ThrowIfNull(runDetailQueryService);
        return (byte)0;
    }

    private readonly IRunRepository _runRepository = runRepository ?? throw new ArgumentNullException(nameof(runRepository));
    private readonly IRunDetailQueryService _runDetailQueryService = runDetailQueryService ?? throw new ArgumentNullException(nameof(runDetailQueryService));
    public async Task<ExecutiveSummaryResponse> GenerateSummaryAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        ScopeContext scope = new()
        {
            TenantId = tenantId,
            WorkspaceId = Guid.Empty,
            ProjectId = Guid.Empty
        };
        IReadOnlyList<RunRecord> recentRuns = await _runRepository.ListRecentInScopeAsync(scope, 1, cancellationToken);
        if (recentRuns.Count == 0)
        {
            return new ExecutiveSummaryResponse
            {
                TenantId = tenantId.ToString("N"),
                SecurityPostureScore = 100,
                TechDebtRiskScore = 100,
                ComplianceAlignmentScore = 100
            };
        }

        RunRecord latestRun = recentRuns[0];
        ArchitectureRunDetail? detail = await _runDetailQueryService.GetRunDetailAsync(latestRun.RunId.ToString("N"), cancellationToken);
        if (detail is null)
        {
            return new ExecutiveSummaryResponse
            {
                TenantId = tenantId.ToString("N"),
                LatestRunId = latestRun.RunId.ToString("N"),
                LatestRunCompletedUtc = latestRun.CompletedUtc,
                SecurityPostureScore = 100,
                TechDebtRiskScore = 100,
                ComplianceAlignmentScore = 100
            };
        }

        int securityScore = 100;
        int techDebtScore = 100;
        int complianceScore = 100;
        foreach (var result in detail.Results)
        {
            foreach (var finding in result.Findings)
            {
                int penalty = finding.Severity switch
                {
                    FindingSeverity.Critical => 20,
                    FindingSeverity.Error => 10,
                    FindingSeverity.Warning => 5,
                    FindingSeverity.Info => 1,
                    _ => 0
                };
                if (string.Equals(finding.Category, "Security", StringComparison.OrdinalIgnoreCase))
                {
                    securityScore -= penalty;
                }
                else if (string.Equals(finding.Category, "Compliance", StringComparison.OrdinalIgnoreCase))
                {
                    complianceScore -= penalty;
                }
                else
                {
                    // Default to Tech Debt for Architecture, Topology, Cost, etc.
                    techDebtScore -= penalty;
                }
            }
        }

        return new ExecutiveSummaryResponse
        {
            TenantId = tenantId.ToString("N"),
            LatestRunId = latestRun.RunId.ToString("N"),
            LatestRunCompletedUtc = latestRun.CompletedUtc,
            SecurityPostureScore = Math.Clamp(securityScore, 0, 100),
            TechDebtRiskScore = Math.Clamp(techDebtScore, 0, 100),
            ComplianceAlignmentScore = Math.Clamp(complianceScore, 0, 100)
        };
    }
}