using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace ArchLucid.Core.Diagnostics;

/// <summary>Thread-safe completion count for one <c>RealAgentExecutor.ExecuteAsync</c> batch (parallel handlers share one instance).</summary>
public sealed class AgentExecutionLlmCallAccumulator
{
    private int _count;

    /// <summary>Adds <paramref name="delta"/> successful remote completions (ignored if non-positive).</summary>
    public void AddCompletions(int delta)
    {
        if (delta > 0)
        {
            _ = Interlocked.Add(ref _count, delta);
        }
    }

    /// <summary>Reads and resets the accumulated count.</summary>
    public int Consume()
    {
        return Interlocked.Exchange(ref _count, 0);
    }
}

/// <summary>
/// Shared <see cref="ActivitySource"/> and <see cref="Meter"/> names for cross-cutting observability (OTel wiring in the API host).
/// </summary>
public static class ArchLucidInstrumentation
{
    /// <summary>Meter name registered with OpenTelemetry in <c>AddArchLucidOpenTelemetry</c>.</summary>
    public const string MeterName = "ArchLucid";

    private static readonly Meter AppMeter = new(MeterName, "1.0.0");

    private static readonly AsyncLocal<AgentExecutionLlmCallAccumulator?> LlmCallsPerRunAccumulator = new();

    private static int _outboxObservableGaugesRegistered;

    private static Func<long>? _auditRetryQueuePendingReader;

    /// <summary>Latest outbox depths for <see cref="EnsureOutboxDepthObservableGaugesRegistered"/>.</summary>
    public static OutboxDepthGaugeState OutboxDepthGauges { get; } = new();

    /// <summary>
    /// Supplies pending audit-retry depth for <c>archlucid_audit_retry_queue_pending</c> (last writer wins; use a singleton queue).
    /// </summary>
    public static void SetAuditRetryQueuePendingReader(Func<long>? reader) =>
        Volatile.Write(ref _auditRetryQueuePendingReader, reader);

    /// <summary>Registers observable gauges once (call from OpenTelemetry host setup).</summary>
    public static void EnsureOutboxDepthObservableGaugesRegistered()
    {
        if (Interlocked.Exchange(ref _outboxObservableGaugesRegistered, 1) != 0)
        {
            return;
        }

        OutboxDepthGaugeState s = OutboxDepthGauges;

        AppMeter.CreateObservableGauge(
            "archlucid_authority_pipeline_work_pending",
            () => new Measurement<long>(s.Current.AuthorityPipelineWorkPending),
            description: "Rows in dbo.AuthorityPipelineWorkOutbox awaiting processing.");

        AppMeter.CreateObservableGauge(
            "archlucid_authority_pipeline_work_oldest_pending_age_seconds",
            () => new Measurement<double>(s.Current.AuthorityPipelineWorkOldestPendingAgeSeconds),
            unit: "s",
            description: "Age in seconds of the oldest pending authority pipeline work outbox row.");

        AppMeter.CreateObservableGauge(
            "archlucid_retrieval_indexing_outbox_pending",
            () => new Measurement<long>(s.Current.RetrievalIndexingOutboxPending),
            description: "Rows in dbo.RetrievalIndexingOutbox awaiting indexing.");

        AppMeter.CreateObservableGauge(
            "archlucid_retrieval_indexing_outbox_oldest_pending_age_seconds",
            () => new Measurement<double>(s.Current.RetrievalIndexingOutboxOldestPendingAgeSeconds),
            unit: "s",
            description: "Age in seconds of the oldest pending retrieval indexing outbox row.");

        AppMeter.CreateObservableGauge(
            "archlucid_integration_event_outbox_publish_pending",
            () => new Measurement<long>(s.Current.IntegrationEventOutboxPublishPending),
            description: "Integration outbox rows eligible for Service Bus publish (excludes dead letters).");

        AppMeter.CreateObservableGauge(
            "archlucid_integration_event_outbox_dead_letter",
            () => new Measurement<long>(s.Current.IntegrationEventOutboxDeadLetter),
            description: "Integration outbox rows in dead-letter state.");

        AppMeter.CreateObservableGauge(
            "archlucid_integration_event_outbox_oldest_actionable_pending_age_seconds",
            () => new Measurement<double>(s.Current.IntegrationEventOutboxOldestActionablePendingAgeSeconds),
            unit: "s",
            description: "Age in seconds of the oldest actionable integration outbox publish row.");

        AppMeter.CreateObservableGauge(
            "archlucid_audit_retry_queue_pending",
            () => new Measurement<long>(_auditRetryQueuePendingReader?.Invoke() ?? 0),
            description: "Approximate audit events waiting in memory for durable write after hot-path failure.");
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

    /// <summary>
    /// Heuristic overlap between aggregate explanation tokens and flattened finding <c>ExplainabilityTrace</c> text (0.0–1.0).
    /// </summary>
    public static readonly Histogram<double> ExplanationFaithfulnessRatio = AppMeter.CreateHistogram<double>(
        "archlucid_explanation_faithfulness_ratio",
        description: "Heuristic faithfulness of run explanation vs finding traces (0.0–1.0).");

    /// <summary>
    /// Per provenance response: fraction of manifest decisions with finding, rule, and graph-context edges (0.0–1.0).
    /// </summary>
    public static readonly Histogram<double> ProvenanceCompleteness = AppMeter.CreateHistogram<double>(
        "archlucid_provenance_completeness_ratio",
        description: "Decision provenance traceability completeness ratio (0.0–1.0).");

    /// <summary>Circuit breaker state changes (labels: <c>gate</c>, <c>from_state</c>, <c>to_state</c>).</summary>
    public static readonly Counter<long> CircuitBreakerStateTransitions =
        AppMeter.CreateCounter<long>(
            "archlucid_circuit_breaker_state_transitions_total",
            description: "Circuit breaker state transitions (labels: gate, from_state, to_state).");

    /// <summary>Calls rejected while open or while a half-open probe is in flight (label: <c>gate</c>).</summary>
    public static readonly Counter<long> CircuitBreakerRejections =
        AppMeter.CreateCounter<long>(
            "archlucid_circuit_breaker_rejections_total",
            description: "Calls rejected because the circuit was open or a probe was in flight (label: gate).");

    /// <summary>Half-open probe results (labels: <c>gate</c>, <c>outcome</c>=success|failure|cancelled).</summary>
    public static readonly Counter<long> CircuitBreakerProbeOutcomes =
        AppMeter.CreateCounter<long>(
            "archlucid_circuit_breaker_probe_outcomes_total",
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
            "archlucid_authority_runs_completed_total",
            description: "Authority runs completed through FinalizeCommittedPipelineAsync.");

    /// <summary>Authority runs created (pre-pipeline, at <c>RunRecord</c> insertion).</summary>
    public static readonly Counter<long> RunsCreatedTotal =
        AppMeter.CreateCounter<long>(
            "archlucid_runs_created_total",
            description: "Authority runs created (pre-pipeline, at RunRecord insertion).");

    /// <summary>Authority pipeline runs that exceeded <c>AuthorityPipeline:PipelineTimeout</c>.</summary>
    public static readonly Counter<long> PipelineTimeoutsTotal =
        AppMeter.CreateCounter<long>(
            "archlucid_authority_pipeline_timeouts_total",
            description: "Authority pipeline executions cancelled by configured pipeline timeout.");

    /// <summary>Findings produced across completed runs (label: <c>severity</c>).</summary>
    public static readonly Counter<long> FindingsProducedTotal =
        AppMeter.CreateCounter<long>(
            "archlucid_findings_produced_total",
            description: "Findings produced across all completed runs (label: severity).");

    /// <summary>Finding engines that threw during snapshot generation (labels: <c>engine_type</c>, <c>category</c>).</summary>
    public static readonly Counter<long> FindingEngineFailuresTotal =
        AppMeter.CreateCounter<long>(
            "archlucid_finding_engine_failures_total",
            description: "Finding engines that failed during findings snapshot generation (labels: engine_type, category).");

    /// <summary>LLM completion calls made during a single <c>RealAgentExecutor.ExecuteAsync</c> batch.</summary>
    public static readonly Histogram<int> LlmCallsPerRun =
        AppMeter.CreateHistogram<int>(
            "archlucid_llm_calls_per_run",
            unit: "{call}",
            description: "Number of LLM completion calls made during a single authority run.");

    /// <summary>Aggregate explanation cache hits (<c>CachingRunExplanationSummaryService</c>).</summary>
    public static readonly Counter<long> ExplanationCacheHits =
        AppMeter.CreateCounter<long>(
            "archlucid_explanation_cache_hits_total",
            description: "Aggregate explanation cache hits (via CachingRunExplanationSummaryService).");

    /// <summary>Aggregate explanation cache misses (factory invoked; LLM work may follow).</summary>
    public static readonly Counter<long> ExplanationCacheMisses =
        AppMeter.CreateCounter<long>(
            "archlucid_explanation_cache_misses_total",
            description: "Aggregate explanation cache misses (LLM call required).");

    /// <summary>Schema validation of raw <c>AgentResult</c> LLM JSON (labels: <c>agent_type</c>, <c>outcome</c>=valid|invalid).</summary>
    public static readonly Counter<long> AgentResultSchemaValidationsTotal =
        AppMeter.CreateCounter<long>(
            "archlucid_agent_result_schema_validations_total",
            description: "Schema validation of raw AgentResult LLM output (labels: agent_type, outcome).");

    /// <summary>Schema validation of explanation LLM JSON (labels: <c>explanation_type</c>, <c>outcome</c>=valid|invalid|skipped).</summary>
    public static readonly Counter<long> ExplanationSchemaValidationsTotal =
        AppMeter.CreateCounter<long>(
            "archlucid_explanation_schema_validations_total",
            description: "Schema validation of explanation LLM payloads (labels: explanation_type, outcome).");

    /// <summary>Per-stage wall time inside the authority pipeline (labels: <c>stage</c>, <c>outcome</c>=success|error).</summary>
    public static readonly Histogram<double> AuthorityPipelineStageDurationMilliseconds =
        AppMeter.CreateHistogram<double>(
            "archlucid_authority_pipeline_stage_duration_ms",
            unit: "ms",
            description: "Per-stage wall time inside the authority pipeline (labels: stage, outcome).");

    /// <summary>Production agent handler completions (label: <c>agent_type_key</c>, <c>outcome</c>=success|error).</summary>
    public static readonly Counter<long> AgentHandlerInvocationsTotal =
        AppMeter.CreateCounter<long>(
            "archlucid_agent_handler_invocations_total",
            description: "Agent handler invocations by type and outcome.");

    /// <summary>
    /// Fraction of expected <c>AgentResult</c> JSON keys present on <c>ParsedResultJson</c> (0.0–1.0; label <c>agent_type</c>).
    /// </summary>
    public static readonly Histogram<double> AgentOutputStructuralCompletenessRatio =
        AppMeter.CreateHistogram<double>(
            "archlucid_agent_output_structural_completeness_ratio",
            description: "Structural completeness of persisted agent parsed JSON (0.0–1.0).");

    /// <summary>Trace JSON that is not a JSON object or failed <see cref="System.Text.Json"/> parse (label <c>agent_type</c>).</summary>
    public static readonly Counter<long> AgentOutputParseFailuresTotal =
        AppMeter.CreateCounter<long>(
            "archlucid_agent_output_parse_failures_total",
            description: "Agent trace ParsedResultJson parse/root-kind failures.");

    /// <summary>Total failed agent trace blob uploads after all retries (labels: <c>agent_type</c>, <c>blob_type</c>).</summary>
    public static readonly Counter<long> AgentTraceBlobUploadFailuresTotal =
        AppMeter.CreateCounter<long>(
            "archlucid_agent_trace_blob_upload_failures_total",
            description: "Total failed agent trace blob uploads after all retries.");

    /// <summary>Wall-clock milliseconds to complete agent trace full-text blob persistence (label <c>agent_type</c>).</summary>
    public static readonly Histogram<double> AgentTraceBlobPersistDurationMs =
        AppMeter.CreateHistogram<double>(
            "archlucid_agent_trace_blob_persist_duration_ms",
            unit: "ms",
            description: "Duration in milliseconds for full prompt/response blob writes per trace.");

    /// <summary>
    /// Real-mode SQL inline fallback for full prompt/response when blob key is missing (labels: <c>agent_type</c>, <c>blob_type</c>=system_prompt|user_prompt|response).
    /// </summary>
    public static readonly Counter<long> AgentTracePromptInlineFallbacksTotal =
        AppMeter.CreateCounter<long>(
            "archlucid_agent_trace_prompt_inline_fallback_total",
            description: "Full-text agent trace fields stored inline after blob miss (Real execution only).");

    /// <summary>Agent output semantic quality score distribution (0-1; label <c>agent_type</c>).</summary>
    public static readonly Histogram<double> AgentOutputSemanticScore =
        AppMeter.CreateHistogram<double>(
            "archlucid_agent_output_semantic_score",
            description: "Agent output semantic quality score (0-1).");

    /// <summary>
    /// Quality gate outcomes after structural + semantic evaluation (labels: <c>agent_type</c>, <c>outcome</c>=accepted|warned|rejected).
    /// </summary>
    public static readonly Counter<long> AgentOutputQualityGateTotal =
        AppMeter.CreateCounter<long>(
            "archlucid_agent_output_quality_gate_total",
            description: "Agent output quality gate outcomes (labels: agent_type, outcome=accepted|warned|rejected).");

    /// <summary>Reference-case evaluation outcomes (labels: <c>case_id</c>, <c>agent_type</c>, <c>outcome</c>=pass|fail).</summary>
    public static readonly Counter<long> AgentOutputReferenceCaseEvaluationsTotal =
        AppMeter.CreateCounter<long>(
            "archlucid_agent_output_reference_case_evaluations_total",
            description: "Reference-case agent output evaluations (labels: case_id, agent_type, outcome=pass|fail).");

    /// <summary>Mean of structural completeness and semantic score for each reference-case evaluation (labels: <c>case_id</c>, <c>agent_type</c>).</summary>
    public static readonly Histogram<double> AgentOutputReferenceCaseScoreRatio =
        AppMeter.CreateHistogram<double>(
            "archlucid_agent_output_reference_case_score_ratio",
            description: "Combined reference-case score (structural+semantic)/2 (0.0–1.0).");

    /// <summary>
    /// Aggregate explanation replaced LLM text with deterministic manifest narrative due to low explanation faithfulness.
    /// </summary>
    public static readonly Counter<long> ExplanationAggregateFaithfulnessFallbacksTotal =
        AppMeter.CreateCounter<long>(
            "archlucid_explanation_aggregate_faithfulness_fallback_total",
            description: "Aggregate run explanation used deterministic narrative after low faithfulness vs findings.");

    /// <summary>Rows detected by consistency probes referencing missing authority state (labels <c>table</c>, <c>column</c>).</summary>
    public static readonly Counter<long> DataConsistencyOrphansDetected =
        AppMeter.CreateCounter<long>(
            "archlucid_data_consistency_orphans_detected_total",
            description: "Orphan coordinator rows detected (labels table, column: LeftRunId or RightRunId).");

    /// <summary>Audit events dropped because the in-memory retry queue was full (hot-path enqueue or requeue after drain failure).</summary>
    public static readonly Counter<long> AuditRetryEnqueueDroppedTotal =
        AppMeter.CreateCounter<long>(
            "archlucid_audit_retry_enqueue_dropped_total",
            description: "Audit retry queue dropped events because the bounded channel was full.");

    /// <summary>Azure OpenAI chat completion prompt (input) tokens.</summary>
    public static readonly Counter<long> LlmPromptTokensTotal =
        AppMeter.CreateCounter<long>(
            "archlucid_llm_prompt_tokens_total",
            description: "Cumulative prompt tokens reported by Azure OpenAI completions.");

    /// <summary>Azure OpenAI chat completion output tokens.</summary>
    public static readonly Counter<long> LlmCompletionTokensTotal =
        AppMeter.CreateCounter<long>(
            "archlucid_llm_completion_tokens_total",
            description: "Cumulative completion tokens reported by Azure OpenAI completions.");

    /// <summary>
    /// Records LLM token counters. When <paramref name="recordPerTenant"/> is true, also emits tagged series with
    /// <c>tenant_id</c> (increases Prometheus cardinality — use only for bounded tenant counts).
    /// Optional <paramref name="llmProviderId"/> and <paramref name="llmDeploymentLabel"/> add low-cardinality series for FinOps dashboards.
    /// </summary>
    /// <summary>
    /// Associates <paramref name="accumulator"/> with the current async flow so the agent host&apos;s completion client
    /// can count remote completions toward <see cref="LlmCallsPerRun"/>. Dispose to detach.
    /// </summary>
    public static IDisposable BeginLlmCallsPerRunAccumulation(AgentExecutionLlmCallAccumulator accumulator)
    {
        ArgumentNullException.ThrowIfNull(accumulator);

        LlmCallsPerRunAccumulator.Value = accumulator;

        return new LlmCallsPerRunAccumulationScope();
    }

    /// <summary>Increments the current batch&apos;s LLM completion count when an accumulator scope is active.</summary>
    public static void RecordLlmCompletionCallForCurrentRunBatch()
    {
        AgentExecutionLlmCallAccumulator? acc = LlmCallsPerRunAccumulator.Value;

        if (acc is not null)
        {
            acc.AddCompletions(1);
        }
    }

    /// <summary>Increments <c>archlucid_finding_engine_failures_total</c>.</summary>
    public static void RecordFindingEngineFailure(string engineType, string category)
    {
        TagList tags = new()
        {
            { "engine_type", engineType },
            { "category", category },
        };

        FindingEngineFailuresTotal.Add(1, tags);
    }

    /// <summary>Increments <c>archlucid_agent_result_schema_validations_total</c> (outcome: valid or invalid).</summary>
    public static void RecordAgentResultSchemaValidation(string agentType, string outcome)
    {
        TagList tags = new()
        {
            { "agent_type", agentType },
            { "outcome", outcome },
        };

        AgentResultSchemaValidationsTotal.Add(1, tags);
    }

    /// <summary>Increments <c>archlucid_explanation_schema_validations_total</c> (outcome: valid, invalid, or skipped).</summary>
    public static void RecordExplanationSchemaValidation(string explanationType, string outcome)
    {
        TagList tags = new()
        {
            { "explanation_type", explanationType },
            { "outcome", outcome },
        };

        ExplanationSchemaValidationsTotal.Add(1, tags);
    }

    /// <summary>Records <see cref="ExplanationFaithfulnessRatio"/> (clamped 0–1).</summary>
    public static void RecordExplanationFaithfulnessRatio(double ratio)
    {
        double clamped = Math.Clamp(ratio, 0.0, 1.0);
        ExplanationFaithfulnessRatio.Record(clamped);
    }

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

    private readonly struct LlmCallsPerRunAccumulationScope : IDisposable
    {
        public void Dispose() => LlmCallsPerRunAccumulator.Value = null;
    }
}
