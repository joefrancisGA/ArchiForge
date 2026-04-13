# Security overview (ArchLucid)

This document points to security-relevant behavior and gates. It is not a full threat model; see ADRs and runbooks for depth.

## Dynamic scanning (OWASP ZAP)

The **OWASP ZAP baseline** scan runs against the **ArchLucid API Docker image** in CI (`.github/workflows/ci.yml`, job `security-zap-api-baseline`) and on a **weekly schedule** (`.github/workflows/zap-baseline-strict-scheduled.yml`). Both use `zap-baseline.py` **without** `-I`, so **warnings and failures from the scan fail the workflow** (merge gate in CI; regression catch on the schedule).

- **Configuration:** `infra/zap/baseline-pr.tsv` (mounted into the scanner container as `config/baseline-pr.tsv`).
- **Triage and rule maintenance:** [docs/security/ZAP_BASELINE_RULES.md](security/ZAP_BASELINE_RULES.md).
- **Operational layout:** [infra/zap/README.md](../infra/zap/README.md).

Other layers (authentication, RLS, rate limiting, CORS, security headers) are described in `docs/DEPLOYMENT.md`, `docs/security/MULTI_TENANT_RLS.md`, and product code under `ArchLucid.Api` / `ArchLucid.Host.Core`.

## Log injection (CWE-117)

ArchLucid uses **Serilog** with **structured logging**: message templates use named placeholders (`{RunId}`, `{Path}`, etc.), and sinks such as JSON formatters emit parameters as **separate fields**. That layout reduces the impact of delimiter injection in **JSON** and similar structured sinks.

**Plaintext sinks** (console, rolling file text, etc.) can still be abused if a logged **string** contains newlines or other control characters—an attacker can forge extra log lines or break parsers. For any **`string`-typed value that originates from user input** (request body, URL path, query string, header), pass it through **`LogSanitizer.Sanitize()`** from **`ArchLucid.Core.Diagnostics`** before it is passed to **`ILogger`** as a structured parameter.

**Value types from routing are safe:** **`Guid`**, **`int`**, **`DateTime`**, and similar types bound from **`[FromRoute]`** do not need sanitization—their string representation in logs cannot introduce C0/C1 control characters in the way arbitrary HTTP strings can. If a route parameter is bound as **`string`** (even when it holds a UUID), static analysis may still treat it as untrusted input; use **`LogSanitizer`**, refactor to a value type + **`{param:guid}`**, or follow dismissal guidance in **`docs/CODEQL_TRIAGE.md`**.

**When adding new `ILogger` calls in controllers:** apply **`LogSanitizer.Sanitize()`** to any **`string`** parameter sourced from HTTP input. **`Guid`**, **`int`**, and other non-string value types from route or validated models are inherently safe for this class of issue.
