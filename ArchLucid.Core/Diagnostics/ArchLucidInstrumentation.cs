using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace ArchLucid.Core.Diagnostics;

/// <summary>
/// Shared <see cref="ActivitySource"/> and <see cref="Meter"/> names for cross-cutting observability (OTel wiring in the API host).
/// </summary>
public static class ArchLucidInstrumentation
{
    /// <summary>Meter name registered with OpenTelemetry in <c>AddArchLucidOpenTelemetry</c>.</summary>
    public const string MeterName = "ArchLucid";

    private static readonly Meter AppMeter = new(MeterName, "1.0.0");

    private static int _outboxObservableGaugesRegistered;

    /// <summary>Latest outbox depths for <see cref="EnsureOutboxDepthObservableGaugesRegistered"/>.</summary>
    public static OutboxDepthGaugeState OutboxDepthGauges { get; } = new();

    /// <summary>Registers observable gauges once (call from OpenTelemetry host setup).</summary>
    public static void EnsureOutboxDepthObservableGaugesRegistered()
    {
        if (Interlocked.Exchange(ref _outboxObservableGaugesRegistered, 1) != 0)
        {
            return;
        }

        OutboxDepthGaugeState s = OutboxDepthGauges;

        AppMeter.CreateObservableGauge(
            "archiforge_authority_pipeline_work_pending",
            () => new Measurement<long>(s.Current.AuthorityPipelineWorkPending),
            description: "Rows in dbo.AuthorityPipelineWorkOutbox awaiting processing.");

        AppMeter.CreateObservableGauge(
            "archiforge_authority_pipeline_work_oldest_pending_age_seconds",
            () => new Measurement<double>(s.Current.AuthorityPipelineWorkOldestPendingAgeSeconds),
            unit: "s",
            description: "Age in seconds of the oldest pending authority pipeline work outbox row.");

        AppMeter.CreateObservableGauge(
            "archiforge_retrieval_indexing_outbox_pending",
            () => new Measurement<long>(s.Current.RetrievalIndexingOutboxPending),
            description: "Rows in dbo.RetrievalIndexingOutbox awaiting indexing.");

        AppMeter.CreateObservableGauge(
            "archiforge_retrieval_indexing_outbox_oldest_pending_age_seconds",
            () => new Measurement<double>(s.Current.RetrievalIndexingOutboxOldestPendingAgeSeconds),
            unit: "s",
            description: "Age in seconds of the oldest pending retrieval indexing outbox row.");

        AppMeter.CreateObservableGauge(
            "archiforge_integration_event_outbox_publish_pending",
            () => new Measurement<long>(s.Current.IntegrationEventOutboxPublishPending),
            description: "Integration outbox rows eligible for Service Bus publish (excludes dead letters).");

        AppMeter.CreateObservableGauge(
            "archiforge_integration_event_outbox_dead_letter",
            () => new Measurement<long>(s.Current.IntegrationEventOutboxDeadLetter),
            description: "Integration outbox rows in dead-letter state.");

        AppMeter.CreateObservableGauge(
            "archiforge_integration_event_outbox_oldest_actionable_pending_age_seconds",
            () => new Measurement<double>(s.Current.IntegrationEventOutboxOldestActionablePendingAgeSeconds),
            unit: "s",
            description: "Age in seconds of the oldest actionable integration outbox publish row.");
    }

    /// <summary>Scheduled advisory scan pipeline (<c>AdvisoryScanRunner</c>).</summary>
    public static readonly ActivitySource AdvisoryScan = new("ArchLucid.AdvisoryScan", "1.0.0");

    /// <summary>Authority run orchestration (ingestion → manifest).</summary>
    public static readonly ActivitySource AuthorityRun = new("ArchLucid.AuthorityRun", "1.0.0");

    /// <summary>Post-commit retrieval indexing of committed runs.</summary>
    public static readonly ActivitySource RetrievalIndex = new("ArchLucid.Retrieval.Index", "1.0.0");

    /// <summary>One span per production agent handler invocation (<c>RealAgentExecutor</c>).</summary>
    public static readonly ActivitySource AgentHandler = new("ArchLucid.Agent.Handler", "1.0.0");

    /// <summary>Azure OpenAI chat completion calls (nested under agent handler when a trace is active).</summary>
    public static readonly ActivitySource AgentLlmCompletion = new("ArchLucid.Agent.LlmCompletion", "1.0.0");

    /// <summary>Retrieval indexing outbox batch processor (<c>RetrievalIndexingOutboxProcessor</c>).</summary>
    public static readonly ActivitySource RetrievalIndexingOutbox = new("ArchLucid.RetrievalIndexing.Outbox", "1.0.0");

    /// <summary>Integration event Service Bus publish outbox (<c>IntegrationEventOutboxProcessor</c>).</summary>
    public static readonly ActivitySource IntegrationEventOutbox = new("ArchLucid.IntegrationEvent.Outbox", "1.0.0");

    /// <summary>Scheduled data retention archival (<c>DataArchivalCoordinator</c>).</summary>
    public static readonly ActivitySource DataArchival = new("ArchLucid.DataArchival", "1.0.0");

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
    /// Per advisory scan: fraction of explainability trace fields populated across findings (0.0–1.0; label <c>scan_type</c>).
    /// </summary>
    public static readonly Histogram<double> ExplainabilityTraceCompleteness = AppMeter.CreateHistogram<double>(
        "archlucid_explainability_trace_completeness_ratio",
        description: "Per-scan trace completeness ratio (0.0–1.0).");

    /// <summary>Circuit breaker state changes (labels: <c>gate</c>, <c>from_state</c>, <c>to_state</c>).</summary>
    public static readonly Counter<long> CircuitBreakerStateTransitions =
        AppMeter.CreateCounter<long>(
            "archiforge_circuit_breaker_state_transitions_total",
            description: "Circuit breaker state transitions (labels: gate, from_state, to_state).");

    /// <summary>Calls rejected while open or while a half-open probe is in flight (label: <c>gate</c>).</summary>
    public static readonly Counter<long> CircuitBreakerRejections =
        AppMeter.CreateCounter<long>(
            "archiforge_circuit_breaker_rejections_total",
            description: "Calls rejected because the circuit was open or a probe was in flight (label: gate).");

    /// <summary>Half-open probe results (labels: <c>gate</c>, <c>outcome</c>=success|failure|cancelled).</summary>
    public static readonly Counter<long> CircuitBreakerProbeOutcomes =
        AppMeter.CreateCounter<long>(
            "archiforge_circuit_breaker_probe_outcomes_total",
            description: "Half-open probe results (labels: gate, outcome=success|failure|cancelled).");

    /// <summary>
    /// LLM call retry attempts before the circuit breaker records a failure (labels: <c>gate</c>, <c>attempt</c>, <c>exception_type</c>).
    /// </summary>
    public static readonly Counter<long> LlmCallRetries =
        AppMeter.CreateCounter<long>(
            "archlucid_llm_call_retries_total",
            description: "LLM call retry attempts before circuit breaker recording (labels: gate, attempt, exception_type).");

    /// <summary>
    /// Hits on the in-resolve <c>(packId, version)</c> deserialized content cache inside <c>EffectiveGovernanceResolver</c>
    /// (avoids duplicate JSON work when the same version appears on multiple assignments).
    /// </summary>
    public static readonly Counter<long> GovernancePackContentDeserializeCacheHits =
        AppMeter.CreateCounter<long>("governance_pack_content_deserialize_cache_hits");

    /// <summary>Misses on that cache (JSON deserialize executed for a distinct pack version in the resolve call).</summary>
    public static readonly Counter<long> GovernancePackContentDeserializeCacheMisses =
        AppMeter.CreateCounter<long>("governance_pack_content_deserialize_cache_misses");

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

    /// <summary>
    /// Records LLM token counters. When <paramref name="recordPerTenant"/> is true, also emits tagged series with
    /// <c>tenant_id</c> (increases Prometheus cardinality — use only for bounded tenant counts).
    /// Optional <paramref name="llmProviderId"/> and <paramref name="llmDeploymentLabel"/> add low-cardinality series for FinOps dashboards.
    /// </summary>
    public static void RecordLlmTokenUsage(
        long promptTokens,
        long completionTokens,
        bool recordPerTenant,
        string? tenantIdNormalized,
        string? llmProviderId = null,
        string? llmDeploymentLabel = null)
    {
        bool hasTags = (recordPerTenant && !string.IsNullOrEmpty(tenantIdNormalized))
                       || !string.IsNullOrEmpty(llmProviderId)
                       || !string.IsNullOrEmpty(llmDeploymentLabel);

        TagList BuildTags()
        {
            TagList tags = new TagList();

            if (recordPerTenant && !string.IsNullOrEmpty(tenantIdNormalized))
            {
                tags.Add("tenant_id", tenantIdNormalized);
            }

            if (!string.IsNullOrEmpty(llmProviderId))
            {
                tags.Add("llm_provider", llmProviderId);
            }

            if (!string.IsNullOrEmpty(llmDeploymentLabel))
            {
                tags.Add("llm_deployment", llmDeploymentLabel);
            }

            return tags;
        }

        if (promptTokens > 0)
        {
            if (hasTags)
            {
                LlmPromptTokensTotal.Add(promptTokens, BuildTags());
            }
            else
            {
                LlmPromptTokensTotal.Add(promptTokens);
            }
        }

        if (completionTokens > 0)
        {
            if (hasTags)
            {
                LlmCompletionTokensTotal.Add(completionTokens, BuildTags());
            }
            else
            {
                LlmCompletionTokensTotal.Add(completionTokens);
            }
        }
    }
}
