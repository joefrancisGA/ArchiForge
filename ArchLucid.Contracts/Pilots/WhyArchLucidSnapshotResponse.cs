namespace ArchLucid.Contracts.Pilots;

/// <summary>
/// Server-rendered telemetry snapshot consumed by the operator-shell <c>/why-archlucid</c> proof page.
/// All counts are cumulative since the API host process started; the page surfaces them as a live
/// "look behind the curtain" for the seeded Contoso Retail demo tenant.
/// </summary>
public sealed class WhyArchLucidSnapshotResponse
{
    /// <summary>UTC timestamp the snapshot was produced.</summary>
    public DateTimeOffset GeneratedUtc { get; set; }

    /// <summary>
    /// Canonical run id (<c>ContosoRetailDemoIdentifiers.RunBaseline</c>) used by the page to call
    /// <c>GET /v1/pilots/runs/{runId}/first-value-report</c> and <c>GET /v1/explain/runs/{runId}/aggregate</c>.
    /// </summary>
    public string DemoRunId { get; set; } = string.Empty;

    /// <summary>Cumulative <c>archlucid_runs_created_total</c> since process start.</summary>
    public long RunsCreatedTotal { get; set; }

    /// <summary>
    /// Cumulative <c>archlucid_findings_produced_total</c>, grouped by the <c>severity</c> tag.
    /// Severity is whatever upstream emitters report (commonly <c>Critical|High|Medium|Low|Info|Unknown</c>).
    /// </summary>
    public IReadOnlyDictionary<string, long> FindingsProducedBySeverity { get; set; }
        = new Dictionary<string, long>(StringComparer.Ordinal);

    /// <summary>
    /// Audit row count returned by <c>IAuditRepository</c> for the default demo scope
    /// (<c>ScopeIds.DefaultTenant</c>/<c>DefaultWorkspace</c>/<c>DefaultProject</c>).
    /// Capped at <see cref="AuditRowCountCap"/>; <see cref="AuditRowCountTruncated"/> indicates the cap was hit.
    /// </summary>
    public int AuditRowCount { get; set; }

    /// <summary><c>true</c> when <see cref="AuditRowCount"/> was clamped to <see cref="AuditRowCountCap"/>.</summary>
    public bool AuditRowCountTruncated { get; set; }

    /// <summary>Maximum value <see cref="AuditRowCount"/> can take in a single response.</summary>
    public const int AuditRowCountCap = 500;
}
