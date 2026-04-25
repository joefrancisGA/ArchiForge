namespace ArchLucid.Contracts.Pilots;

/// <summary>
///     Aggregated proof-of-ROI deltas for the most recent N committed runs in scope
///     (<c>GET /v1/pilots/runs/recent-deltas</c>). Powers the <c>BeforeAfterDeltaPanel</c>
///     "top" and "sidebar" variants on the operator shell.
/// </summary>
/// <remarks>
///     Aggregates are computed server-side so the UI does not have to fan out one HTTP call per run.
///     Median (not mean) is used for both aggregates so a single noisy outlier (e.g. a long debugging run)
///     does not skew the headline. <see cref="MedianTotalFindings" /> and
///     <see cref="MedianTimeToCommittedManifestTotalSeconds" /> are <see langword="null" /> when
///     <see cref="Items" /> is empty so the UI can render an empty state instead of a misleading "0".
/// </remarks>
public sealed class RecentPilotRunDeltasResponse
{
    /// <summary>The committed runs included in the aggregate (newest first), each pre-projected for slim transport.</summary>
    public IReadOnlyList<RecentPilotRunDeltaSummaryResponse> Items
    {
        get; init;
    } = [];

    /// <summary>Number requested by the caller via <c>?count=</c> (clamped to the service's hard upper bound before this query ran).</summary>
    public int RequestedCount
    {
        get; init;
    }

    /// <summary>How many committed runs were actually found in scope and used for the aggregates (≤ <see cref="RequestedCount" />).</summary>
    public int ReturnedCount
    {
        get; init;
    }

    /// <summary>Median of <c>TotalFindings</c> across <see cref="Items" />; <see langword="null" /> when the list is empty.</summary>
    public double? MedianTotalFindings
    {
        get; init;
    }

    /// <summary>
    ///     Median of <c>TimeToCommittedManifestTotalSeconds</c> across <see cref="Items" /> (the value-report calls
    ///     this <em>time to first finding-quality manifest</em> — same number, sponsor-friendly framing);
    ///     <see langword="null" /> when no committed runs are in the window.
    /// </summary>
    public double? MedianTimeToCommittedManifestTotalSeconds
    {
        get; init;
    }
}
