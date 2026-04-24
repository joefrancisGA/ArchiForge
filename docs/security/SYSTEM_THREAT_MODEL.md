> **Scope:** ArchLucid — system STRIDE threat model (summary) - full detail, tables, and links in the sections below.

> **Spine doc:** [Five-document onboarding spine](../FIRST_5_DOCS.md). Read this file only if you have a specific reason beyond those five entry documents.


# ArchLucid — system STRIDE threat model (summary)

## 1. Objective

Give security reviewers a **single** STRIDE-oriented view of the **whole** product boundary (not only Ask/RAG — see **`ASK_RAG_THREAT_MODEL.md`** for that slice).

## 2. Assumptions

- Azure-first deployment: **Container Apps**, **Azure SQL**, optional **Service Bus**, **Blob**, **Front Door + WAF**, **Entra ID**.
- Operators are **authenticated**; **DevelopmentBypass** exists only in non-production with guardrails.
- Incomplete requirements and misconfiguration are **expected**; controls are **defense in depth**.

## 3. Constraints

- This is **not** a formal penetration-test report; it informs controls and backlog.
- **No public SMB (445)**; data plane via **private endpoints** where configured (**`docs/CUSTOMER_TRUST_AND_ACCESS.md`**).

## 4. Architecture overview

**Nodes:** Browser / CLI → optional **edge (Front Door, APIM)** → **ArchLucid.Api** / **ArchLucid.Worker** → **SQL**, **Blob**, **Azure OpenAI**, optional **Service Bus**, **Redis**.

**Trust boundaries:** Client ↔ Edge, Edge ↔ API, API ↔ SQL/Blob/LLM, Worker ↔ SQL/queues.

## 5. Component breakdown (STRIDE — representative threats)

| Boundary | S | T | R | I | D | E | Primary mitigations |
|----------|---|---|---|---|---|---|----------------------|
| Client → API | Fake tokens, stolen API keys | Tampered bodies | — | TLS + JWT validation / API key | Rate limits, outbox flood | Privilege via mis-roles | Entra roles, least-privilege policies, **`ArchLucidPolicies`** |
| Client → API (trial) | External IdP token replay, weak local passwords | Hosted IdP / ArchLucid SQL tamper | — | External ID tenant binding + JwtBearer issuer rules; PBKDF2 + optional HIBP | Credential stuffing, cache poisoning | Over-privileged minted JWTs | **`Auth:Trial:Modes`**, **`TrialBootstrapEmailVerificationPolicy`**, lockout + role gates (**`docs/security/TRIAL_AUTH.md`**) |
| API → SQL | SQL auth misuse | SQL injection | — | RLS + parameterized Dapper | DB DoS, heavy queries | `db_owner` misuse | Managed identity / scoped SQL user, RLS **`SESSION_CONTEXT`** |
| API → LLM | — | Prompt injection → unsafe actions | — | **PII / secrets in prompts** (see **`AGENT_TRACE_FORENSICS.md`**, **`docs/runbooks/LLM_PROMPT_REDACTION.md`**) | Token exhaustion, 429 storms | — | Quotas, circuit breakers, deny-list **`LlmPromptRedaction`** (on by default in shipped appsettings) |
| API → Blob | SAS misuse | Object tamper | — | Blob exfiltration | — | — | Private endpoint, MI, container ACLs |
| Worker → Service Bus | — | Message tamper | — | Payload leak | Queue flood | — | Namespace auth, DLQ, admin retry APIs |
| Billing webhooks (Stripe / Azure Marketplace) | Forged webhook replay, stolen signing secrets | Tampered payloads → wrong tenant conversion | — | **Stripe-Signature** HMAC + **Marketplace JWT** validation; **dbo.BillingWebhookEvents** idempotency key | Webhook flood | Replay after partial failure | Anonymous endpoints only after crypto verification; return **200** only after SQL commit; see **`docs/BILLING.md`** |
| Worker → SQL (trial lifecycle + hard purge) | Mis-timed transitions, operator panic deletes | Orphan / cross-tenant purge if mis-scoped | — | **Idempotent** `TryRecordTrialLifecycleTransitionAsync`; `SqlTenantHardPurgeService` scopes **`WHERE TenantId=@TenantId`**; **`dbo.AuditEvents`** excluded | Long `DELETE TOP` loops | Break-glass abuse | **`SqlRowLevelSecurityBypassAmbient`** policy + **`TrialLifecycleSchedulerHostedService`** leader lease; see **`docs/runbooks/TRIAL_LIFECYCLE.md`** |
| Logs / audit | — | Log forging (**CWE-117**) | Repudiation if audit skipped | Sensitive data in logs | Log volume DoS | — | **`LogSanitizer`**, append-only **`dbo.AuditEvents`** (role **DENY**) |

### 5.1 Anonymous demo read surfaces (`/v1/demo/*`)

| Route | Auth | Gate | Rate limit | Data scope | Caching / staleness |
|-------|------|------|------------|------------|---------------------|
| **`GET /v1/demo/explain`** | **AllowAnonymous** | **`Demo:Enabled`** (`FeatureGate`) | **`fixed`** | Hard-pinned demo tenant / seeded Contoso identifiers only | No response-body cache at the controller |
| **`GET /v1/demo/preview`** | **AllowAnonymous** | **`Demo:Enabled`** (`FeatureGate`) | **`fixed`** | Same scope; flat JSON bundle (run + manifest + artifacts + timeline + aggregate explanation) | In-process **`IHotPathReadCache`** entry (TTL **`Demo:PreviewCacheSeconds`**, default 5 minutes); stable cache key with **run/manifest identity inside the cached value** so re-seed propagates on next miss; HTTP **`Cache-Control`** + SHA-256 **`ETag`** (**`304`** when **`If-None-Match`** matches) |

**Privacy posture:** responses set **`isDemoData=true`** and fictional Contoso naming from the seed; **`noindex`** on the marketing page prevents search engines from indexing demo metrics as production telemetry.

### 5.2 Local first-real-value path (`archlucid try --real`)

| Surface | Risk | Mitigation |
|---------|------|------------|
| Developer exports **`AZURE_OPENAI_API_KEY`** into shell / compose | Key theft via shoulder-surfing, screen share, or committed `.env` | Opt-in gate **`ARCHLUCID_REAL_AOAI=1`** + CLI preflight; document short-lived keys only (**[`docs/library/FIRST_REAL_VALUE.md`](../library/FIRST_REAL_VALUE.md)**, **[`docs/library/CONFIGURATION_KEY_VAULT.md`](../library/CONFIGURATION_KEY_VAULT.md)** § local). |
| Fallback to simulator after AOAI failure | Buyer misreads simulator metrics as LLM output | Markdown **warning** callout + **Execution provenance** footer; audit **`FirstRealValueRunFellBackToSimulator`**. |
| **`--strict-real`** in automation | Pipeline fails when AOAI unavailable | Explicit operator choice — no silent substitution. |

## 6. Data flow (high level)

1. **Authority run:** HTTP → transactional SQL → outboxes → worker indexing / integration publish.
2. **Governance:** HTTP → SQL + durable **`IAuditService`** + optional integration events.
3. **LLM path:** Agent handler → completion client (retry + circuit breaker + optional fallback) → Azure OpenAI.

## 7. Security model

- **Default deny** on controllers; health/version exceptions only where coded.
- **CORS** deny-by-default in production without explicit origins.
- **OWASP ZAP** + **Schemathesis** in CI (see **`docs/SECURITY.md`**).
- **Operational:** secret rotation (**`SECRET_AND_CERT_ROTATION.md`**), private networking (**`infra/terraform-private`**).

## 8. Operational considerations

- **Review cadence:** After major features touching auth, RLS, or LLM prompts.
- **Drills:** Geo-failover (**`docs/runbooks/GEO_FAILOVER_DRILL.md`**).
- **Gaps to track in backlog:** Formal third-party pen test (templates in **`docs/security/PEN_TEST_*`**), residual uncovered tables per **`MULTI_TENANT_RLS.md`** §9, and continuous improvement of deny-list patterns beyond the built-in **`PromptRedactor`** rules.

## Related

- **`docs/SECURITY.md`**
- **`docs/security/ASK_RAG_THREAT_MODEL.md`**
- **`docs/AUDIT_COVERAGE_MATRIX.md`**
