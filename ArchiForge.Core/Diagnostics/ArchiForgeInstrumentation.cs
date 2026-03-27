using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace ArchiForge.Core.Diagnostics;

/// <summary>
/// Shared <see cref="ActivitySource"/> and <see cref="Meter"/> names for cross-cutting observability (OTel wiring in the API host).
/// </summary>
public static class ArchiForgeInstrumentation
{
    /// <summary>Meter name registered with OpenTelemetry in <c>AddArchiForgeOpenTelemetry</c>.</summary>
    public const string MeterName = "ArchiForge";

    private static readonly Meter AppMeter = new(MeterName, "1.0.0");

    /// <summary>Scheduled advisory scan pipeline (<c>AdvisoryScanRunner</c>).</summary>
    public static readonly ActivitySource AdvisoryScan = new("ArchiForge.AdvisoryScan", "1.0.0");

    /// <summary>Authority run orchestration (ingestion → manifest).</summary>
    public static readonly ActivitySource AuthorityRun = new("ArchiForge.AuthorityRun", "1.0.0");

    /// <summary>Post-commit retrieval indexing of committed runs.</summary>
    public static readonly ActivitySource RetrievalIndex = new("ArchiForge.Retrieval.Index", "1.0.0");

    /// <summary>Digest channel send succeeded (labels: <c>channel</c>).</summary>
    public static readonly Counter<long> DigestDeliverySucceeded = AppMeter.CreateCounter<long>("digest_delivery_succeeded");

    /// <summary>Digest channel send failed after non-cancellation error (labels: <c>channel</c>).</summary>
    public static readonly Counter<long> DigestDeliveryFailed = AppMeter.CreateCounter<long>("digest_delivery_failed");

    /// <summary>
    /// Wall time for <c>EvaluateAndPersistAsync</c> (labels: <c>rule_kind</c> = <c>simple</c> | <c>composite</c>).
    /// </summary>
    public static readonly Histogram<double> AlertEvaluationDurationMilliseconds = AppMeter.CreateHistogram<double>(
        "alert_evaluation_duration_ms",
        unit: "ms",
        description: "Time spent in alert EvaluateAndPersistAsync per rule kind.");

    /// <summary>Wall time for effective governance resolution (<c>IEffectiveGovernanceResolver.ResolveAsync</c>).</summary>
    public static readonly Histogram<double> GovernanceResolveDurationMilliseconds = AppMeter.CreateHistogram<double>(
        "governance_resolve_duration_ms",
        unit: "ms",
        description: "Time to resolve effective governance for a tenant/workspace/project scope.");

    /// <summary>
    /// Hits on the in-resolve <c>(packId, version)</c> deserialized content cache inside <c>EffectiveGovernanceResolver</c>
    /// (avoids duplicate JSON work when the same version appears on multiple assignments).
    /// </summary>
    public static readonly Counter<long> GovernancePackContentDeserializeCacheHits =
        AppMeter.CreateCounter<long>("governance_pack_content_deserialize_cache_hits");

    /// <summary>Misses on that cache (JSON deserialize executed for a distinct pack version in the resolve call).</summary>
    public static readonly Counter<long> GovernancePackContentDeserializeCacheMisses =
        AppMeter.CreateCounter<long>("governance_pack_content_deserialize_cache_misses");
}
