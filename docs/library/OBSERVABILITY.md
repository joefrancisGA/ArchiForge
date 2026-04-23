> **Scope:** Observability — metrics and tracing (ArchLucid) - full detail, tables, and links in the sections below.

> **Spine doc:** [Five-document onboarding spine](../FIRST_5_DOCS.md). Read this file only if you have a specific reason beyond those five entry documents.


# Observability — metrics and tracing (ArchLucid)

**Audience:** SRE, platform engineers, and developers wiring Prometheus/Grafana, Application Insights, or OTLP exporters.

**Scope:** This doc lists **stable** custom instrumentation names owned in **`ArchLucid.Core.Diagnostics.ArchLucidInstrumentation`**. It is not an exhaustive inventory of ASP.NET Core, HTTP client, or SQL client auto-instrumentation.

---

## Meter

| Name | Version | Registration |
|------|---------|----------------|
| **`ArchLucid`** | `1.0.0` | `AddMeter(ArchLucidInstrumentation.MeterName)` in `ObservabilityExtensions.AddArchLucidOpenTelemetry` |

---

## Histograms and counters (selected)

| Instrument | Type | Unit | Labels / notes |
|------------|------|------|----------------|
| **`archlucid_authority_pipeline_stage_duration_ms`** | Histogram | `ms` | **`stage`**: `context_ingestion`, `graph`, `findings`, `decisioning`, `artifacts`. **`outcome`**: `success`, `error`. Wall time per stage in **`AuthorityPipelineStagesExecutor`**. |
| **`archlucid_authority_runs_completed_total`** | Counter | — | Authority runs completed through finalization. |
| **`archlucid_authority_pipeline_work_pending`** | Observable gauge | — | Outbox depth (see `EnsureOutboxDepthObservableGaugesRegistered`). |
| **`alert_evaluation_duration_ms`** | Histogram | `ms` | Alert evaluation. |
| **`governance_resolve_duration_ms`** | Histogram | `ms` | Effective governance resolution. |
| **`archlucid_explainability_trace_completeness_ratio`** | Histogram | — | Advisory scan trace completeness. |
| **`archlucid_explanation_faithfulness_ratio`** | Histogram | — | Heuristic overlap of aggregate explanation tokens vs finding **`ExplainabilityTrace`** text (**`ExplanationFaithfulnessChecker`** on **`RunExplanationSummaryService`**). |
| **`archlucid_circuit_breaker_*`** | Counter | — | State transitions, rejections, probe outcomes. |
| **`archlucid_llm_*`** | Counter | — | Token usage, retries (see `ArchLucidInstrumentation` source). |
| **`archlucid_llm_prompt_redactions_total`** | Counter | **`category`** | Deny-list replacements applied on the **LLM accounting** path before the model call. |
| **`archlucid_llm_prompt_redaction_skipped_total`** | Counter | — | Model calls observed while **`LlmPromptRedaction:Enabled`** is **false** (audit deliberate bypass). |
| **`archlucid_first_session_completed_total`** | Counter | — | Once per tenant on first successful golden-manifest commit (Core Pilot funnel; SQL **`TenantOnboardingState`**). |
| **`archlucid_operator_task_success_total`** | Counter | `task` (`first_run_committed` \| `first_session_completed`) | Server-side onboarding milestones: first golden-manifest commit per tenant (`SqlFirstSessionLifecycleHook`) and successful self-service registration completion (`RegistrationController` **201**). Process-life in default Prometheus scrape; operator UI tile reads **`GET /v1/diagnostics/operator-task-success-rates`** (in-process listener snapshot — resets on API host restart). |
| **`archlucid_agent_output_structural_completeness_ratio`** | Histogram | — | **`agent_type`**: Topology, Cost, Compliance, Critic. Fraction of expected **`AgentResult`** JSON keys present on **`ParsedResultJson`** (see **`AgentOutputEvaluationRecorder`**). |
| **`archlucid_agent_output_parse_failures_total`** | Counter | — | **`agent_type`**. **`ParsedResultJson`** is not a JSON object or failed JSON parse when re-checked for metrics. |
| **`archlucid_agent_trace_blob_upload_failures_total`** | Counter | — | **`agent_type`**, **`blob_type`** (`system_prompt`, `user_prompt`, `response`). Incremented when a blob write exhausts all retry attempts. |
| **`archlucid_agent_trace_prompt_inline_fallback_total`** | Counter | — | **`agent_type`**, **`blob_type`**. **Real** execution: full text written to SQL **`Full*Inline`** when the blob key for that part is missing (see **`docs/AGENT_TRACE_FORENSICS.md`**). |
| **`archlucid_agent_trace_blob_persist_duration_ms`** | Histogram | — | **`agent_type`**. Wall-clock time for awaited full-prompt/blob persistence after trace row insert (includes retries; see **`AgentExecutionTraceRecorder`** and **`docs/AGENT_TRACE_FORENSICS.md`**). |
| **`archlucid_agent_output_semantic_score`** | Histogram | — | **`agent_type`**. Semantic quality score (0.0–1.0) evaluating claim evidence and finding completeness in agent output JSON. |
| **`archlucid_agent_output_quality_gate_total`** | Counter | — | **`agent_type`**, **`outcome`** (`accepted` / `warned` / `rejected`). Emitted when **`ArchLucid:AgentOutput:QualityGate:Enabled`** is **true** (see **`AgentOutputEvaluationRecorder`**). |
| **`archlucid_explanation_aggregate_faithfulness_fallback_total`** | Counter | — | Aggregate **`GET …/explain/runs/{runId}/aggregate`** replaced LLM narrative with deterministic manifest text after low faithfulness vs findings. |
| **`archlucid_data_consistency_orphans_detected_total`** | Counter | **`table`**, **`column`** | Rows counted by **`DataConsistencyOrphanProbeHostedService`** when **`dbo.ComparisonRecords`** (**`LeftRunId`** / **`RightRunId`**), **`dbo.GoldenManifests`**, or **`dbo.FindingsSnapshots`** reference a **`RunId`** missing from **`dbo.Runs`** (detection-only). |
| **`archlucid_data_consistency_alerts_total`** | Counter | **`table`**, **`column`** | Incremented when **`DataConsistency:Enforcement:Mode`** is **`Alert`** or **`Quarantine`** and orphan counts meet **`AlertThreshold`** for that slice. |
| **`archlucid_explanation_citations_emitted_total`** | Counter | **`kind`** (`CitationKind` string) | Citation references attached to **`GET /v1/explain/runs/{runId}/aggregate`** for UI chips. |

For the full set, read **`ArchLucid.Core/Diagnostics/ArchLucidInstrumentation.cs`**.

---

## Business-Level KPI Metrics

These instruments support **product and operator dashboards** (runs volume, findings mix, LLM batch intensity, explanation cache effectiveness). They use the same **`ArchLucid`** meter as operational metrics.

| Instrument | Type | Labels | What it measures | Suggested Grafana panel |
|------------|------|--------|------------------|-------------------------|
| **`archlucid_runs_created_total`** | Counter | — | Authority **`RunRecord`** rows inserted at orchestration start (pre-pipeline), including runs that later queue deferred work. | **Time series** — `rate()` or `increase()` over a window (e.g. runs/min). |
| **`archlucid_authority_pipeline_timeouts_total`** | Counter | — | Authority pipeline (sync or queued completion) cancelled because **`AuthorityPipeline:PipelineTimeout`** elapsed before commit. | **Time series** — alert if **`rate() > 0`** sustained; tune timeout vs. workload. |
| **`archlucid_findings_produced_total`** | Counter | **`severity`** (`FindingSeverity` enum name: `Info`, `Warning`, `Error`, `Critical`) | Findings persisted with the findings snapshot after the authority **findings** stage completes (one increment batch per severity bucket per run). | **Time series** — stacked or separate lines per `severity`, or **bar gauge** for share in window. |
| **`archlucid_llm_calls_per_run`** | Histogram (`int`, unit `{call}`) | — | Count of successful Azure OpenAI JSON completions during one **`RealAgentExecutor.ExecuteAsync`** batch (parallel handlers share one observation). | **Heatmap** (histogram over time) or **percentiles** via `histogram_quantile`; optional **stat** for last value. |
| **`archlucid_explanation_cache_hits_total`** | Counter | — | Aggregate explanation summary served from **`IHotPathReadCache`** without invoking the inner **`RunExplanationSummaryService`** factory. | **Time series** — `rate()` alongside misses. |
| **`archlucid_explanation_cache_misses_total`** | Counter | — | Cache factory invoked (inner summary built; may imply LLM work). | **Time series** — `rate()` alongside hits. |
| **`archlucid_agent_output_structural_completeness_ratio`** | Histogram | **`agent_type`** | Distribution of structural completeness for persisted agent **`ParsedResultJson`** (0.0–1.0). | **Heatmap** or **quantiles**; alert if p10 drops after a prompt or model change. |
| **`archlucid_agent_output_parse_failures_total`** | Counter | **`agent_type`** | JSON parse / root-kind failures when scoring trace payloads for metrics. | **Time series** — `rate()`; correlate with **`archlucid_agent_result_schema_validations_total`** (invalid). |
| **`archlucid_agent_trace_blob_upload_failures_total`** | Counter | **`agent_type`**, **`blob_type`** | Blob writes that exhausted all retry attempts for agent trace full-prompt persistence. | **Time series** — `rate()`; alert on sustained > 0. |
| **`archlucid_agent_trace_prompt_inline_fallback_total`** | Counter | **`agent_type`**, **`blob_type`** | Inline SQL fallback after blob miss (Real mode). | **Time series** — `rate()`; correlate with blob failures. |
| **`archlucid_agent_trace_blob_persist_duration_ms`** | Histogram | **`agent_type`** | End-to-end blob persistence latency after trace insert (timeout-bounded). | **Heatmap** / **p95**; spike with flat availability → storage saturation or timeout tuning. |
| **`archlucid_agent_output_semantic_score`** | Histogram | **`agent_type`** | Semantic quality: claim evidence coverage + finding completeness (0.0–1.0). | **Heatmap** or **quantiles**; alert if p10 < 0.5 after prompt/model change. |
| **`archlucid_agent_output_quality_gate_total`** | Counter | **`agent_type`**, **`outcome`** | Optional gate after structural + semantic scoring (off unless **`ArchLucid:AgentOutput:QualityGate:Enabled`**). | **Time series** — `rate()` by outcome; investigate spikes in **`rejected`**. |
| **`archlucid_explanation_aggregate_faithfulness_fallback_total`** | Counter | — | Deterministic aggregate narrative substituted after low faithfulness vs findings. | **Time series** — correlate with model or prompt changes. |

### Explanation cache hit ratio (Prometheus)

Use a ratio of **hit rate** to **hit + miss** rates (avoid dividing raw counters):

```promql
rate(archlucid_explanation_cache_hits_total[5m])
/
(
  rate(archlucid_explanation_cache_hits_total[5m])
  + rate(archlucid_explanation_cache_misses_total[5m])
)
```

When the denominator is **zero** (no traffic), the result is undefined; dashboards may show gaps or you may wrap the denominator with **`clamp_min(..., 1e-9)`** for a defined 0–1 series.

A recording rule **`archlucid:explanation_cache_hit_ratio`** is defined in **`infra/prometheus/archlucid-slo-rules.yml`** for reuse in Grafana variables and alerts.

---

## Trial funnel (self-service product metrics)

**Purpose:** Quantify the self-service trial as a **funnel** aligned with durable audit types in `AuditEventTypes` (`TrialSignupAttempted`, `TrialSignupFailed`, `TrialFirstRunCompleted`, `BillingCheckoutInitiated`, `BillingCheckoutCompleted`). Operational detail: **`docs/runbooks/TRIAL_FUNNEL.md`**.

| Instrument | Type | Labels | Emitted when |
|------------|------|--------|----------------|
| **`archlucid_trial_signups_total`** | Counter | `source`, `mode` | Trial tenant successfully bootstrapped after registration (`TrialTenantBootstrapService`). |
| **`archlucid_trial_signup_failures_total`** | Counter | `stage`, `reason` | Duplicate slug, validation/provisioning failures, email policy block, or local identity errors. |
| **`archlucid_trial_first_run_seconds`** | Histogram | (histogram series) | First coordinator **commit** that persists a golden manifest for a trial tenant (`SqlTrialFunnelCommitHook`). |
| **`archlucid_trial_active_tenants`** | Observable gauge | — | Cached SQL count of active trials; updated from the operational metrics hosted service. |
| **`archlucid_trial_runs_used_ratio`** | Histogram | (histogram series) | Same hook as first-run latency: `TrialRunsUsed` / limit at first qualifying commit. |
| **`archlucid_trial_conversion_total`** | Counter | `from_state`, `to_tier` | Manual convert (`TenantTrialController`) or webhook activator path. |
| **`archlucid_trial_expirations_total`** | Counter | `reason` | **`TrialLifecycleTransitionEngine`** (worker) on lifecycle transitions. |
| **`archlucid_billing_checkouts_total`** | Counter | `provider`, `tier`, `outcome` | `BillingCheckoutController` validation/conflict/session/provider outcomes. |

**Dashboard:** `infra/grafana/dashboard-archlucid-trial-funnel.json` (Terraform `grafana_dashboard.trial_funnel`).  
**Alerts:** `infra/prometheus/archlucid-alerts.yml` group **`archlucid-trial-funnel`**.

---

## Activity sources (custom)

Registered via `tracing.AddSource(...)` in **`ObservabilityExtensions`** (including all names below):

| Source name | Typical use |
|-------------|-------------|
| **`ArchLucid.AuthorityRun`** | Authority run orchestration; **child** stage spans (`authority.*`) under the run span — see [BACKGROUND_JOB_CORRELATION.md](BACKGROUND_JOB_CORRELATION.md) §10. |
| **`ArchLucid.AdvisoryScan`** | Scheduled advisory scans. |
| **`ArchLucid.Retrieval.Index`** | Post-commit retrieval indexing. |
| **`ArchLucid.Agent.Handler`** | Production agent handler. |
| **`ArchLucid.Agent.LlmCompletion`** | LLM completion calls. |
| **`ArchLucid.RetrievalIndexing.Outbox`** | Retrieval indexing outbox processor. |
| **`ArchLucid.IntegrationEvent.Outbox`** | Integration event publish outbox. |
| **`ArchLucid.DataArchival`** | Data retention archival. |

---

## Trace tags (conventions)

- **`archlucid.run_id`** — run identifier on authority pipeline stages.
- **`archlucid.stage.name`** — low-cardinality stage key (`context_ingestion`, `graph`, …) for dashboards and queries.
- **`correlation.id`** — logical correlation (`ActivityCorrelation.LogicalCorrelationIdTag`); aligns with Serilog `CorrelationId` where pushed.
- **`error.type`** — exception type name on failed spans when recorded.

---

## Persisted trace IDs

**`dbo.Runs.OtelTraceId`** stores the **W3C trace ID** captured at **run creation** (from the active **`Activity`** when the authority run record is first persisted — see migration **052**). It is **not** overwritten on later updates, so it remains a stable handle for **creation-time** distributed tracing.

Operators can use it for **post-hoc trace lookup** in two ways:

- **Run detail UI** — the operator shell shows a **Creation trace** link when a persisted id exists (distinct from the per-request **`X-Trace-Id`** / **`traceparent`** on the current page load). Configure **`NEXT_PUBLIC_TRACE_VIEWER_URL_TEMPLATE`** in **`archlucid-ui`** (same **`{traceId}`** placeholder as below).
- **CLI** — **`archlucid trace <runId>`** fetches run detail from the API, reads **`run.otelTraceId`**, and prints a trace viewer URL when **`ARCHLUCID_TRACE_VIEWER_URL_TEMPLATE`** is set (optional browser open via **`ARCHLUCID_TRACE_OPEN_BROWSER`**). See **[CLI_USAGE.md](CLI_USAGE.md)**.

For request-scoped correlation headers and sampling, see **Sampling strategy** and **Response headers** below.

---

## Agent execution trace blob storage + SQL inline fallback

**`AgentExecutionTraceRecorder`** uploads **full** (unsanitized) system prompt, user prompt, and raw model response to **`IArtifactBlobStore`** (container **`agent-traces`**, paths **`{runId}/{traceId}/system-prompt.txt`**, **`user-prompt.txt`**, **`response.txt`**) after the trace row insert for **Real** execution, subject to **`AgentExecution:TraceStorage:BlobPersistenceTimeoutSeconds`**. **Simulator** traces skip blob/inline full-text persistence. Failed or timed-out parts are mirrored into SQL **`FullSystemPromptInline`**, **`FullUserPromptInline`**, **`FullResponseInline`** when blob keys are missing. Histograms/counters: **`archlucid_agent_trace_blob_persist_duration_ms`**, **`archlucid_agent_trace_blob_upload_failures_total`**, **`archlucid_agent_trace_prompt_inline_fallback_total`**. Operational and privacy notes: **`docs/AGENT_TRACE_FORENSICS.md`**.

Optional reference-case scoring (**`AgentExecution:ReferenceEvaluation:Enabled`** — **false** in shipped **`appsettings.json`**) emits **`archlucid_agent_output_reference_case_evaluations_total`** (labels **`case_id`**, **`agent_type`**, **`outcome`**) and **`archlucid_agent_output_reference_case_score_ratio`**; rows may be appended to **`dbo.AgentOutputEvaluationResults`** (migration **063**). With **`Enabled: false`**, those histograms/counters are not produced on the hot path.

---

## Sampling strategy

| Environment   | `SamplingRatio` | Rationale |
|---------------|-------------------|-----------|
| Development   | `1.0` (default)   | Full fidelity for debugging. |
| Staging       | `1.0`             | Full fidelity for pre-prod verification. |
| Production    | `0.1` – `0.25`    | Reduces trace volume ~75–90% while maintaining statistical coverage. |

**Configuration:**

```json
{
  "Observability": {
    "Tracing": {
      "SamplingRatio": 0.1
    }
  }
}
```

Optional **`Observability:Tracing:AlwaysSampleActivitySources`** (array of `ActivitySource` names, e.g. `ArchLucid.AuthorityRun`) is bound into **`ObservabilityHostOptions`** for operators and future use. The in-process OpenTelemetry .NET SDK does not yet supply **ActivitySource** name on **`SamplingParameters`** (see [open-telemetry/opentelemetry-dotnet#4752](https://github.com/open-telemetry/opentelemetry-dotnet/issues/4752)), so **per-source always-on sampling is not applied in the API/worker**. Use an OTLP **collector** with tail-sampling (or backend rules) to keep high-value sources at full fidelity in production.

**Head-based vs. tail-based**

The built-in sampler is **head-based** (decision at trace start). Some interesting traces (errors, slow requests) may therefore be dropped before export. For **tail-based** sampling (retain errors, latency outliers, etc.), place an OTLP collector with a tail-sampling processor between the app and the trace backend.

**Authority run traces** (`ArchLucid.AuthorityRun`) are high-value and relatively low-volume — prefer retaining them at **1.0** in production via **collector** rules or tail sampling, or via future in-process support once the SDK exposes source-aware sampling.

**Response headers**

Every API response includes **`traceparent`** (W3C) and **`X-Trace-Id`** headers **regardless of sampling**. The values reflect the current **`Activity`** context even when the trace is not exported, so operators can copy an ID into a trace backend (a sampled-out trace may appear as “not found” rather than a mismatched id).

The **operator UI** run detail page (and coordinator **Provenance** page) read **`X-Trace-Id`** from the API response and show a **View trace** deep link when **`NEXT_PUBLIC_TRACE_VIEWER_URL_TEMPLATE`** is set in **`archlucid-ui`** (see [OPERATOR_QUICKSTART.md](OPERATOR_QUICKSTART.md) §Operator UI).

Wiring: **`ObservabilityTraceSamplingConfigurator.ConfigureTraceSampling`** runs **before** `AddAspNetCoreInstrumentation` in **`ObservabilityExtensions.AddArchLucidOpenTelemetry`**. Malformed **`SamplingRatio`** strings are treated as **`1.0`** (full sampling) so configuration typos do not prevent the host from starting.

---

## Health JSON (detailed)

**`GET /health`** (authenticated; **ReadAuthority**; detailed response writer) includes a **`circuit_breakers`** check whose **`data.gates`** array lists each OpenAI breaker with **`name`**, **`state`** (`Closed` / `Open` / `HalfOpen`), **`consecutiveFailures`**, **`failureThreshold`**, **`breakDurationSeconds`**, and **`lastStateChangeUtc`** (ISO-8601 or **`never`**). That gives operators the same operational shape as metrics-backed triage **without requiring Prometheus** for thresholds, failure counts, or last transition time. Still use **`archlucid_circuit_breaker_*`** counters for trends and dashboards. **`/health/live`** and **`/health/ready`** omit this check (it has no readiness/liveness tags).

---

## Committed Grafana dashboards (`infra/grafana/`)

| File | Purpose |
|------|---------|
| `dashboard-archlucid-authority.json` | Authority pipeline spans and throughput. |
| `dashboard-archlucid-slo.json` | HTTP SLO / burn-rate style panels. |
| `dashboard-archlucid-llm-usage.json` | LLM token rates. |
| `dashboards/archlucid-container-apps-overview.json` | Container Apps overview. |
| **`dashboard-archlucid-run-lifecycle.json`** | Run-lifecycle / traceability: template variable **`runId`**, links to API audit search, authority stage histograms, circuit breaker rates — use with [runbooks/TRACE_A_RUN.md](../runbooks/TRACE_A_RUN.md). |
| **`dashboard-archlucid-trial-funnel.json`** | Self-service trial funnel (signups, failures, first-run latency, billing, conversion) — use with [runbooks/TRIAL_FUNNEL.md](../runbooks/TRIAL_FUNNEL.md). |

Import paths and Terraform wiring: [runbooks/SLO_PROMETHEUS_GRAFANA.md](../runbooks/SLO_PROMETHEUS_GRAFANA.md).

---

## Prometheus alerts (explainability)

**`infra/prometheus/archlucid-alerts.yml`** — group **`archlucid-explainability`**:

- **`ArchLucidExplanationFaithfulnessFallbackTrend`** — spikes in aggregate faithfulness fallbacks (deterministic narrative substitution).
- **`ArchLucidExplainabilityTraceCompletenessP10Low`** — 10th percentile of **`archlucid_explainability_trace_completeness_ratio`** below **0.35** over a sustained window (tune per environment; requires histogram buckets in Prometheus).

See [EXPLAINABILITY_TRACE_COVERAGE.md](EXPLAINABILITY_TRACE_COVERAGE.md).

**`infra/prometheus/archlucid-alerts.yml`** — group **`archlucid-trial-funnel`** (signup failure rate page, first-run p95 ticket): see [runbooks/TRIAL_FUNNEL.md](../runbooks/TRIAL_FUNNEL.md).

---

## Azure Logic Apps (optional)

When **`infra/terraform-logicapps/`** hosts are enabled, send **platform + workflow logs** and **site metrics** to a **Log Analytics workspace** by setting **`enable_logic_app_diagnostic_settings = true`** and **`logic_app_diagnostic_log_analytics_workspace_id`** to the workspace resource ID. Terraform creates one **`azurerm_monitor_diagnostic_setting`** per deployed **`azurerm_logic_app_standard`** site (`enabled_log` category group **`allLogs`**, **`enabled_metric`** **`AllMetrics`**). Retention and workspace-based **Application Insights** (if used) are controlled on the workspace / classic AI resource — not in this module.

If you prefer the Portal for a one-off host, you can still add **Diagnostic settings** manually; keep destinations on **private** analytics paths consistent with org policy.

**Correlations:** Logic App run IDs with Service Bus **message** `messageId` / body **`approvalRequestId`** for governance approvals (`com.archlucid.governance.approval.submitted` on the dedicated subscription from `infra/terraform-servicebus` when `enable_logic_app_governance_approval_subscription` is true), and with body **`providerDedupeKey`** / **`subscriptionId`** for Marketplace fulfillment (`com.archlucid.billing.marketplace.webhook.received.v1` when `enable_logic_app_marketplace_fulfillment_subscription` is true). See [runbooks/LOGIC_APPS_STANDARD.md](../runbooks/LOGIC_APPS_STANDARD.md) and [CURSOR_PROMPTS_LOGIC_APPS.md](../archive/quality/2026-04-23-doc-depth-reorg/CURSOR_PROMPTS_LOGIC_APPS.md).

---

## Related documents

- [PERFORMANCE.md](PERFORMANCE.md) — hot-path caching (including aggregate explanation summary TTL and invalidation).
- [BACKGROUND_JOB_CORRELATION.md](BACKGROUND_JOB_CORRELATION.md) — background jobs + authority stage hierarchy.
- [TEST_EXECUTION_MODEL.md](TEST_EXECUTION_MODEL.md) — `Suite=Core` and observability-related tests.
- `ArchLucid.Host.Core/Startup/ObservabilityExtensions.cs` — host wiring.
- `ArchLucid.Host.Core/Startup/ObservabilityTraceSamplingConfigurator.cs` — trace sampling from configuration.
