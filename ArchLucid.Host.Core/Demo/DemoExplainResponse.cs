using ArchLucid.Core.Explanation;
using ArchLucid.Provenance;

namespace ArchLucid.Host.Core.Demo;

/// <summary>
/// Side-by-side payload rendered by the operator-shell <c>/demo/explain</c> proof route:
/// the citations-bound aggregate explanation and the full provenance graph for the same
/// committed demo-seed run, both fetched server-side under the demo tenant.
/// </summary>
/// <remarks>
/// The route only ships when <c>Demo:Enabled=true</c>; the response always carries
/// <see cref="IsDemoData"/>=<see langword="true"/> so consumers (and screenshots) cannot
/// quote the numbers as production telemetry.
/// </remarks>
public sealed class DemoExplainResponse
{
    /// <summary>UTC the response was assembled.</summary>
    public DateTimeOffset GeneratedUtc
    {
        get;
        init;
    }

    /// <summary>Run id (no-dash GUID, lowercase) of the latest committed demo-seed run.</summary>
    public string RunId
    {
        get;
        init;
    } = string.Empty;

    /// <summary>Committed manifest version key for <see cref="RunId"/>, when present.</summary>
    public string? ManifestVersion
    {
        get;
        init;
    }

    /// <summary>Always <see langword="true"/>: the route is gated on the demo seed and never returns production data.</summary>
    public bool IsDemoData
    {
        get;
        init;
    } = true;

    /// <summary>
    /// Operator-facing one-liner ("demo tenant — replace before publishing" semantics)
    /// for the page banner; mirrors the wording used by the sponsor-facing reports.
    /// </summary>
    public string DemoStatusMessage
    {
        get;
        init;
    } = "demo tenant — replace before publishing";

    /// <summary>Executive aggregate explanation + citations for the run (same payload as <c>/v1/explain/runs/{runId}/aggregate</c>).</summary>
    public required RunExplanationSummary RunExplanation
    {
        get;
        init;
    }

    /// <summary>Full UI-shaped provenance graph for the run (same payload as <c>/v1/provenance/runs/{runId}/graph</c>).</summary>
    public required GraphViewModel ProvenanceGraph
    {
        get;
        init;
    }
}
