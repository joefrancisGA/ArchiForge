using ArchLucid.Contracts.Governance;

namespace ArchLucid.Application.Governance;

/// <inheritdoc cref="IGovernanceRationaleService" />
public sealed class GovernanceRationaleService(IGovernanceLineageService lineageService) : IGovernanceRationaleService
{
    private readonly IGovernanceLineageService _lineageService =
        lineageService ?? throw new ArgumentNullException(nameof(lineageService));

    /// <inheritdoc />
    public async Task<GovernanceRationaleResult?> GetApprovalRequestRationaleAsync(
        string approvalRequestId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(approvalRequestId);

        GovernanceLineageResult? lineage = await _lineageService
                .GetApprovalRequestLineageAsync(approvalRequestId, cancellationToken)
            ;

        if (lineage is null)
            return null;

        GovernanceApprovalRequest approval = lineage.ApprovalRequest;
        List<string> bullets =
        [
            $"Environment path: {approval.SourceEnvironment} → {approval.TargetEnvironment}",
            $"Manifest version: {approval.ManifestVersion}",
            $"Workflow status: {approval.Status}",
            $"Run id: {approval.RunId}"
        ];

        if (lineage.Run is { } run)

            bullets.Add($"Run status: {run.Status}; created {run.CreatedUtc:O} UTC");

        if (lineage.Manifest is { } manifest)

            bullets.Add(
                $"Authority manifest: version {manifest.ManifestVersion ?? "—"}; " +
                $"{manifest.DecisionCount} decision(s); {manifest.UnresolvedIssueCount} open issue(s); " +
                $"{manifest.ComplianceGapCount} compliance gap(s).");

        if (!string.IsNullOrWhiteSpace(lineage.RiskPosture))

            bullets.Add($"Derived risk posture: {lineage.RiskPosture}");

        if (lineage.TopFindings.Count > 0)

            bullets.Add(
                $"Top findings (sample): {string.Join("; ", lineage.TopFindings.Take(3).Select(f => $"{f.Severity} {f.Title}"))}");

        if (lineage.Promotions.Count > 0)

            bullets.Add($"Prior promotions recorded for this run: {lineage.Promotions.Count}.");

        string summary =
            $"Governance rationale for approval {approval.ApprovalRequestId}: " +
            $"promotion request from {approval.SourceEnvironment} to {approval.TargetEnvironment} " +
            $"for run {approval.RunId} at manifest {approval.ManifestVersion}.";

        return new GovernanceRationaleResult
        {
            SchemaVersion = 1, ApprovalRequestId = approval.ApprovalRequestId, Summary = summary, Bullets = bullets
        };
    }
}
