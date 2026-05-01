namespace ArchLucid.Contracts.Pilots;

/// <summary>
///     One row in <see cref="RecentPilotRunDeltasResponse" /> — a single committed run's headline numbers
///     (no evidence chain, no audit detail) sized for sidebar / index-page rendering.
/// </summary>
/// <remarks>
///     Sourced server-side from <c>PilotRunDeltas</c> (same numbers as <c>GET /v1/pilots/runs/{runId}/pilot-run-deltas</c>);
///     the slim shape avoids paying for the per-finding evidence chain when the caller only needs the headline.
/// </remarks>
public sealed class RecentPilotRunDeltaSummaryResponse
{
    /// <summary>Run identifier (32-char hex, no dashes — same shape as <c>RunSummary.RunId</c>).</summary>
    public string RunId
    {
        get;
        init;
    } = string.Empty;

    /// <summary>Architecture-request identifier this run satisfies (used by inline variant to find a prior run for the same request).</summary>
    public string RequestId
    {
        get;
        init;
    } = string.Empty;

    /// <summary>UTC timestamp the run was created (start of the elapsed-time window).</summary>
    public DateTime RunCreatedUtc
    {
        get;
        init;
    }

    /// <summary>UTC timestamp the golden manifest was committed; <see langword="null" /> for runs that did not commit.</summary>
    public DateTime? ManifestCommittedUtc
    {
        get;
        init;
    }

    /// <summary>Wall-clock seconds from <see cref="RunCreatedUtc" /> to <see cref="ManifestCommittedUtc" />, or <see langword="null" /> when uncommitted.</summary>
    public double? TimeToCommittedManifestTotalSeconds
    {
        get;
        init;
    }

    /// <summary>Total findings across all severities (sum of <c>FindingsBySeverity</c> counts).</summary>
    public int TotalFindings
    {
        get;
        init;
    }

    /// <summary>Severity of the highest-ranked finding on this run (e.g. "Critical", "High"); <see langword="null" /> when no findings.</summary>
    public string? TopFindingSeverity
    {
        get;
        init;
    }

    /// <summary>Run is a Contoso Retail demo seed; UI MUST surface a "demo tenant" banner so seeded numbers are not quoted as real outcomes.</summary>
    public bool IsDemoTenant
    {
        get;
        init;
    }
}
