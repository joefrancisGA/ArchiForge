> **Scope:** Security overview (ArchLucid) - full detail, tables, and links in the sections below.

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

## OpenAPI-driven fuzzing (Schemathesis, PR + schedule)

Merge-blocking **Schemathesis light** runs on every PR after full .NET regression: [`.github/workflows/ci.yml`](../.github/workflows/ci.yml) job **`api-schemathesis-light`** builds the API image, starts the container, and runs **`--phases=examples`** against **`/openapi/v1.json`** with **`--checks=all`** (response schema and status conformance). Full fuzzing and stateful phases run weekly — see **[docs/API_FUZZ_TESTING.md](API_FUZZ_TESTING.md)**.

## Shipped auth defaults (`appsettings.json` / `appsettings.Development.json`)

- **`ArchLucid.Api/appsettings.json`** (all environments unless overridden): **`ArchLucidAuth:Mode`** is **`ApiKey`**, with **`Authentication:ApiKey:Enabled`** **`false`** and **`DevelopmentBypassAll`** **`false`**. In that combination the API uses the API key authentication scheme but **rejects unauthenticated requests** until operators set **`Authentication:ApiKey:Enabled=true`** and configure **`AdminKey`** / **`ReadOnlyKey`** (or supply equivalent environment variables / Key Vault). This is **fail-closed** for accidental deployments that only ship base JSON.
- **`ArchLucid.Api/appsettings.Development.json`** (merged when **`ASPNETCORE_ENVIRONMENT=Development`**, including CI and local **`dotnet run`**): sets **`ArchLucidAuth:Mode`** back to **`DevelopmentBypass`** for frictionless local and test factories. **`Authentication:ApiKey:DevelopmentBypassAll`** stays **`false`** so the “open API key path” bypass is not the default even in Development.
- **`appsettings.Production.json`** / **`appsettings.Staging.json`** continue to set **`JwtBearer`** with Entra-style placeholders; **`docker-compose.yml`** still sets **`ArchLucidAuth__Mode=DevelopmentBypass`** explicitly for the compose dev stack.

**Optional Entra-only production (regulated SaaS):** set **`ArchLucidAuth:RequireJwtBearerInProduction=true`**. When **`ASPNETCORE_ENVIRONMENT=Production`**, **`ArchLucidConfigurationRules`** then requires **`ArchLucidAuth:Mode=JwtBearer`** (API keys are rejected at startup). Default is **`false`** so pilots may keep **`ApiKey`** in production until they cut over to Entra.

## DevelopmentBypass production guard

`ArchLucidAuth:Mode=DevelopmentBypass` and **`Authentication:ApiKey:DevelopmentBypassAll=true`** are for **local and non-production** integration only. The API calls **`AuthSafetyGuard.GuardAllDevelopmentBypasses`** during startup **before** authentication services are registered. It throws **`InvalidOperationException`** when the host is treated as **production-like** — **`IHostEnvironment.IsProduction()`**, or **`ASPNETCORE_ENVIRONMENT`** / **`ARCHLUCID_ENVIRONMENT`** values whose trimmed name **contains `prod`** (case-insensitive), **except** names containing **`non-production`** or **`nonproduction`** (so stacks like “non-production” stay non-prod). This catches misnamed hosts such as **`PreProduction`**, **`production`** (lowercase), or **`staging-prod`**. When that production-like bar is met, the guard throws if **any** of the following is true:

- The effective auth mode is **DevelopmentBypass**, or
- **`Authentication:ApiKey:DevelopmentBypassAll`** is **true** (open API-key access; must stay off in production even when using **JwtBearer** or **ApiKey** mode).

**`AuthSafetyGuard.GuardDevelopmentBypassInProduction`** is a documented alias that delegates to **`GuardAllDevelopmentBypasses`** (same checks).

This is in addition to **`ArchLucidConfigurationRules.CollectErrors`**, which still surfaces the same misconfiguration in logs when validation runs after the host is built. Use **`JwtBearer`** or **`ApiKey`** in real production environments, with **`DevelopmentBypassAll=false`**.

## Role-based access control (RBAC)

JWT **`roles`** / **`ClaimTypes.Role`** and DevelopmentBypass **`ArchLucidAuth:DevRole`** use the names in **`ArchLucid.Core.Authorization.ArchLucidRoles`**. Authorization policies are registered in **`ArchLucid.Host.Core.Startup.ArchLucidAuthorizationPoliciesExtensions.AddArchLucidAuthorizationPolicies`** and referenced from controllers via **`ArchLucid.Core.Authorization.ArchLucidPolicies`**.

**Trial-tier auth:** optional **`Auth:Trial:Modes`** enables **Entra External ID (CIAM)** consumer sign-in and/or **local email/password** backed by SQL; minted trial JWTs still carry **`ArchLucidRoles`** so **`ReadAuthority`** / **`ExecuteAuthority`** behave the same as workforce Entra tokens. See **`docs/security/TRIAL_AUTH.md`** and **ADR 0015**.

| Role (`ArchLucidRoles`) | Claim value | Typical access |
|-------------------------|-------------|----------------|
| **ReadOnly** / **Reader** | `Reader` | Read runs, manifests, governance reads, audit list/search, provenance, retrieval (policy **`ReadAuthority`** / **`RequireReadOnly`**). |
| **Operator** | `Operator` | ReadOnly capabilities plus create runs, replay, compare, exports that are not admin-only, alert mutations (**`ExecuteAuthority`** / **`RequireOperator`**). |
| **Admin** | `Admin` | Operator capabilities plus policy packs, advisory schedules, system configuration surfaces protected with **`AdminAuthority`** / **`RequireAdmin`**. |
| **Auditor** | `Auditor` | Read-only scope plus **`GET /v1/audit/export`** and other endpoints that require **`RequireAuditor`** (Auditor or Admin role). |

Fine-grained **`permission`** claims (for example **`commit:run`**, **`export:consulting-docx`**) are still issued by **`ArchLucidRoleClaimsTransformation`** so existing permission policies remain meaningful for JWT and DevelopmentBypass. **ApiKey** mode maps keys to **Admin** or **Reader** roles only; use JWT with an **Auditor** app role when audit export is required for a principal.

## HTTP rate limiting (role-aware)

**`fixed`** and **`expensive`** ASP.NET rate-limit policies partition buckets by **resolved role segment + client IP** (`RateLimitingRolePartitionBuilder`). Base permit counts come from **`RateLimiting:FixedWindow:*`** and **`RateLimiting:Expensive:*`**; the shipped default for **`fixed`** is **60 requests per minute** per partition when **`RateLimiting:FixedWindow:PermitLimit`** is not overridden. Optional multipliers are in **`RateLimiting:RoleMultipliers`** (**`Admin`**, **`Operator`**, **`Reader`**, **`Anonymous`**), clamped in code to a safe range. **ApiKey** and JWT principals inherit the same **`IsInRole`** checks, so automation keys mapped to **Admin** receive a higher budget than anonymous traffic.

## LLM content safety (optional; fail-closed in production-like hosts)

**`ArchLucid:ContentSafety:Enabled`** toggles **`IContentSafetyGuard`** registration for **non-production-like** hosts (see **`ContentSafetyOptions`** / **`appsettings.Advanced.json`**). This path is **configuration-driven** (not a **`FeatureManagement`** flag today).

When the host is **Production**, **Staging**, or **`ARCHLUCID_ENVIRONMENT`** is **`Production`** / **`Staging`**, ArchLucid **always** registers **`AzureContentSafetyGuard`** and **startup validation** requires **`ArchLucid:ContentSafety:Endpoint`** and **`ArchLucid:ContentSafety:ApiKey`**. **`FailClosedOnSdkError`** is forced **true** in those environments so SDK/network failures block rather than allow traffic.

| State | Behavior |
|--------|----------|
| **Production-like host** | **`AzureContentSafetyGuard`** mandatory when SQL-backed agents run; missing **`Endpoint`**/**`ApiKey`** fails **`ArchLucidConfigurationRules`** at startup. |
| **Non-production-like, disabled** (default) | **`NullContentSafetyGuard`** — pass-through; no outbound calls, gated by **`ArchLucid:ContentSafety:AllowNullGuardInDevelopment`** (default **true**). |
| **Non-production-like, enabled** without **`Endpoint`** or **`ApiKey`** | **`ContentSafetyEnabledButUnconfiguredGuard`** — **throws** on **`CheckInputAsync`** / **`CheckOutputAsync`** (fail-fast misconfiguration). |
| **Non-production-like, enabled** with absolute **`Endpoint`** and **`ApiKey`** | **`AzureContentSafetyGuard`** — calls **Azure AI Content Safety** text analysis (four severity levels). **`BlockSeverityThreshold`** (default **4**) blocks when any category severity is **≥** threshold. |

**Product status:** **`Azure.AI.ContentSafety`** is wired in **`ArchLucid.AgentRuntime.Safety.AzureContentSafetyGuard`** and registered from **`ArchLucid.Host.Composition`**. Offline **prompt-injection** fixture shape is validated in CI via **`scripts/ci/eval_agent_quality.py --prompt-injection-only`**. See **`docs/AI_AGENT_PROMPT_REGRESSION.md`**.

## SQL RLS break-glass bypass

**`SqlRowLevelSecurityBypassAmbient.Enter()`** is only permitted when **`SqlServer:RowLevelSecurity:ApplySessionContext`** is **true** **and** both **`ARCHLUCID_ALLOW_RLS_BYPASS=true`** and **`ArchLucid:Persistence:AllowRlsBypass=true`** are set. This replaces env-only bypass. When enabled on a **production-like** host, Prometheus records **`archlucid_rls_bypass_enabled_info{scope="production_like"}==1`** and alert **`ArchLucidRlsBypassEnabledInProduction`** may fire; see **`infra/prometheus/archlucid-alerts.yml`**.

## Log injection (CWE-117)

ArchLucid uses **Serilog** with **structured logging**: message templates use named placeholders (`{RunId}`, `{Path}`, etc.), and sinks such as JSON formatters emit parameters as **separate fields**. That layout reduces the impact of delimiter injection in **JSON** and similar structured sinks.

**Plaintext sinks** (console, rolling file text, etc.) can still be abused if a logged **string** contains newlines or other control characters—an attacker can forge extra log lines or break parsers. For any **`string`-typed value that originates from user input** (request body, URL path, query string, header), pass it through **`LogSanitizer.Sanitize()`** from **`ArchLucid.Core.Diagnostics`** before it is passed to **`ILogger`** as a structured parameter.

**Value types from routing are safe:** **`Guid`**, **`int`**, **`DateTime`**, and similar types bound from **`[FromRoute]`** do not need sanitization—their string representation in logs cannot introduce C0/C1 control characters in the way arbitrary HTTP strings can. If a route parameter is bound as **`string`** (even when it holds a UUID), static analysis may still treat it as untrusted input; use **`LogSanitizer`**, refactor to a value type + **`{param:guid}`**, or follow dismissal guidance in **`docs/CODEQL_TRIAGE.md`**.

**When adding new `ILogger` calls in controllers:** apply **`LogSanitizer.Sanitize()`** to any **`string`** parameter sourced from HTTP input. **`Guid`**, **`int`**, and other non-string value types from route or validated models are inherently safe for this class of issue.

## PII and conversation retention

- **Architecture requests and run payloads** may include system descriptions, URLs, and free text that operators paste from internal docs. Treat stored **run rows**, **context snapshots**, **agent traces** (including optional inline prompts when enabled), and **audit** entries as **tenant-scoped operational data**, not anonymous telemetry.
- **LLM calls:** When **`AgentExecution:TraceStorage:PersistFullPrompts`** or inline forensic columns are enabled, prompts and completions may be persisted in SQL and/or blob storage. Restrict access via **RBAC**, **private networking**, and **SQL/Key Vault** permissions aligned with your data classification policy.
- **Retention:** Default posture is **keep until archived/deleted by operator workflows** (see **[AUDIT_RETENTION_POLICY.md](AUDIT_RETENTION_POLICY.md)** for audit export and tiering notes). For regulated environments, define **explicit retention / purge** runbooks per workspace and document them in deployment packages.
- **Exports:** Support bundles, DOCX/ZIP exports, and audit CSVs can contain **PII-sized** content; distribute only over approved channels and encrypt at rest in transit per org policy.
