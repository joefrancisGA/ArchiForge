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

## Related documents

- [BACKGROUND_JOB_CORRELATION.md](BACKGROUND_JOB_CORRELATION.md) — background jobs + authority stage hierarchy.
- [TEST_EXECUTION_MODEL.md](TEST_EXECUTION_MODEL.md) — `Suite=Core` and observability-related tests.
- `ArchLucid.Host.Core/Startup/ObservabilityExtensions.cs` — host wiring.
- `ArchLucid.Host.Core/Startup/ObservabilityTraceSamplingConfigurator.cs` — trace sampling from configuration.
