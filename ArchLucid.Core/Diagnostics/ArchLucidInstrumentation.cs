using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace ArchLucid.Core.Diagnostics;

/// <summary>
///     Thread-safe completion count for one <c>RealAgentExecutor.ExecuteAsync</c> batch (parallel handlers share one
///     instance).
/// </summary>
public sealed class AgentExecutionLlmCallAccumulator
{
    private int _count;

    /// <summary>Adds <paramref name="delta" /> successful remote completions (ignored if non-positive).</summary>
    public void AddCompletions(int delta)
    {
        if (delta > 0)

            _ = Interlocked.Add(ref _count, delta);
    }

    /// <summary>Reads and resets the accumulated count.</summary>
    public int Consume()
    {
        return Interlocked.Exchange(ref _count, 0);
    }
}

/// <summary>
///     Shared <see cref="ActivitySource" /> and <see cref="Meter" /> names for cross-cutting observability (OTel wiring in
///     the API host).
/// </summary>
public static class ArchLucidInstrumentation
{
    /// <summary>Meter name registered with OpenTelemetry in <c>AddArchLucidOpenTelemetry</c>.</summary>
    public const string MeterName = "ArchLucid";

    private static readonly Meter AppMeter = new(MeterName, "1.0.0");

    private static readonly AsyncLocal<AgentExecutionLlmCallAccumulator?> LlmCallsPerRunAccumulator = new();

    private static int _outboxObservableGaugesRegistered;

    private static int _trialFunnelObservableGaugesRegistered;

    private static int _llmCompletionCacheObservableInstrumentsRegistered;

    private static long _llmCompletionCacheHitsAggregate;

    private static long _llmCompletionCacheMissesAggregate;

    private static long _trialActiveTenantsCached;

    private static Func<long>? _auditRetryQueuePendingReader;

    private static long _rlsBypassProductionLikeEnabled;

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
    public static readonly Counter<long> DigestDeliverySucceeded =
        AppMeter.CreateCounter<long>("digest_delivery_succeeded");

    /// <summary>Digest channel send failed after non-cancellation error (labels: <c>channel</c>).</summary>
    public static readonly Counter<long> DigestDeliveryFailed = AppMeter.CreateCounter<long>("digest_delivery_failed");

    /// <summary>
    ///     Outbound HTTP webhook POST attempts (<c>IWebhookPoster</c>; labels <c>event_type</c>, <c>succeeded</c>=true|false).
    /// </summary>
    public static readonly Counter<long> WebhookDeliveries =
        AppMeter.CreateCounter<long>(
            "archlucid.webhook.deliveries",
            description:
            "Webhook HTTP deliveries (labels event_type low-cardinality literal, succeeded=true|false).");

    /// <summary>Wall-clock HTTP POST latency for webhook deliveries (ms; label <c>event_type</c>).</summary>
    public static readonly Histogram<double> WebhookDeliveryDurationMilliseconds =
        AppMeter.CreateHistogram<double>(
            "archlucid.webhook.delivery_duration",
            "ms",
            "Outbound webhook HTTP POST attempt duration.");

    /// <summary>
    ///     Wall time for <c>EvaluateAndPersistAsync</c> (labels: <c>rule_kind</c> = <c>simple</c> | <c>composite</c>).
    /// </summary>
    public static readonly Histogram<double> AlertEvaluationDurationMilliseconds = AppMeter.CreateHistogram<double>(
        "alert_evaluation_duration_ms",
        "ms",
        "Time spent in alert EvaluateAndPersistAsync per rule kind.");

    /// <summary>Wall time for effective governance resolution (<c>IEffectiveGovernanceResolver.ResolveAsync</c>).</summary>
    public static readonly Histogram<double> GovernanceResolveDurationMilliseconds = AppMeter.CreateHistogram<double>(
        "governance_resolve_duration_ms",
        "ms",
        "Time to resolve effective governance for a tenant/workspace/project scope.");

    /// <summary>
    ///     Per advisory scan: fraction of explainability trace fields populated across findings (0.0–1.0; label
    ///     <c>scan_type</c>).
    /// </summary>
    public static readonly Histogram<double> ExplainabilityTraceCompleteness = AppMeter.CreateHistogram<double>(
        "archlucid_explainability_trace_completeness_ratio",
        description: "Per-scan trace completeness ratio (0.0–1.0).");

    /// <summary>
    ///     Heuristic overlap between aggregate explanation tokens and flattened finding <c>ExplainabilityTrace</c> text
    ///     (0.0–1.0).
    /// </summary>
    public static readonly Histogram<double> ExplanationFaithfulnessRatio = AppMeter.CreateHistogram<double>(
        "archlucid_explanation_faithfulness_ratio",
        description: "Heuristic faithfulness of run explanation vs finding traces (0.0–1.0).");

    /// <summary>
    ///     Per provenance response: fraction of manifest decisions with finding, rule, and graph-context edges (0.0–1.0).
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

    /// <summary>
    ///     LLM completions rejected by per-tenant sliding-window token quota or UTC-day budget (pre-call, in
    ///     <c>LlmCompletionAccountingClient</c>).
    /// </summary>
    public static readonly Counter<long> LlmQuotaExceededTotal =
        AppMeter.CreateCounter<long>(
            "archlucid_llm_quota_exceeded_total",
            description: "LLM calls rejected by tenant token quota or daily budget before outbound completion.");

    /// <summary>Half-open probe results (labels: <c>gate</c>, <c>outcome</c>=success|failure|cancelled).</summary>
    public static readonly Counter<long> CircuitBreakerProbeOutcomes =
        AppMeter.CreateCounter<long>(
            "archlucid_circuit_breaker_probe_outcomes_total",
            description: "Half-open probe results (labels: gate, outcome=success|failure|cancelled).");

    /// <summary>
    ///     LLM call retry attempts before the circuit breaker records a failure (labels: <c>gate</c>, <c>attempt</c>,
    ///     <c>exception_type</c>).
    /// </summary>
    public static readonly Counter<long> LlmCallRetries =
        AppMeter.CreateCounter<long>(
            "archlucid_llm_call_retries_total",
            description:
            "LLM call retry attempts before circuit breaker recording (labels: gate, attempt, exception_type).");

    /// <summary>
    ///     Hits on the in-resolve <c>(packId, version)</c> deserialized content cache inside
    ///     <c>EffectiveGovernanceResolver</c>
    ///     (avoids duplicate JSON work when the same version appears on multiple assignments).
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

    /// <summary>
    ///     Operator new-run wizard cost-preview fetches when <c>AgentExecution:Mode=Real</c> (no tenant / PII tags).
    /// </summary>
    public static readonly Counter<long> RunsCostPreviewViewedTotal =
        AppMeter.CreateCounter<long>(
            "archlucid.runs.cost_preview.viewed_total",
            description: "GET /v1/agent-execution/cost-preview served for Real mode (wizard review step).");

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
            description:
            "Finding engines that failed during findings snapshot generation (labels: engine_type, category).");

    /// <summary>LLM completion calls made during a single <c>RealAgentExecutor.ExecuteAsync</c> batch.</summary>
    public static readonly Histogram<int> LlmCallsPerRun =
        AppMeter.CreateHistogram<int>(
            "archlucid_llm_calls_per_run",
            "{call}",
            "Number of LLM completion calls made during a single authority run.");

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

    /// <summary>LLM completion response cache hits (<c>CachingLlmCompletionClient</c>, label <c>agent_type</c>).</summary>
    public static readonly Counter<long> LlmCompletionCacheHitsTotal =
        AppMeter.CreateCounter<long>(
            "archlucid_llm_cache_hits_total",
            description: "LLM completion response cache hits (label: agent_type).");

    /// <summary>LLM completion response cache misses (<c>CachingLlmCompletionClient</c>, label <c>agent_type</c>).</summary>
    public static readonly Counter<long> LlmCompletionCacheMissesTotal =
        AppMeter.CreateCounter<long>(
            "archlucid_llm_cache_misses_total",
            description: "LLM completion response cache misses (label: agent_type).");


    /// <summary>In-process cache hits for <c>GET /v1/demo/preview</c> (marketing commit-page bundle).</summary>
    public static readonly Counter<long> DemoPreviewCacheHits =
        AppMeter.CreateCounter<long>(
            "archlucid.demo.preview.cache_hit_total",
            description: "Demo marketing preview bundle cache hits (GET /v1/demo/preview).");

    /// <summary>In-process cache misses for <c>GET /v1/demo/preview</c> (factory invoked).</summary>
    public static readonly Counter<long> DemoPreviewCacheMisses =
        AppMeter.CreateCounter<long>(
            "archlucid.demo.preview.cache_miss_total",
            description: "Demo marketing preview bundle cache misses (GET /v1/demo/preview).");

    /// <summary><c>archlucid try --real</c> path: execute invoked with pilot try header (API-side proxy for CLI intent).</summary>
    public static readonly Counter<long> TryRealModeAttemptedTotal =
        AppMeter.CreateCounter<long>(
            "archlucid.try.real_mode.attempted_total",
            description: "archlucid try --real: pilot-marked execute attempts.");

    /// <summary><c>archlucid try --real</c> path: pilot-marked execute returned success.</summary>
    public static readonly Counter<long> TryRealModeSucceededTotal =
        AppMeter.CreateCounter<long>(
            "archlucid.try.real_mode.succeeded_total",
            description: "archlucid try --real: pilot-marked execute successes.");

    /// <summary><c>archlucid try --real</c> path: simulator substitution after seed-fake-results fallback.</summary>
    public static readonly Counter<long> TryRealModeFellBackToSimulatorTotal =
        AppMeter.CreateCounter<long>(
            "archlucid.try.real_mode.fellback_to_simulator_total",
            description: "archlucid try --real: fell back to simulator output (development seed path).");

    /// <summary>
    ///     Schema validation of raw <c>AgentResult</c> LLM JSON (labels: <c>agent_type</c>, <c>outcome</c>
    ///     =valid|invalid).
    /// </summary>
    public static readonly Counter<long> AgentResultSchemaValidationsTotal =
        AppMeter.CreateCounter<long>(
            "archlucid_agent_result_schema_validations_total",
            description: "Schema validation of raw AgentResult LLM output (labels: agent_type, outcome).");

    /// <summary>Follow-up LLM attempts after an <c>AgentResult</c> schema violation (label: <c>agent_type</c>).</summary>
    public static readonly Counter<long> AgentSchemaRemediationRetriesTotal =
        AppMeter.CreateCounter<long>(
            "archlucid.agent.schema_remediation_retries_total",
            description: "Remediation LLM attempts after AgentResult schema validation failed (label: agent_type).");

    /// <summary>
    ///     Schema validation of explanation LLM JSON (labels: <c>explanation_type</c>, <c>outcome</c>
    ///     =valid|invalid|skipped).
    /// </summary>
    public static readonly Counter<long> ExplanationSchemaValidationsTotal =
        AppMeter.CreateCounter<long>(
            "archlucid_explanation_schema_validations_total",
            description: "Schema validation of explanation LLM payloads (labels: explanation_type, outcome).");

    /// <summary>Per-stage wall time inside the authority pipeline (labels: <c>stage</c>, <c>outcome</c>=success|error).</summary>
    public static readonly Histogram<double> AuthorityPipelineStageDurationMilliseconds =
        AppMeter.CreateHistogram<double>(
            "archlucid_authority_pipeline_stage_duration_ms",
            "ms",
            "Per-stage wall time inside the authority pipeline (labels: stage, outcome).");

    /// <summary>Successful self-service trial activations (labels: <c>source</c>, <c>mode</c>).</summary>
    public static readonly Counter<long> TrialSignupsTotal =
        AppMeter.CreateCounter<long>(
            "archlucid_trial_signups_total",
            description: "Self-service trial funnel: successful trial activations (labels: source, mode).");

    /// <summary>Failed signup / trial bootstrap attempts (labels: <c>stage</c>, <c>reason</c>).</summary>
    public static readonly Counter<long> TrialSignupFailuresTotal =
        AppMeter.CreateCounter<long>(
            "archlucid_trial_signup_failures_total",
            description: "Self-service trial funnel: failed signup or bootstrap attempts (labels: stage, reason).");

    /// <summary>Background health check of <c>GET /v1/demo/preview</c> (labels: <c>outcome</c>=success|failure).</summary>
    public static readonly Counter<long> TrialFunnelHealthProbeTotal =
        AppMeter.CreateCounter<long>(
            "archlucid_trial_funnel_health_probe_total",
            description: "Trial funnel demo preview probe outcomes (label outcome=success|failure).");

    /// <summary>Failed <c>POST /v1/register</c> HTTP responses (labels: <c>reason</c>=validation|conflict|internal).</summary>
    public static readonly Counter<long> TrialRegistrationFailuresTotal =
        AppMeter.CreateCounter<long>(
            "archlucid_trial_registration_failures_total",
            description: "Self-service registration API failures (label reason=validation|conflict|internal).");

    /// <summary>
    ///     Successful <c>POST /v1/register</c> where the prospect did not supply <c>baselineReviewCycleHours</c> (soft-default
    ///     / model path).
    /// </summary>
    public static readonly Counter<long> TrialSignupBaselineSkippedTotal =
        AppMeter.CreateCounter<long>(
            "archlucid_trial_signup_baseline_skipped_total",
            description: "Self-service trial signup completed without tenant-supplied baseline review-cycle hours.");

    /// <summary>Manual prep / people-per-review baseline persisted (settings UI gate).</summary>
    public static readonly Counter<long> BaselineManualPrepCapturedTotal =
        AppMeter.CreateCounter<long>(
            "archlucid_baseline_manual_prep_captured_total",
            description: "Tenant manual baseline fields saved (PUT /v1/tenant/baseline).");

    /// <summary>Seconds from trial anchor (<c>TrialStartUtc</c> when set, otherwise <c>CreatedUtc</c>) to first committed manifest.</summary>
    public static readonly Histogram<double> TrialFirstRunSeconds =
        AppMeter.CreateHistogram(
            "archlucid_trial_first_run_seconds",
            "s",
            "Seconds from tenant trial anchor (TrialStartUtc or CreatedUtc) to first committed manifest.",
            advice: new InstrumentAdvice<double>
            {
                HistogramBucketBoundaries =
                [
                    5, 15, 30, 60, 120, 300, 600, 1200, 3600, 7200, 86400
                ]
            });

    /// <summary>
    ///     Seconds from tenant anchor to first golden manifest commit for any tenant (labels: <c>tenant_kind</c>
    ///     = <c>trial</c> | <c>non_trial</c>).
    /// </summary>
    public static readonly Histogram<double> TenantTimeToFirstCommitSeconds =
        AppMeter.CreateHistogram(
            "archlucid_tenant_time_to_first_commit_seconds",
            "s",
            "Seconds from tenant anchor (TrialStartUtc or CreatedUtc) to first committed manifest (all tenants).",
            advice: new InstrumentAdvice<double>
            {
                HistogramBucketBoundaries =
                [
                    5, 15, 30, 60, 120, 300, 600, 1200, 3600, 7200, 86400
                ]
            });

    /// <summary><c>TrialRunsUsed / TrialRunsLimit</c> at first manifest commit for metered trials (0.0–1.0+).</summary>
    public static readonly Histogram<double> TrialRunsUsedRatio =
        AppMeter.CreateHistogram(
            "archlucid_trial_runs_used_ratio",
            description: "TrialRunsUsed divided by TrialRunsLimit when the first manifest commits (labels none).",
            advice: new InstrumentAdvice<double>
            {
                HistogramBucketBoundaries =
                [
                    0.05, 0.1, 0.2, 0.35, 0.5, 0.65, 0.8, 0.95, 1.0, 1.25, 2.0
                ]
            });

    /// <summary>Trial conversions to paid or higher tier (labels: <c>from_state</c>, <c>to_tier</c>).</summary>
    public static readonly Counter<long> TrialConversionTotal =
        AppMeter.CreateCounter<long>(
            "archlucid_trial_conversion_total",
            description: "Trial conversions (labels: from_state, to_tier).");

    /// <summary>Automated lifecycle transitions toward expiry / deletion (label <c>reason</c>).</summary>
    public static readonly Counter<long> TrialExpirationsTotal =
        AppMeter.CreateCounter<long>(
            "archlucid_trial_expirations_total",
            description: "Trial lifecycle transitions applied by automation (label: reason).");

    /// <summary>First successful golden-manifest commit per tenant (Core Pilot onboarding funnel).</summary>
    public static readonly Counter<long> FirstSessionCompletedTotal =
        AppMeter.CreateCounter<long>(
            "archlucid_first_session_completed_total",
            description: "Increments once per tenant on first successful manifest commit.");

    /// <summary>
    ///     First-tenant onboarding funnel events (Improvement 12). Aggregated counter — the
    ///     <c>event</c> tag is the only label by default. The <c>tenant_id</c> tag is added only when the
    ///     <c>Telemetry:FirstTenantFunnel:PerTenantEmission</c> feature flag is on (owner-only flip per
    ///     pending question 40 / <c>docs/security/PRIVACY_NOTE.md</c> §3.A).
    /// </summary>
    public static readonly Counter<long> FirstTenantFunnelEventsTotal =
        AppMeter.CreateCounter<long>(
            "archlucid_first_tenant_funnel_events_total",
            description:
            "First-tenant onboarding funnel events (label: event=signup|tour_opt_in|first_run_started|first_run_committed|first_finding_viewed|thirty_minute_milestone). tenant_id label added ONLY when Telemetry:FirstTenantFunnel:PerTenantEmission is true.");

    /// <summary>
    ///     Operator onboarding funnel successes (labels: <c>task</c> = <c>first_run_committed</c> |
    ///     <c>first_session_completed</c>).
    /// </summary>
    public static readonly Counter<long> OperatorTaskSuccessTotal =
        AppMeter.CreateCounter<long>(
            "archlucid_operator_task_success_total",
            description:
            "Server-side verified onboarding milestones (label task=first_run_committed|first_session_completed).");

    /// <summary>
    ///     Operator UI sponsor banner showed the days-since-first-commit badge (labels: <c>tenant_id</c>,
    ///     <c>days_since_first_commit_bucket</c>).
    /// </summary>
    public static readonly Counter<long> SponsorBannerFirstCommitBadgeRenderedTotal =
        AppMeter.CreateCounter<long>(
            "archlucid.ui.sponsor_banner.first_commit_badge_rendered",
            description:
            "Sponsor banner first-commit badge render (operator shell). Labels: tenant_id, days_since_first_commit_bucket.");

    /// <summary>Deny-list redactions applied before Azure OpenAI and trace persistence (label <c>category</c>).</summary>
    public static readonly Counter<long> LlmPromptRedactionsTotal =
        AppMeter.CreateCounter<long>(
            "archlucid_llm_prompt_redactions_total",
            description: "Outbound LLM prompt redactions (labels: email|ssn|credit_card|jwt|api_key|custom).");

    /// <summary>LLM completions while <c>LlmPromptRedaction:Enabled</c> is false (audit deliberate bypass).</summary>
    public static readonly Counter<long> LlmPromptRedactionSkippedTotal =
        AppMeter.CreateCounter<long>(
            "archlucid_llm_prompt_redaction_skipped_total",
            description: "LLM completions observed while prompt redaction is disabled.");

    /// <summary>Billing checkout attempts (labels: <c>provider</c>, <c>tier</c>, <c>outcome</c>).</summary>
    public static readonly Counter<long> BillingCheckoutsTotal =
        AppMeter.CreateCounter<long>(
            "archlucid_billing_checkouts_total",
            description: "Billing checkout sessions (labels: provider, tier, outcome).");

    /// <summary>Production agent handler completions (label: <c>agent_type_key</c>, <c>outcome</c>=success|error).</summary>
    public static readonly Counter<long> AgentHandlerInvocationsTotal =
        AppMeter.CreateCounter<long>(
            "archlucid_agent_handler_invocations_total",
            description: "Agent handler invocations by type and outcome.");

    /// <summary>
    ///     Fraction of expected <c>AgentResult</c> JSON keys present on <c>ParsedResultJson</c> (0.0–1.0; label
    ///     <c>agent_type</c>).
    /// </summary>
    public static readonly Histogram<double> AgentOutputStructuralCompletenessRatio =
        AppMeter.CreateHistogram<double>(
            "archlucid_agent_output_structural_completeness_ratio",
            description: "Structural completeness of persisted agent parsed JSON (0.0–1.0).");

    /// <summary>
    ///     Trace JSON that is not a JSON object or failed <see cref="System.Text.Json" /> parse (label <c>agent_type</c>
    ///     ).
    /// </summary>
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
            "ms",
            "Duration in milliseconds for full prompt/response blob writes per trace.");

    /// <summary>
    ///     Real-mode SQL inline fallback for full prompt/response when blob key is missing (labels: <c>agent_type</c>,
    ///     <c>blob_type</c>=system_prompt|user_prompt|response).
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
    ///     Quality gate outcomes after structural + semantic evaluation (labels: <c>agent_type</c>, <c>outcome</c>
    ///     =accepted|warned|rejected).
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

    /// <summary>
    ///     Mean of structural completeness and semantic score for each reference-case evaluation (labels: <c>case_id</c>,
    ///     <c>agent_type</c>).
    /// </summary>
    public static readonly Histogram<double> AgentOutputReferenceCaseScoreRatio =
        AppMeter.CreateHistogram<double>(
            "archlucid_agent_output_reference_case_score_ratio",
            description: "Combined reference-case score (structural+semantic)/2 (0.0–1.0).");

    /// <summary>
    ///     Aggregate explanation replaced LLM text with deterministic manifest narrative due to low explanation faithfulness.
    /// </summary>
    public static readonly Counter<long> ExplanationAggregateFaithfulnessFallbacksTotal =
        AppMeter.CreateCounter<long>(
            "archlucid_explanation_aggregate_faithfulness_fallback_total",
            description: "Aggregate run explanation used deterministic narrative after low faithfulness vs findings.");

    /// <summary>Citation chips emitted on aggregate explanations (label <c>kind</c>: <c>CitationKind</c> enum name).</summary>
    public static readonly Counter<long> ExplanationCitationsEmitted =
        AppMeter.CreateCounter<long>(
            "archlucid_explanation_citations_emitted_total",
            description: "Citation references attached to aggregate run explanations (label kind).");

    /// <summary>Rows detected by consistency probes referencing missing authority state (labels <c>table</c>, <c>column</c>).</summary>
    public static readonly Counter<long> DataConsistencyOrphansDetected =
        AppMeter.CreateCounter<long>(
            "archlucid_data_consistency_orphans_detected_total",
            description: "Orphan coordinator rows detected (labels table, column: LeftRunId or RightRunId).");

    /// <summary>Environment-graded alert when orphan count meets threshold (labels <c>table</c>, <c>column</c>).</summary>
    public static readonly Counter<long> DataConsistencyAlerts =
        AppMeter.CreateCounter<long>(
            "archlucid_data_consistency_alerts_total",
            description: "Data consistency enforcement alert increments (labels table, column).");

    /// <summary>Rows inserted into <c>dbo.DataConsistencyQuarantine</c> from orphan probes (labels <c>table</c>, <c>column</c>).</summary>
    public static readonly Counter<long> DataConsistencyOrphansQuarantined =
        AppMeter.CreateCounter<long>(
            "archlucid_data_consistency_orphans_quarantined_total",
            description: "Orphan rows quarantined (inserted into dbo.DataConsistencyQuarantine; labels table, column).");

    /// <summary>Wall time for scheduled read-only data consistency reconciliation (full pass).</summary>
    public static readonly Histogram<double> DataConsistencyReconciliationDurationMilliseconds =
        AppMeter.CreateHistogram<double>(
            "archlucid_data_consistency_check_duration_ms",
            "ms",
            "Wall time for scheduled data consistency reconciliation (read-only checks).");

    /// <summary>Findings emitted during data consistency reconciliation (labels <c>severity</c>, <c>check_name</c>).</summary>
    public static readonly Counter<long> DataConsistencyReconciliationFindingsTotal =
        AppMeter.CreateCounter<long>(
            "archlucid_data_consistency_findings_total",
            description: "Data consistency reconciliation findings (labels severity, check_name).");

    /// <summary><c>ArchLucid.Jobs.Cli</c> / <c>IArchLucidJob</c> executions (labels: <c>job_name</c>, <c>exit_class</c>).</summary>
    public static readonly Counter<long> ContainerJobRunsTotal =
        AppMeter.CreateCounter<long>(
            "archlucid_container_job_runs_total",
            description:
            "ArchLucid.Jobs.Cli job runs (labels: job_name, exit_class=success|failure|unknown_job|configuration_error|cancelled).");

    /// <summary>Wall time for <c>IArchLucidJob.RunOnceAsync</c> (labels: <c>job_name</c>, <c>exit_code</c>).</summary>
    public static readonly Histogram<double> ContainerJobRunDurationMilliseconds =
        AppMeter.CreateHistogram<double>(
            "archlucid_container_job_run_duration_ms",
            "ms",
            "Duration of one-shot background jobs (labels: job_name, exit_code).");

    /// <summary>
    ///     Audit events dropped because the in-memory retry queue was full (hot-path enqueue or requeue after drain
    ///     failure).
    /// </summary>
    public static readonly Counter<long> AuditRetryEnqueueDroppedTotal =
        AppMeter.CreateCounter<long>(
            "archlucid_audit_retry_enqueue_dropped_total",
            description: "Audit retry queue dropped events because the bounded channel was full.");

    /// <summary>
    ///     Durable SQL audit writes abandoned after <see cref="ArchLucid.Core.Audit.DurableAuditLogRetry.TryLogAsync" />
    ///     exhausted retries (label <c>event_type</c>).
    /// </summary>
    public static readonly Counter<long> AuditWriteFailuresTotal =
        AppMeter.CreateCounter<long>(
            "archlucid_audit_write_failures_total",
            description: "Durable audit writes abandoned after max retries (label event_type).");

    /// <summary>
    ///     Startup configuration advisory warnings (label <c>rule_name</c>) — bounded code constants only (TECH_BACKLOG TB-002).
    /// </summary>
    public static readonly Counter<long> StartupConfigWarningsTotal =
        AppMeter.CreateCounter<long>(
            "archlucid_startup_config_warnings_total",
            description: "Non-fatal startup configuration warnings (label rule_name).");

    /// <summary>
    ///     Observed latency for named SQL/query gates (TECH_BACKLOG TB-003 parity with CI allowlist; label <c>query_name</c>).
    /// </summary>
    public static readonly Histogram<double> QueryNamedLatencyMilliseconds =
        AppMeter.CreateHistogram<double>(
            "archlucid_query_p95_ms",
            "ms",
            "Latency snapshot for named query performance regression gates (label query_name).");

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
    ///     Estimated LLM spend (USD) from configured per-million token rates on recorded traces (label <c>tenant</c>).
    /// </summary>
    public static readonly Counter<double> LlmCostUsdTotal =
        AppMeter.CreateCounter<double>(
            "archlucid_llm_cost_usd_total",
            "USD",
            "Estimated LLM USD from token counts × AgentExecution:LlmCostEstimation rates (label tenant).");

    /// <summary>Latest outbox depths for <see cref="EnsureOutboxDepthObservableGaugesRegistered" />.</summary>
    public static OutboxDepthGaugeState OutboxDepthGauges
    {
        get;
    } = new();

    /// <summary>
    ///     Supplies pending audit-retry depth for <c>archlucid_audit_retry_queue_pending</c> (last writer wins; use a
    ///     singleton queue).
    /// </summary>
    public static void SetAuditRetryQueuePendingReader(Func<long>? reader)
    {
        Volatile.Write(ref _auditRetryQueuePendingReader, reader);
    }

    /// <summary>
    ///     Sets the observable gauge backing <c>archlucid_rls_bypass_enabled_info</c> (1 when break-glass is on in a
    ///     production-like host).
    /// </summary>
    public static void SetRlsBypassProductionLikeEnabled(long zeroOrOne)
    {
        Volatile.Write(ref _rlsBypassProductionLikeEnabled, zeroOrOne != 0 ? 1 : 0);
    }

    /// <summary>Registers observable gauges once (call from OpenTelemetry host setup).</summary>
    public static void EnsureOutboxDepthObservableGaugesRegistered()
    {
        if (Interlocked.Exchange(ref _outboxObservableGaugesRegistered, 1) != 0)
            return;


        OutboxDepthGaugeState s = OutboxDepthGauges;

        AppMeter.CreateObservableGauge(
            "archlucid_authority_pipeline_work_pending",
            () => new Measurement<long>(s.Current.AuthorityPipelineWorkPending),
            description: "Rows in dbo.AuthorityPipelineWorkOutbox awaiting processing.");

        AppMeter.CreateObservableGauge(
            "archlucid_authority_pipeline_work_oldest_pending_age_seconds",
            () => new Measurement<double>(s.Current.AuthorityPipelineWorkOldestPendingAgeSeconds),
            "s",
            "Age in seconds of the oldest pending authority pipeline work outbox row.");

        AppMeter.CreateObservableGauge(
            "archlucid_retrieval_indexing_outbox_pending",
            () => new Measurement<long>(s.Current.RetrievalIndexingOutboxPending),
            description: "Rows in dbo.RetrievalIndexingOutbox awaiting indexing.");

        AppMeter.CreateObservableGauge(
            "archlucid_retrieval_indexing_outbox_oldest_pending_age_seconds",
            () => new Measurement<double>(s.Current.RetrievalIndexingOutboxOldestPendingAgeSeconds),
            "s",
            "Age in seconds of the oldest pending retrieval indexing outbox row.");

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
            "s",
            "Age in seconds of the oldest actionable integration outbox publish row.");

        AppMeter.CreateObservableGauge(
            "archlucid_audit_retry_queue_pending",
            () => new Measurement<long>(_auditRetryQueuePendingReader?.Invoke() ?? 0),
            description: "Approximate audit events waiting in memory for durable write after hot-path failure.");

        AppMeter.CreateObservableGauge(
            "archlucid_rls_bypass_enabled_info",
            () => new Measurement<long>(
                Volatile.Read(ref _rlsBypassProductionLikeEnabled),
                new KeyValuePair<string, object?>("scope", "production_like")),
            description:
            "1 when SQL RLS break-glass bypass is enabled (env + ArchLucid:Persistence:AllowRlsBypass) on a Production/Staging-classified host.");
    }

    /// <summary>Registers trial funnel observable gauges once (call from OpenTelemetry host setup).</summary>
    public static void EnsureTrialFunnelObservableGaugesRegistered()
    {
        if (Interlocked.Exchange(ref _trialFunnelObservableGaugesRegistered, 1) != 0)
            return;


        AppMeter.CreateObservableGauge(
            "archlucid_trial_active_tenants",
            () => new Measurement<long>(Volatile.Read(ref _trialActiveTenantsCached)),
            description:
            "Tenants currently on an active self-service trial (TrialStatus=Active, TrialExpiresUtc set).");
    }

    /// <summary>
    ///     Registers observable LLM completion cache instruments once (<c>CachingLlmCompletionClient</c>).
    /// </summary>
    public static void EnsureLlmCompletionCacheObservableInstrumentsRegistered()
    {
        if (Interlocked.Exchange(ref _llmCompletionCacheObservableInstrumentsRegistered, 1) != 0)

            return;


        AppMeter.CreateObservableGauge(
            "archlucid_llm_cache_hit_ratio",
            () =>
            {
                long hits = Interlocked.Read(ref _llmCompletionCacheHitsAggregate);
                long misses = Interlocked.Read(ref _llmCompletionCacheMissesAggregate);
                long denominator = hits + misses;

                double ratio = denominator == 0 ? 0 : hits / (double)denominator;

                return new Measurement<double>(ratio);
            },
            description: "Process-wide LLM completion cache hit ratio (hits / (hits + misses)) from CachingLlmCompletionClient.");
    }

    /// <summary>Updates the cached value read by <c>archlucid_trial_active_tenants</c> (background metrics collector).</summary>
    public static void PublishTrialActiveTenantCount(long count)
    {
        if (count < 0)

            count = 0;


        Volatile.Write(ref _trialActiveTenantsCached, count);
    }

    /// <summary>
    ///     Associates <paramref name="accumulator" /> with the current async flow so the agent host&apos;s completion client
    ///     can count remote completions toward <see cref="LlmCallsPerRun" />. Dispose to detach.
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

            acc.AddCompletions(1);
    }

    /// <summary>Records one LLM completion response cache hit (label <c>agent_type</c>).</summary>
    public static void RecordLlmCompletionCacheHit(string agentType)
    {
        string label = string.IsNullOrWhiteSpace(agentType) ? "unknown" : agentType.Trim();

        _ = Interlocked.Increment(ref _llmCompletionCacheHitsAggregate);

        TagList tags = new();
        tags.Add("agent_type", label);

        LlmCompletionCacheHitsTotal.Add(1, tags);
    }

    /// <summary>Records one LLM completion response cache miss (label <c>agent_type</c>).</summary>
    public static void RecordLlmCompletionCacheMiss(string agentType)
    {
        string label = string.IsNullOrWhiteSpace(agentType) ? "unknown" : agentType.Trim();

        _ = Interlocked.Increment(ref _llmCompletionCacheMissesAggregate);

        TagList tags = new();
        tags.Add("agent_type", label);

        LlmCompletionCacheMissesTotal.Add(1, tags);
    }

    /// <summary>Increments <c>archlucid.try.real_mode.attempted_total</c>.</summary>
    public static void RecordTryRealModePilotAttempted() => TryRealModeAttemptedTotal.Add(1);

    /// <summary>Increments <c>archlucid.try.real_mode.succeeded_total</c>.</summary>
    public static void RecordTryRealModePilotSucceeded() => TryRealModeSucceededTotal.Add(1);

    /// <summary>Increments <c>archlucid.try.real_mode.fellback_to_simulator_total</c>.</summary>
    public static void RecordTryRealModePilotFellBackToSimulator() => TryRealModeFellBackToSimulatorTotal.Add(1);

    /// <summary>Increments <c>archlucid_finding_engine_failures_total</c>.</summary>
    public static void RecordFindingEngineFailure(string engineType, string category)
    {
        TagList tags = new() { { "engine_type", engineType }, { "category", category } };

        FindingEngineFailuresTotal.Add(1, tags);
    }

    /// <summary>Increments <c>archlucid_agent_result_schema_validations_total</c> (outcome: valid or invalid).</summary>
    public static void RecordAgentResultSchemaValidation(string agentType, string outcome)
    {
        TagList tags = new() { { "agent_type", agentType }, { "outcome", outcome } };

        AgentResultSchemaValidationsTotal.Add(1, tags);
    }

    /// <summary>Increments <see cref="AgentSchemaRemediationRetriesTotal" />.</summary>
    public static void RecordAgentSchemaRemediationRetry(string agentTypeLabel)
    {
        string t = string.IsNullOrWhiteSpace(agentTypeLabel) ? "unknown" : agentTypeLabel.Trim();
        TagList tags = new() { { "agent_type", t } };

        AgentSchemaRemediationRetriesTotal.Add(1, tags);
    }

    /// <summary>Increments <c>archlucid_explanation_schema_validations_total</c> (outcome: valid, invalid, or skipped).</summary>
    public static void RecordExplanationSchemaValidation(string explanationType, string outcome)
    {
        TagList tags = new() { { "explanation_type", explanationType }, { "outcome", outcome } };

        ExplanationSchemaValidationsTotal.Add(1, tags);
    }

    /// <summary>Records <see cref="ExplanationFaithfulnessRatio" /> (clamped 0–1).</summary>
    public static void RecordExplanationFaithfulnessRatio(double ratio)
    {
        double clamped = Math.Clamp(ratio, 0.0, 1.0);
        ExplanationFaithfulnessRatio.Record(clamped);
    }

    /// <summary>Increments <see cref="TrialSignupsTotal" />.</summary>
    public static void RecordTrialSignup(string source, string mode)
    {
        TagList tags = new()
        {
            { "source", string.IsNullOrWhiteSpace(source) ? "unknown" : source.Trim() },
            { "mode", string.IsNullOrWhiteSpace(mode) ? "unknown" : mode.Trim() }
        };

        TrialSignupsTotal.Add(1, tags);
    }

    /// <summary>Increments <see cref="TrialSignupFailuresTotal" />.</summary>
    public static void RecordTrialSignupFailure(string stage, string reason)
    {
        TagList tags = new()
        {
            { "stage", string.IsNullOrWhiteSpace(stage) ? "unknown" : stage.Trim() },
            { "reason", string.IsNullOrWhiteSpace(reason) ? "unknown" : reason.Trim() }
        };

        TrialSignupFailuresTotal.Add(1, tags);
    }

    /// <summary>Increments <see cref="TrialFunnelHealthProbeTotal" /> (label: <c>outcome</c> success|failure).</summary>
    public static void RecordTrialFunnelHealthProbe(string outcome)
    {
        string o = string.IsNullOrWhiteSpace(outcome) ? "unknown" : outcome.Trim();
        if (o is not ("success" or "failure"))
            o = "unknown";
        TagList tags = new() { { "outcome", o } };
        TrialFunnelHealthProbeTotal.Add(1, tags);
    }

    /// <summary>Increments <see cref="TrialRegistrationFailuresTotal" /> (label: <c>reason</c> validation|conflict|internal).</summary>
    public static void RecordTrialRegistrationFailure(string reason)
    {
        string r = string.IsNullOrWhiteSpace(reason) ? "unknown" : reason.Trim();
        if (r is not ("validation" or "conflict" or "internal"))
            r = "unknown";
        TagList tags = new() { { "reason", r } };
        TrialRegistrationFailuresTotal.Add(1, tags);
    }

    /// <summary>Increments <see cref="TrialSignupBaselineSkippedTotal" /> (model-default baseline path at signup).</summary>
    public static void RecordTrialSignupBaselineSkipped()
    {
        TrialSignupBaselineSkippedTotal.Add(1);
    }

    /// <summary>Increments <see cref="BaselineManualPrepCapturedTotal" />.</summary>
    public static void RecordBaselineManualPrepCaptured()
    {
        BaselineManualPrepCapturedTotal.Add(1);
    }

    /// <summary>Records <see cref="TrialFirstRunSeconds" /> when positive and finite.</summary>
    public static void RecordTrialFirstRunLatencySeconds(double seconds)
    {
        if (seconds <= 0 || double.IsNaN(seconds) || double.IsInfinity(seconds))
            return;


        TrialFirstRunSeconds.Record(seconds);
    }

    /// <summary>
    ///     Records <see cref="TenantTimeToFirstCommitSeconds" /> for the first successful manifest pin (any tenant).
    /// </summary>
    public static void RecordTenantTimeToFirstCommitSeconds(double seconds, string tenantKind)
    {
        if (seconds <= 0 || double.IsNaN(seconds) || double.IsInfinity(seconds))
            return;


        string k = string.IsNullOrWhiteSpace(tenantKind) ? "unknown" : tenantKind.Trim();

        if (k is not ("trial" or "non_trial"))
            k = "unknown";

        TenantTimeToFirstCommitSeconds.Record(seconds, new TagList { { "tenant_kind", k } });
    }

    /// <summary>Records <see cref="TrialRunsUsedRatio" /> clamped to non-negative values.</summary>
    public static void RecordTrialRunsUsedRatio(double ratio)
    {
        if (double.IsNaN(ratio) || double.IsInfinity(ratio))
            return;


        TrialRunsUsedRatio.Record(Math.Max(0, ratio));
    }

    /// <summary>Increments <see cref="TrialConversionTotal" />.</summary>
    public static void RecordTrialConversion(string fromState, string toTier)
    {
        TagList tags = new()
        {
            { "from_state", string.IsNullOrWhiteSpace(fromState) ? "unknown" : fromState.Trim() },
            { "to_tier", string.IsNullOrWhiteSpace(toTier) ? "unknown" : toTier.Trim() }
        };

        TrialConversionTotal.Add(1, tags);
    }

    /// <summary>Increments <see cref="TrialExpirationsTotal" />.</summary>
    public static void RecordTrialExpiration(string reason)
    {
        string r = string.IsNullOrWhiteSpace(reason) ? "unknown" : reason.Trim();
        TagList tags = new() { { "reason", r } };

        TrialExpirationsTotal.Add(1, tags);
    }

    /// <summary>Increments <see cref="SponsorBannerFirstCommitBadgeRenderedTotal" />.</summary>
    public static void RecordSponsorBannerFirstCommitBadgeRendered(Guid tenantId, string daysSinceFirstCommitBucket)
    {
        string bucket = string.IsNullOrWhiteSpace(daysSinceFirstCommitBucket)
            ? "unknown"
            : daysSinceFirstCommitBucket.Trim();
        TagList tags = new() { { "tenant_id", tenantId.ToString("D") }, { "days_since_first_commit_bucket", bucket } };

        SponsorBannerFirstCommitBadgeRenderedTotal.Add(1, tags);
    }

    /// <summary>Increments <see cref="FirstSessionCompletedTotal" /> once per tenant (caller must gate).</summary>
    public static void RecordFirstSessionCompleted()
    {
        FirstSessionCompletedTotal.Add(1);
    }

    /// <summary>Increments <see cref="AuditWriteFailuresTotal" /> (label <c>event_type</c>).</summary>
    public static void RecordAuditWriteFailure(string eventType)
    {
        string e = string.IsNullOrWhiteSpace(eventType) ? "unknown" : eventType.Trim();
        AuditWriteFailuresTotal.Add(1, new TagList { { "event_type", e } });
    }

    /// <summary>
    ///     Increments <see cref="StartupConfigWarningsTotal"/> once per distinct advisory emission (TECH_BACKLOG TB-002).
    /// </summary>
    public static void RecordStartupConfigWarning(string ruleName)
    {
        string r = string.IsNullOrWhiteSpace(ruleName) ? "unknown" : ruleName.Trim();
        StartupConfigWarningsTotal.Add(1, new TagList { { "rule_name", r } });
    }

    /// <summary>Records a latency observation for TB-003 allowlisted queries (production or CI ingest).</summary>
    public static void RecordNamedQueryLatencyMilliseconds(string queryName, double milliseconds)
    {
        string q = string.IsNullOrWhiteSpace(queryName) ? "unknown" : queryName.Trim();
        QueryNamedLatencyMilliseconds.Record(milliseconds, new TagList { { "query_name", q } });
    }

    /// <summary>
    ///     Increments <see cref="FirstTenantFunnelEventsTotal" />. <paramref name="eventName" /> must be one of
    ///     <see cref="FirstTenantFunnelEventNames" />. <paramref name="tenantIdNormalized" /> is added as a
    ///     <c>tenant_id</c> tag <b>only</b> when <paramref name="recordPerTenant" /> is true (owner-only flag
    ///     per pending question 40). Never tags <c>userId</c>, IP, or any other personal data.
    /// </summary>
    public static void RecordFirstTenantFunnelEvent(
        string eventName,
        bool recordPerTenant,
        string? tenantIdNormalized)
    {
        if (!FirstTenantFunnelEventNames.IsValid(eventName))
            throw new ArgumentOutOfRangeException(
                nameof(eventName),
                eventName,
                $"eventName must be one of: {string.Join(", ", FirstTenantFunnelEventNames.All)}.");

        TagList tags = new() { { "event", eventName } };

        if (recordPerTenant && !string.IsNullOrEmpty(tenantIdNormalized))

            tags.Add("tenant_id", tenantIdNormalized);

        FirstTenantFunnelEventsTotal.Add(1, tags);
    }

    /// <summary>Increments <see cref="OperatorTaskSuccessTotal" /> for a low-cardinality <paramref name="task" /> label.</summary>
    public static void RecordOperatorTaskSuccess(string task)
    {
        string t = string.IsNullOrWhiteSpace(task) ? "unknown" : task.Trim();
        if (t is not ("first_run_committed" or "first_session_completed"))
            throw new ArgumentOutOfRangeException(nameof(task),
                "task must be first_run_committed or first_session_completed.");

        TagList tags = new() { { "task", t } };

        OperatorTaskSuccessTotal.Add(1, tags);
    }

    /// <summary>Records <see cref="LlmPromptRedactionsTotal" /> for a category bucket.</summary>
    public static void RecordLlmPromptRedactions(string category, int matchCount)
    {
        if (matchCount <= 0)
            return;


        string c = string.IsNullOrWhiteSpace(category) ? "unknown" : category.Trim();
        TagList tags = new() { { "category", c } };

        LlmPromptRedactionsTotal.Add(matchCount, tags);
    }

    /// <summary>Increments <see cref="LlmPromptRedactionSkippedTotal" /> when redaction is disabled.</summary>
    public static void RecordLlmPromptRedactionSkipped(int count = 1)
    {
        if (count <= 0)
            return;


        LlmPromptRedactionSkippedTotal.Add(count);
    }

    /// <summary>Increments <see cref="BillingCheckoutsTotal" />.</summary>
    public static void RecordBillingCheckout(string provider, string tier, string outcome)
    {
        TagList tags = new()
        {
            { "provider", string.IsNullOrWhiteSpace(provider) ? "unknown" : provider.Trim() },
            { "tier", string.IsNullOrWhiteSpace(tier) ? "unknown" : tier.Trim() },
            { "outcome", string.IsNullOrWhiteSpace(outcome) ? "unknown" : outcome.Trim() }
        };

        BillingCheckoutsTotal.Add(1, tags);
    }

    /// <summary>Adds <paramref name="estimatedCostUsd" /> to <see cref="LlmCostUsdTotal" /> when positive.</summary>
    public static void RecordLlmCostUsd(decimal estimatedCostUsd, string? tenantLabel)
    {
        if (estimatedCostUsd <= 0m)
            return;


        string tenant = string.IsNullOrWhiteSpace(tenantLabel) ? "unknown" : tenantLabel.Trim();
        TagList tags = new() { { "tenant", tenant } };

        LlmCostUsdTotal.Add((double)estimatedCostUsd, tags);
    }

    /// <summary>
    ///     Records LLM token counters. When <paramref name="recordPerTenant" /> is true, also emits tagged series with
    ///     <c>tenant_id</c> (increases Prometheus cardinality — use only for bounded tenant counts).
    ///     Optional <paramref name="llmProviderId" /> and <paramref name="llmDeploymentLabel" /> add low-cardinality series
    ///     for FinOps dashboards.
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

        if (promptTokens > 0)
            if (hasTags)
                LlmPromptTokensTotal.Add(promptTokens, BuildTags());
            else
                LlmPromptTokensTotal.Add(promptTokens);

        if (completionTokens <= 0)
            return;

        if (hasTags)
            LlmCompletionTokensTotal.Add(completionTokens, BuildTags());
        else
            LlmCompletionTokensTotal.Add(completionTokens);

        return;

        TagList BuildTags()
        {
            TagList tags = [];

            if (recordPerTenant && !string.IsNullOrEmpty(tenantIdNormalized))
                tags.Add("tenant_id", tenantIdNormalized);

            if (!string.IsNullOrEmpty(llmProviderId))

                tags.Add("llm_provider", llmProviderId);

            if (!string.IsNullOrEmpty(llmDeploymentLabel))
                tags.Add("llm_deployment", llmDeploymentLabel);

            return tags;
        }
    }

    private readonly struct LlmCallsPerRunAccumulationScope : IDisposable
    {
        public void Dispose()
        {
            LlmCallsPerRunAccumulator.Value = null;
        }
    }
}
