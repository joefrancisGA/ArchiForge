using ArchLucid.Contracts.Architecture;
using ArchLucid.Contracts.Governance;
using ArchLucid.Core.Scoping;
using ArchLucid.Decisioning.Findings;
using ArchLucid.Decisioning.Manifest;
using ArchLucid.Decisioning.Models;
using ArchLucid.Persistence.Data.Repositories;
using ArchLucid.Persistence.Queries;

namespace ArchLucid.Application.Governance;
/// <inheritdoc cref = "IGovernanceLineageService"/>
public sealed class GovernanceLineageService(IGovernanceApprovalRequestRepository approvalRepo, IGovernancePromotionRecordRepository promotionRepo, IRunDetailQueryService runDetailQuery, IAuthorityQueryService authorityQuery, IScopeContextProvider scopeProvider) : IGovernanceLineageService
{
    private readonly byte __primaryConstructorArgumentValidation = __ValidatePrimaryConstructorArguments(approvalRepo, promotionRepo, runDetailQuery, authorityQuery, scopeProvider);
    private static byte __ValidatePrimaryConstructorArguments(ArchLucid.Persistence.Data.Repositories.IGovernanceApprovalRequestRepository approvalRepo, ArchLucid.Persistence.Data.Repositories.IGovernancePromotionRecordRepository promotionRepo, ArchLucid.Application.IRunDetailQueryService runDetailQuery, ArchLucid.Persistence.Queries.IAuthorityQueryService authorityQuery, ArchLucid.Core.Scoping.IScopeContextProvider scopeProvider)
    {
        ArgumentNullException.ThrowIfNull(approvalRepo);
        ArgumentNullException.ThrowIfNull(promotionRepo);
        ArgumentNullException.ThrowIfNull(runDetailQuery);
        ArgumentNullException.ThrowIfNull(authorityQuery);
        ArgumentNullException.ThrowIfNull(scopeProvider);
        return (byte)0;
    }

    private readonly IGovernanceApprovalRequestRepository _approvalRepo = approvalRepo ?? throw new ArgumentNullException(nameof(approvalRepo));
    private readonly IAuthorityQueryService _authorityQuery = authorityQuery ?? throw new ArgumentNullException(nameof(authorityQuery));
    private readonly IGovernancePromotionRecordRepository _promotionRepo = promotionRepo ?? throw new ArgumentNullException(nameof(promotionRepo));
    private readonly IRunDetailQueryService _runDetailQuery = runDetailQuery ?? throw new ArgumentNullException(nameof(runDetailQuery));
    private readonly IScopeContextProvider _scopeProvider = scopeProvider ?? throw new ArgumentNullException(nameof(scopeProvider));
    /// <inheritdoc/>
    public async System.Threading.Tasks.Task<ArchLucid.Contracts.Governance.GovernanceLineageResult?> GetApprovalRequestLineageAsync(string approvalRequestId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(approvalRequestId);
        GovernanceApprovalRequest? approval = await _approvalRepo.GetByIdAsync(approvalRequestId, cancellationToken);
        if (approval is null)
            return null;
        ArchitectureRunDetail? coordinatorDetail = await _runDetailQuery.GetRunDetailAsync(approval.RunId, cancellationToken);
        GovernanceLineageRunSummary? runSummary = coordinatorDetail is null ? null : new GovernanceLineageRunSummary
        {
            RunId = coordinatorDetail.Run.RunId,
            Status = coordinatorDetail.Run.Status.ToString(),
            CreatedUtc = coordinatorDetail.Run.CreatedUtc,
            CompletedUtc = coordinatorDetail.Run.CompletedUtc,
            CurrentManifestVersion = coordinatorDetail.Run.CurrentManifestVersion
        };
        IReadOnlyList<GovernancePromotionRecord> promotions = await _promotionRepo.GetByRunIdAsync(approval.RunId, cancellationToken);
        GovernanceLineageManifestSummary? manifestSummary = null;
        List<GovernanceLineageFindingSummary> topFindings = [];
        string? riskPosture = null;
        if (!Guid.TryParseExact(approval.RunId, "N", out Guid authorityRunId))
            return new GovernanceLineageResult
            {
                ApprovalRequest = approval,
                Run = runSummary,
                Manifest = manifestSummary,
                TopFindings = topFindings,
                RiskPosture = riskPosture,
                Promotions = promotions.ToList()
            };
        ScopeContext scope = _scopeProvider.GetCurrentScope();
        RunDetailDto? authorityDetail = await _authorityQuery.GetRunDetailAsync(scope, authorityRunId, cancellationToken);
        if (authorityDetail?.GoldenManifest is not null)
        {
            ManifestDocument gm = authorityDetail.GoldenManifest;
            manifestSummary = new GovernanceLineageManifestSummary
            {
                ManifestVersion = gm.Metadata.Version,
                DecisionCount = gm.Decisions.Count,
                UnresolvedIssueCount = gm.UnresolvedIssues.Items.Count,
                ComplianceGapCount = gm.Compliance.Gaps.Count
            };
            riskPosture = AuthorityManifestRiskPosture.Derive(gm);
        }

        if (authorityDetail?.FindingsSnapshot?.Findings is not { Count: > 0 } findings)
            return new GovernanceLineageResult
            {
                ApprovalRequest = approval,
                Run = runSummary,
                Manifest = manifestSummary,
                TopFindings = topFindings,
                RiskPosture = riskPosture,
                Promotions = promotions.ToList()
            };
        IEnumerable<Finding> ordered = findings.OrderByDescending(f => (int)f.Severity).ThenBy(f => f.Title, StringComparer.OrdinalIgnoreCase);
        topFindings.AddRange(
            from f in ordered.Take(10)let score = ExplainabilityTraceCompletenessAnalyzer.AnalyzeFinding(f)select new GovernanceLineageFindingSummary { FindingId = f.FindingId, Title = f.Title, EngineType = f.EngineType, Severity = f.Severity.ToString(), TraceCompletenessRatio = score.CompletenessRatio, SourceAgentExecutionTraceId = f.Trace?.SourceAgentExecutionTraceId });
        return new GovernanceLineageResult
        {
            ApprovalRequest = approval,
            Run = runSummary,
            Manifest = manifestSummary,
            TopFindings = topFindings,
            RiskPosture = riskPosture,
            Promotions = promotions.ToList()
        };
    }
}