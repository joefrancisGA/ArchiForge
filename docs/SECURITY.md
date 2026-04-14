# Security overview (ArchLucid)

This document points to security-relevant behavior and gates. It is not a full threat model; see ADRs and runbooks for depth.

## Dynamic scanning (OWASP ZAP)

The **OWASP ZAP baseline** scan runs against the **ArchLucid API Docker image** in CI (`.github/workflows/ci.yml`, job `security-zap-api-baseline`) and on a **weekly schedule** (`.github/workflows/zap-baseline-strict-scheduled.yml`). Both use `zap-baseline.py` **without** `-I`, so **warnings and failures from the scan fail the workflow** (merge gate in CI; regression catch on the schedule).

- **Configuration:** `infra/zap/baseline-pr.tsv` (mounted into the scanner container as `config/baseline-pr.tsv`).
- **Triage and rule maintenance:** [docs/security/ZAP_BASELINE_RULES.md](security/ZAP_BASELINE_RULES.md).
- **Operational layout:** [infra/zap/README.md](../infra/zap/README.md).

Other layers (authentication, RLS, rate limiting, CORS, security headers) are described in `docs/DEPLOYMENT.md`, `docs/security/MULTI_TENANT_RLS.md`, and product code under `ArchLucid.Api` / `ArchLucid.Host.Core`.

- **API key rotation (comma-separated overlap):** [docs/runbooks/API_KEY_ROTATION.md](runbooks/API_KEY_ROTATION.md)
- **RLS residual risk acceptance (template):** [docs/security/RLS_RISK_ACCEPTANCE.md](security/RLS_RISK_ACCEPTANCE.md)

**System-wide STRIDE summary (product boundary):** [docs/security/SYSTEM_THREAT_MODEL.md](security/SYSTEM_THREAT_MODEL.md).

## DevelopmentBypass production guard

`ArchLucidAuth:Mode=DevelopmentBypass` is for **local and non-production** integration only. The API calls **`AuthSafetyGuard.GuardDevelopmentBypassInProduction`** during startup **before** authentication services are registered. It throws **`InvalidOperationException`** when the effective auth mode is DevelopmentBypass **and** any of the following is true:

- **`IHostEnvironment`** is Production (`ASPNETCORE_ENVIRONMENT=Production`), or
- **`ARCHLUCID_ENVIRONMENT`** is set to **`Production`** (supports hosts that intentionally keep ASP.NET in a non-Production name but are operationally production).

This is in addition to **`ArchLucidConfigurationRules.CollectErrors`**, which still surfaces the same misconfiguration in logs when validation runs after the host is built. Use **`JwtBearer`** or **`ApiKey`** in real production environments.

## Role-based access control (RBAC)

JWT **`roles`** / **`ClaimTypes.Role`** and DevelopmentBypass **`ArchLucidAuth:DevRole`** use the names in **`ArchLucid.Core.Authorization.ArchLucidRoles`**. Authorization policies are registered in **`ArchLucid.Host.Core.Startup.ArchLucidAuthorizationPoliciesExtensions.AddArchLucidAuthorizationPolicies`** and referenced from controllers via **`ArchLucid.Core.Authorization.ArchLucidPolicies`**.

| Role (`ArchLucidRoles`) | Claim value | Typical access |
|-------------------------|-------------|----------------|
| **ReadOnly** / **Reader** | `Reader` | Read runs, manifests, governance reads, audit list/search, provenance, retrieval (policy **`ReadAuthority`** / **`RequireReadOnly`**). |
| **Operator** | `Operator` | ReadOnly capabilities plus create runs, replay, compare, exports that are not admin-only, alert mutations (**`ExecuteAuthority`** / **`RequireOperator`**). |
| **Admin** | `Admin` | Operator capabilities plus policy packs, advisory schedules, system configuration surfaces protected with **`AdminAuthority`** / **`RequireAdmin`**. |
| **Auditor** | `Auditor` | Read-only scope plus **`GET /v1/audit/export`** and other endpoints that require **`RequireAuditor`** (Auditor or Admin role). |

Fine-grained **`permission`** claims (for example **`commit:run`**, **`export:consulting-docx`**) are still issued by **`ArchLucidRoleClaimsTransformation`** so existing permission policies remain meaningful for JWT and DevelopmentBypass. **ApiKey** mode maps keys to **Admin** or **Reader** roles only; use JWT with an **Auditor** app role when audit export is required for a principal.

## Log injection (CWE-117)

ArchLucid uses **Serilog** with **structured logging**: message templates use named placeholders (`{RunId}`, `{Path}`, etc.), and sinks such as JSON formatters emit parameters as **separate fields**. That layout reduces the impact of delimiter injection in **JSON** and similar structured sinks.

**Plaintext sinks** (console, rolling file text, etc.) can still be abused if a logged **string** contains newlines or other control characters—an attacker can forge extra log lines or break parsers. For any **`string`-typed value that originates from user input** (request body, URL path, query string, header), pass it through **`LogSanitizer.Sanitize()`** from **`ArchLucid.Core.Diagnostics`** before it is passed to **`ILogger`** as a structured parameter.

**Value types from routing are safe:** **`Guid`**, **`int`**, **`DateTime`**, and similar types bound from **`[FromRoute]`** do not need sanitization—their string representation in logs cannot introduce C0/C1 control characters in the way arbitrary HTTP strings can. If a route parameter is bound as **`string`** (even when it holds a UUID), static analysis may still treat it as untrusted input; use **`LogSanitizer`**, refactor to a value type + **`{param:guid}`**, or follow dismissal guidance in **`docs/CODEQL_TRIAGE.md`**.

**When adding new `ILogger` calls in controllers:** apply **`LogSanitizer.Sanitize()`** to any **`string`** parameter sourced from HTTP input. **`Guid`**, **`int`**, and other non-string value types from route or validated models are inherently safe for this class of issue.
