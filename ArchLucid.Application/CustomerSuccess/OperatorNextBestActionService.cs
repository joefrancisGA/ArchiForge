using ArchLucid.Core.CustomerSuccess;
using ArchLucid.Core.Scoping;

namespace ArchLucid.Application.CustomerSuccess;

public sealed record OperatorNextBestActionItem(
    string ActionId,
    string Title,
    string Reason,
    string Href,
    int SortOrder);

public interface IOperatorNextBestActionService
{
    Task<IReadOnlyList<OperatorNextBestActionItem>> GetActionsAsync(CancellationToken cancellationToken);
}

public sealed class OperatorNextBestActionService(
    IScopeContextProvider scopeProvider,
    IOperatorStickinessSnapshotReader snapshotReader) : IOperatorNextBestActionService
{
    private readonly IScopeContextProvider _scopeProvider =
        scopeProvider ?? throw new ArgumentNullException(nameof(scopeProvider));

    private readonly IOperatorStickinessSnapshotReader _snapshotReader =
        snapshotReader ?? throw new ArgumentNullException(nameof(snapshotReader));

    public async Task<IReadOnlyList<OperatorNextBestActionItem>> GetActionsAsync(CancellationToken cancellationToken)
    {
        ScopeContext scope = _scopeProvider.GetCurrentScope();
        OperatorStickinessSignals signals = await _snapshotReader
            .GetOperatorSignalsAsync(scope.TenantId, scope.WorkspaceId, scope.ProjectId, cancellationToken)
            .ConfigureAwait(false);

        List<OperatorNextBestActionItem> items = [];

        if (signals.TotalRunsInScope == 0)
        {
            items.Add(
                new OperatorNextBestActionItem(
                    "first_run",
                    "Create your first architecture review",
                    "No runs exist yet for this workspace project.",
                    "/runs/new",
                    10));
        }

        if (signals.TotalRunsInScope > 0 && signals.CommittedRunsInScope == 0)
        {
            string href = signals.LatestRunId is { } id ? $"/reviews/{id:D}" : "/runs";
            items.Add(
                new OperatorNextBestActionItem(
                    "finalize",
                    "Finalize a committed manifest",
                    "You have runs that are not finalized yet — commit the latest review output.",
                    href,
                    20));
        }

        if (signals.CommittedRunsInScope > 0 && signals.LatestRunId is { } runId)
        {
            items.Add(
                new OperatorNextBestActionItem(
                    "review_findings",
                    "Review findings on the latest run",
                    "Triage findings and capture pilot feedback while context is fresh.",
                    $"/reviews/{runId:D}",
                    30));

            items.Add(
                new OperatorNextBestActionItem(
                    "sponsor_pack",
                    "Generate the pilot scorecard package",
                    "Give your sponsor a sponsor one-pager and evidence bundle from a finalized run.",
                    $"/reviews/{runId:D}",
                    35));
        }

        if (signals.ComparisonAuditEvents30d == 0 && signals.CommittedRunsInScope >= 2)
        {
            items.Add(
                new OperatorNextBestActionItem(
                    "compare",
                    "Compare two finalized runs",
                    "You have multiple committed runs — diff manifests to show progress.",
                    "/compare",
                    40));
        }

        if (signals.PendingGovernanceApprovals > 0)
        {
            items.Add(
                new OperatorNextBestActionItem(
                    "governance",
                    "Resolve governance approvals",
                    $"You have {signals.PendingGovernanceApprovals} pending approval(s) blocking promotion.",
                    "/governance",
                    25));
        }

        items.Add(
            new OperatorNextBestActionItem(
                "scorecard",
                "Open the in-product pilot scorecard",
                "Track cumulative tenant proof and ROI baselines in one place.",
                "/scorecard",
                90));

        return items.OrderBy(static x => x.SortOrder).Take(5).ToList();
    }
}
