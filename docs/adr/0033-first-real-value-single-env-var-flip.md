> **Scope:** Maintainers recording why local `archlucid try --real` is opt-in, key-preflighted, and fallible-to-simulator; not hosted SaaS try-real, managed identity for the dev loop, or non-CLI execution paths.

# ADR 0033 — First real value: single env-var flip for `archlucid try --real`

## Status

Accepted (2026-04-24)

## Context

The **`archlucid try`** path gives a committed manifest and sponsor-grade Markdown in about a minute using the **simulator**. Buyers still could not **self-prove** value against **their own** Azure OpenAI (AOAI) deployment without operator assistance.

## Decision

Ship **`archlucid try --real`** as an **opt-in** local path:

1. **Gate:** `ARCHLUCID_REAL_AOAI=1` must be set in the shell alongside `--real`. This is the feature switch (no separate `Demo:Enabled`-style flag for this path).
2. **Preflight:** the CLI validates **`AZURE_OPENAI_ENDPOINT`**, **`AZURE_OPENAI_API_KEY`**, and **`AZURE_OPENAI_DEPLOYMENT_NAME`** before applying the compose overlay.
3. **Compose:** additive overlay **`docker-compose.real-aoai.yml`** sets **`AgentExecution:Mode=Real`**, maps AOAI configuration, and caps **`AzureOpenAI:MaxCompletionTokens`** (default **1024**, overridable via **`AZURE_OPENAI_MAX_COMPLETION_TOKENS`**).
4. **Fallback:** on AOAI failure, default behaviour is **simulator substitution** with a visible Markdown warning; **`--strict-real`** fails loud (for CI smoke that must not mask outages).
5. **Provenance:** first-value reports append an **Execution provenance** footer (mode, trace count, deployment when known).
6. **Audit + telemetry:** **`FirstRealValueRunStarted`**, **`FirstRealValueRunCompleted`**, **`FirstRealValueRunFellBackToSimulator`** plus OTel counters on the pilot path.

## Alternatives considered

- **Hosted SaaS “try real”:** rejected for v1 — this ADR is explicitly the **local CLI + Docker** path only.
- **Managed identity for local AOAI:** deferred — v1 uses **key-based** configuration only for the developer loop.
- **Always-on real mode for `try`:** rejected — surprise cost and flaky laptops; opt-in gate + preflight reduce support burden.

## Consequences

- **Security:** secrets pass through compose environment into a **local** API container; operators must treat dev machines like workstations handling production keys (short-lived keys, no screen sharing of `.env`).
- **Cost:** default **1024** max completion tokens limits worst-case bill per task on small pilots.
- **Reliability:** fallback preserves the demo narrative when AOAI is misconfigured or rate-limited; **`--strict-real`** preserves a hard failure mode for automation.

## References

- [`docs/library/FIRST_REAL_VALUE.md`](../library/FIRST_REAL_VALUE.md)
- [`docs/library/CLI_USAGE.md`](../library/CLI_USAGE.md) — `try` row
- [`docs/runbooks/AGENT_EXECUTION_FAILURES.md`](../runbooks/AGENT_EXECUTION_FAILURES.md)
