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
| **`archlucid_circuit_breaker_*`** | Counter | — | State transitions, rejections, probe outcomes. |
| **`archlucid_llm_*`** | Counter | — | Token usage, retries (see `ArchLucidInstrumentation` source). |

For the full set, read **`ArchLucid.Core/Diagnostics/ArchLucidInstrumentation.cs`**.

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

## Related documents

- [BACKGROUND_JOB_CORRELATION.md](BACKGROUND_JOB_CORRELATION.md) — background jobs + authority stage hierarchy.
- [TEST_EXECUTION_MODEL.md](TEST_EXECUTION_MODEL.md) — `Suite=Core` and observability-related tests.
- `ArchLucid.Host.Core/Startup/ObservabilityExtensions.cs` — host wiring.
