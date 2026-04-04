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

    /// <summary>One span per production agent handler invocation (<c>RealAgentExecutor</c>).</summary>
    public static readonly ActivitySource AgentHandler = new("ArchiForge.Agent.Handler", "1.0.0");

    /// <summary>Azure OpenAI chat completion calls (nested under agent handler when a trace is active).</summary>
    public static readonly ActivitySource AgentLlmCompletion = new("ArchiForge.Agent.LlmCompletion", "1.0.0");

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

    /// <summary>
    /// Incremented each time a persistence read falls back to a JSON column because
    /// relational child rows are absent. Tags: <c>entity_type</c>, <c>slice</c>, <c>read_mode</c>.
    /// </summary>
    public static readonly Counter<long> JsonFallbackUsed =
        AppMeter.CreateCounter<long>(
            "persistence_json_fallback_used",
            description: "Count of persistence reads that fell back to JSON columns.");

    /// <summary>Authority runs that finished the synchronous pipeline successfully (post-commit).</summary>
    public static readonly Counter<long> AuthorityRunsCompletedTotal =
        AppMeter.CreateCounter<long>(
            "archiforge_authority_runs_completed_total",
            description: "Authority runs completed through FinalizeCommittedPipelineAsync.");

    /// <summary>Production agent handler completions (label: <c>agent_type_key</c>, <c>outcome</c>=success|error).</summary>
    public static readonly Counter<long> AgentHandlerInvocationsTotal =
        AppMeter.CreateCounter<long>(
            "archiforge_agent_handler_invocations_total",
            description: "Agent handler invocations by type and outcome.");

    /// <summary>Azure OpenAI chat completion prompt (input) tokens.</summary>
    public static readonly Counter<long> LlmPromptTokensTotal =
        AppMeter.CreateCounter<long>(
            "archiforge_llm_prompt_tokens_total",
            description: "Cumulative prompt tokens reported by Azure OpenAI completions.");

    /// <summary>Azure OpenAI chat completion output tokens.</summary>
    public static readonly Counter<long> LlmCompletionTokensTotal =
        AppMeter.CreateCounter<long>(
            "archiforge_llm_completion_tokens_total",
            description: "Cumulative completion tokens reported by Azure OpenAI completions.");
}
