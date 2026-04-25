> **Scope:** Evaluators who want the shipped `archlucid try` Docker stack to call their Azure OpenAI instead of the simulator; not production deployment architecture, cost governance beyond the noted token default, or ADR-level rationale (see the linked ADR).

# First real value (`archlucid try --real`)

**Audience:** Evaluators who want the same **demo stack** as `archlucid try`, but with **Azure OpenAI** completing agents instead of the deterministic simulator.

## What you need

1. **Shell gate (opt-in):** set **`ARCHLUCID_REAL_AOAI=1`** in the environment where you run the CLI. Without this, `--real` is ignored for safety (no surprise spend against a subscription you did not intend).
2. **Azure OpenAI credentials** in the environment (validated before compose):
   - `AZURE_OPENAI_ENDPOINT`
   - `AZURE_OPENAI_API_KEY`
   - `AZURE_OPENAI_DEPLOYMENT_NAME`
3. **Optional cost cap:** `AZURE_OPENAI_MAX_COMPLETION_TOKENS` (defaults to **1024** in the `docker-compose.real-aoai.yml` overlay when unset).

## What the CLI does

- Runs **`docker compose`** with **`docker-compose.demo.yml`** plus **`docker-compose.real-aoai.yml`**, which sets `AgentExecution__Mode=Real` and maps the Azure OpenAI settings into the API container.
- Sends **`X-ArchLucid-Pilot-Try-Real-Mode: 1`** on execute so the host can emit **`FirstRealValueRun*`** audit events and real-mode telemetry counters.
- If execute fails in real mode and you did **not** pass **`--strict-real`**, the CLI falls back to **`seed-fake-results`** with `pilotTryRealModeFellBack=true`, marks the run, and the first-value Markdown gains a **warning callout** plus an **Execution provenance** footer (see [`ExecutionProvenanceFooterRenderer`](../../ArchLucid.Application/Pilots/ExecutionProvenanceFooterRenderer.cs)).
- **`--strict-real`** is for smoke jobs that must **fail** instead of substituting simulator output (for example CI that is not allowed to mask AOAI outages).

## Operator triage

See **[`docs/runbooks/AGENT_EXECUTION_FAILURES.md`](../runbooks/AGENT_EXECUTION_FAILURES.md)** — real-mode and fallback behaviour are documented there.

## Architecture decision

ADR **[`docs/adr/0033-first-real-value-single-env-var-flip.md`](../adr/0033-first-real-value-single-env-var-flip.md)**.
