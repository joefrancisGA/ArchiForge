> **Scope:** Operators and pilot evaluators stuck during the Core Pilot; symptom-first triage with links to canonical runbooks—not incident response, security coordination, or a full RCA guide.

# Pilot rescue playbook (V1)

Use when you need **symptom → likely cause → first command → deeper doc**. Full flow: **[TROUBLESHOOTING.md](../TROUBLESHOOTING.md)** and **[CORE_PILOT.md](../CORE_PILOT.md)**.

**Correlation:** Include **`X-Correlation-ID`** (and `correlationId` inside ProblemDetails JSON) in notes whenever you open a thread, grep logs, or attach diagnostics.

**Support bundle:** From a machine with CLI access to the tenant, run **`archlucid support-bundle --zip`** (or your deployment’s equivalent). Open **`README.txt`** then **`next-steps.json`**; use **`references.json`** for doc paths from repo root. Inspect contents before external send—see **support bundle** row below and [TROUBLESHOOTING.md](../TROUBLESHOOTING.md) (support bundle / redaction).

| Symptom | Likely cause | First command / check | Next doc |
| --- | --- | --- | --- |
| API unreachable / connection refused | Host down, wrong URL/port, network | `dotnet run --project ArchLucid.Cli -- doctor`; confirm `ARCHLUCID_API_URL` / TLS | [COMMON_ERRORS.md](COMMON_ERRORS.md), [TROUBLESHOOTING.md](../TROUBLESHOOTING.md) |
| **`/health/ready`** unhealthy | Missing dependency (SQL, Bus, key vault), migration, config | Read JSON `entries[]` for first **Unhealthy** / **Degraded** (dependency name) | [TROUBLESHOOTING.md](../TROUBLESHOOTING.md) (opening readiness steps) |
| **401** | Missing/invalid API key or JWT | Set `ARCHLUCID_API_KEY` or Entra bearer per environment docs | [API_KEY_ROTATION.md](../library/API_KEY_ROTATION.md), [TROUBLESHOOTING.md](../TROUBLESHOOTING.md) |
| **403** | Role or scope (tenant/workspace/project) mismatch | Confirm identity, **`x-tenant-id`**, workspace/project headers | [OPERATOR_ATLAS.md](../library/OPERATOR_ATLAS.md) |
| **402** | Trial / seat entitlement exhausted | Read response `detail` and `supportHint`—no entitlement bypass in production | [TRIAL_AND_SIGNUP.md](../go-to-market/TRIAL_AND_SIGNUP.md) |
| Run not **ReadyToCommit** / pipeline stuck | Prior stage incomplete, execute failure, data issue | `archlucid status <runId>`; run detail pipeline timeline | [CORE_PILOT.md](../CORE_PILOT.md) (steps 2–3) |
| **Commit** blocked (governance) | Policy gate requires fixes or documented override | Capture gate message + findings snapshot id | [PRE_COMMIT_GOVERNANCE_GATE.md](../library/PRE_COMMIT_GOVERNANCE_GATE.md) |
| No artifacts after commit | Commit not 2xx, async worker lag, wrong run scope | Re-check commit response; refresh run; search logs by correlation id | [CORE_PILOT.md](../CORE_PILOT.md) (step 4), [API_CONTRACTS.md](../library/API_CONTRACTS.md) |
| Real-mode / **Azure OpenAI** failures | Quota, deployment name, circuit breaker, auth to AOAI | **`GET /health`** → `circuit_breakers`; verify `ARCHLUCID_REAL_AOAI` / deployment settings (non-secret names only in notes) | [FIRST_REAL_VALUE.md](../library/FIRST_REAL_VALUE.md), [RESILIENCE_CONFIGURATION.md](../library/RESILIENCE_CONFIGURATION.md), [AGENT_OUTPUT_EVALUATION.md](../library/AGENT_OUTPUT_EVALUATION.md) |
| Support ZIP before external | Residual secrets or unintended tenant/contact data in attached files | Shipped bundle redacts bearer tokens/API keys/password-shaped lines automatically. **Resolved 2026-05-03** — [item 37(c)](../PENDING_QUESTIONS.md): manually review **before external send**; include tenant-identifying or contact **PII** only when downloader holds **ExecuteAuthority** and **explicitly intends** disclosure | [TROUBLESHOOTING.md](../TROUBLESHOOTING.md) (support bundle) |

**Escalation:** **[PILOT_GUIDE.md](../library/PILOT_GUIDE.md)** (contacts and reporting).

**Unsafe shortcuts:** Do **not** enable **`DevelopmentBypass`** or **RLS bypass** outside documented break-glass—[COMMON_ERRORS.md](COMMON_ERRORS.md) (DevelopmentBypass and tenant/RLS mismatch sections).
