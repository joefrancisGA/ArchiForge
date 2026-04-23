> **Scope:** Map SOC 2 self-assessment themes to concrete repository evidence.

> **Spine doc:** [Five-document onboarding spine](../FIRST_5_DOCS.md). Read this file only if you have a specific reason beyond those five entry documents.


# Compliance evidence matrix (SOC 2 alignment)

This table links **control themes** from [`SOC2_SELF_ASSESSMENT_2026.md`](SOC2_SELF_ASSESSMENT_2026.md) to **verifiable artifacts** in-repo.

| Theme | Evidence path | Notes |
|-------|----------------|-------|
| Authentication / authorization | [`ArchLucid.Host.Core/Startup/AuthSafetyGuard.cs`](../../ArchLucid.Host.Core/Startup/AuthSafetyGuard.cs), `ArchLucid.Api/Program.cs`, [`SECURITY.md`](../library/SECURITY.md) | Fail-closed defaults |
| Tenant isolation | `docs/security/MULTI_TENANT_RLS.md`, SQL migrations under `ArchLucid.Persistence/Migrations/` | Historical RLS object names may still include `Archiforge*` per rename policy |
| API contract hardening | `.github/workflows/ci.yml` (`api-schemathesis-light`), `.github/workflows/schemathesis-scheduled.yml` | PR vs scheduled coverage |
| Audit trail | `docs/AUDIT_COVERAGE_MATRIX.md`, `ArchLucid.Api/Controllers/Admin/AuditController.cs` | Append-only events |
| Operational readiness | `docs/runbooks/*`, `docs/runbooks/COORDINATOR_TO_AUTHORITY_PARITY.md` | Parity evidence for ADR 0021 |

## Related

- [`SOC2_SELF_ASSESSMENT_2026.md`](SOC2_SELF_ASSESSMENT_2026.md)
- [`../go-to-market/TRUST_CENTER.md`](../go-to-market/TRUST_CENTER.md)
