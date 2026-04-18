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
| API → LLM | — | Prompt injection → unsafe actions | — | **PII / secrets in prompts** (see **`AGENT_TRACE_FORENSICS.md`**) | Token exhaustion, 429 storms | — | Quotas, circuit breakers, optional prompt redaction backlog |
| API → Blob | SAS misuse | Object tamper | — | Blob exfiltration | — | — | Private endpoint, MI, container ACLs |
| Worker → Service Bus | — | Message tamper | — | Payload leak | Queue flood | — | Namespace auth, DLQ, admin retry APIs |
| Billing webhooks (Stripe / Azure Marketplace) | Forged webhook replay, stolen signing secrets | Tampered payloads → wrong tenant conversion | — | **Stripe-Signature** HMAC + **Marketplace JWT** validation; **dbo.BillingWebhookEvents** idempotency key | Webhook flood | Replay after partial failure | Anonymous endpoints only after crypto verification; return **200** only after SQL commit; see **`docs/BILLING.md`** |
| Worker → SQL (trial lifecycle + hard purge) | Mis-timed transitions, operator panic deletes | Orphan / cross-tenant purge if mis-scoped | — | **Idempotent** `TryRecordTrialLifecycleTransitionAsync`; `SqlTenantHardPurgeService` scopes **`WHERE TenantId=@TenantId`**; **`dbo.AuditEvents`** excluded | Long `DELETE TOP` loops | Break-glass abuse | **`SqlRowLevelSecurityBypassAmbient`** policy + **`TrialLifecycleSchedulerHostedService`** leader lease; see **`docs/runbooks/TRIAL_LIFECYCLE.md`** |
| Logs / audit | — | Log forging (**CWE-117**) | Repudiation if audit skipped | Sensitive data in logs | Log volume DoS | — | **`LogSanitizer`**, append-only **`dbo.AuditEvents`** (role **DENY**) |

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
- **Gaps to track in backlog:** Formal pen test, expanded RLS table coverage, systematic PII redaction in prompts.

## Related

- **`docs/SECURITY.md`**
- **`docs/security/ASK_RAG_THREAT_MODEL.md`**
- **`docs/AUDIT_COVERAGE_MATRIX.md`**
