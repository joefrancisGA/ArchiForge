> **Scope:** Healthcare / Medicare–adjacent **sales and architecture** positioning — not legal advice, not a compliance attestation. For procurement posture, see [`trust-center.md`](../trust-center.md) and in-repo DPA/MSA templates.

# Healthcare vertical — architecture brief (starter)

**Audience:** Field architects and sponsors describing ArchLucid next to **Medicare / Medicaid–adjacent** systems. **BAA, PHI, and attestation** questions belong in **contract** and **trust-center** copy — not in this file as claims.

**Last reviewed:** 2026-04-27

## Product fit (one paragraph)

ArchLucid helps teams produce **reviewable architecture manifests, findings, and governance evidence** for systems *you describe* in briefs and structured context. It is **not** an EHR, claims system, or clinical data store. **Do not upload PHI** into briefs or free-text context; use de-identified or architectural descriptions only. Contractual and BAA paths → **`sales@archlucid.net`**.

## Medicare / Medicaid–adjacent integration (patterns, not an implementation spec)

| Concern | How teams usually frame it in an architecture run | What ArchLucid evidence can reflect |
|--------|----------------------------------------------------|-------------------------------------|
| **Boundary systems** (e.g. CMS interfaces, state MMIS) | As components and data-flow edges in the manifest | Graph + findings on coupling and interfaces |
| **PII/PHI separation** | As explicit non-goals in the brief and policy packs | Drift and governance rules against “no PHI in context” team norms |
| **Audit trail** | As operational requirement | Append-only audit and run history (see [`AUDIT_COVERAGE_MATRIX.md`](../library/AUDIT_COVERAGE_MATRIX.md)) |

## Minimum HIPAA *program* control mapping (illustrative)

**Not** a HITRUST/SOC mapping — a **starter list** for conversation with GRC. Product controls in-repo: RLS, tenant scope, DPA template, subprocessors, trust center.

| Typical HIPAA administrative / technical theme | In-repo touchpoint (self-asserted) |
|------------------------------------------------|------------------------------------|
| Access control (least privilege) | App RBAC + optional API keys; see [`MULTI_TENANT_RLS.md`](../security/MULTI_TENANT_RLS.md) |
| Audit controls | Durable audit design — [`AUDIT_COVERAGE_MATRIX.md`](../library/AUDIT_COVERAGE_MATRIX.md) |
| Transmission / integrity (in scope of your deployment) | TLS, Azure patterns in [`MANAGED_IDENTITY_SQL_BLOB.md`](../security/MANAGED_IDENTITY_SQL_BLOB.md) |

**Gap:** A **BAA** (if required) is a **legal** instrument, not a feature flag. State explicitly in customer conversations whether your deployment processes PHI; default product positioning is **architecture evidence only, no clinical PHI in scope** unless a separate agreement says otherwise (see [`PENDING_QUESTIONS.md`](../PENDING_QUESTIONS.md), [`V1_SCOPE.md`](../library/V1_SCOPE.md)).

## Related

- **Trust / PHI overview:** [`trust-center.md`](../trust-center.md) — **Healthcare and PHI**
- **Tenant isolation (buyer):** [`TENANT_ISOLATION.md`](TENANT_ISOLATION.md)
