> **Scope:** LLM prompt deny-list redaction (P2) - full detail, tables, and links in the sections below.

# LLM prompt deny-list redaction (P2)

**Last reviewed:** 2026-04-20

## When to use this runbook

- You need to **disable** outbound prompt redaction for a short diagnostic window (expect **`archlucid_llm_prompt_redaction_skipped_total`** to rise).
- You are validating that **production-like** hosts warn on **`LlmPromptRedaction:Enabled=false`** (`LlmPromptRedactionProductionWarningPostConfigure`).
- Forensics found **PII-shaped** tokens still reaching Azure OpenAI — tune deny-list rules in code (`PromptRedactor`) and ship a follow-up migration only if storage shape changes.

## Configuration

| Key | Meaning |
|-----|--------|
| **`LlmPromptRedaction:Enabled`** | When **true**, system and user prompt strings are passed through **`IPromptRedactor`** before **`LlmCompletionAccountingClient`** calls the model, and trace/blob persistence paths in **`AgentExecutionTraceRecorder`** use the same redactor for stored text. |
| **`LlmPromptRedaction:ReplacementToken`** | String substituted for matches (default **`[REDACTED]`**). |

Defaults ship **`Enabled=true`** in [`ArchLucid.Api/appsettings.json`](../../ArchLucid.Api/appsettings.json) and [`ArchLucid.Api/appsettings.Production.json`](../../ArchLucid.Api/appsettings.Production.json).

## Observability

| Metric | Meaning |
|--------|--------|
| **`archlucid_llm_prompt_redactions_total`** | Counter with label **`category`** (`email`, `ssn`, `credit_card`, `jwt`, `api_key`, …) — increments by match count on the **accounting client** path (avoids double-counting with trace-only redaction). |
| **`archlucid_llm_prompt_redaction_skipped_total`** | Completions observed while redaction is **disabled**. |

See [`docs/OBSERVABILITY.md`](../OBSERVABILITY.md) and [`ArchLucid.Core/Diagnostics/ArchLucidInstrumentation.cs`](../../ArchLucid.Core/Diagnostics/ArchLucidInstrumentation.cs).

## Security notes

- Redaction is a **first-line defense**, not a guarantee: novel secret formats can slip through; combine with **least-privilege** prompts, **Key Vault**, and **no secrets in user-editable briefs**.
- **Regex timeouts** are bounded; timeouts log a warning and skip that category for the remainder of the string pass.

## Related

- [`docs/AGENT_TRACE_FORENSICS.md`](../AGENT_TRACE_FORENSICS.md) — where full prompts land in blob/SQL.
- [`docs/security/SYSTEM_THREAT_MODEL.md`](../security/SYSTEM_THREAT_MODEL.md) — API → LLM boundary.
