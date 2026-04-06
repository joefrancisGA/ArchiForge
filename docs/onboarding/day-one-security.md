# Day one — Security / GRC (week one)

**Goal:** Map **trust boundaries**, **identity**, and **data-plane exposure** for **ArchLucid** in **your** Azure landing zone. **Not** a full pen test or every ADR.

**Ticket:** `ONBOARD-SEC-001` (copy into your work tracker)

---

## Scope (3–5 outcomes — check off by end of week one)

- [ ] **1. Trust narrative** — Read [CUSTOMER_TRUST_AND_ACCESS.md](../CUSTOMER_TRUST_AND_ACCESS.md) (edge → API → SQL/blob; Entra vs API key; private endpoints).
- [ ] **2. AuthZ model** — Map **Admin / Operator / Reader** (Entra app roles) to API policies **ReadAuthority / ExecuteAuthority / AdminAuthority** (root [README.md](../../README.md#api-authentication-archiforgeauth), [appsettings.Entra.sample.json](../../ArchiForge.Api/appsettings.Entra.sample.json)).
- [ ] **3. Data isolation** — Skim [security/MULTI_TENANT_RLS.md](../security/MULTI_TENANT_RLS.md): RLS as **defense in depth**, `SESSION_CONTEXT`, and that **`ApplySessionContext`** must be **on** in production SQL paths you care about.
- [ ] **4. LLM / RAG surface** — Skim [security/ASK_RAG_THREAT_MODEL.md](../security/ASK_RAG_THREAT_MODEL.md) if Ask/RAG is enabled; note exfiltration and prompt-injection assumptions.
- [ ] **5. Supply chain** — Confirm your org accepts the repo’s CI posture: **gitleaks**, **CodeQL**, **Trivy** (image + Terraform), vulnerable NuGet gate ([BUILD.md](../BUILD.md) CI section).

---

## Escalation

| Blocker | Where |
|---------|--------|
| PII / retention | [security/PII_RETENTION_CONVERSATIONS.md](../security/PII_RETENTION_CONVERSATIONS.md) |
| Managed identity + SQL/blob | [security/MANAGED_IDENTITY_SQL_BLOB.md](../security/MANAGED_IDENTITY_SQL_BLOB.md) |
| SMB / storage exposure | Workspace rule: no public **445**; [infra/terraform-private/README.md](../../infra/terraform-private/README.md) |

**Last reviewed:** 2026-04-04
