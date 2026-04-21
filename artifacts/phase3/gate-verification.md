# ADR 0021 Phase 3 exit gate verification (2026-04-21)

Mechanical checks recorded before any Phase 3 code deletion. **Result: gates not satisfied — Phase 3 blocked.**

| Gate | Criterion | Result | Evidence |
|------|-----------|--------|----------|
| **(i)** | `git log --diff-filter=D --since="30 days ago"` for coordinator concrete deletion targets shows ≥30 days green after deletion | **N/A today** | `artifacts/phase3/git-log-gate-i.txt` is empty — concretes not yet deleted; PR A/B sequencing applies after merge |
| **(ii)** | `dotnet test --filter "Suite=Core\|Suite=Integration"` green | **Not run in this session** | Re-run locally before merge when gates unblock |
| **(iii)** | Live-API E2E `archlucid-ui/e2e/live-api-*.spec.ts` green within 7 days | **Not verified** | Check `.github/workflows/ci.yml` job history on `main` |
| **(iv)** | Parity report: 14 contiguous days, Coordinator writes = 0 | **FAIL** | `docs/runbooks/COORDINATOR_TO_AUTHORITY_PARITY.md` still has `*(TBD)*` rows only |
| **Phase 2** | `AuditEventTypes.Run` nested class exists (active catalog) | **FAIL** | `rg` on `ArchLucid.Core/Audit/AuditEventTypes.cs` — no `static class Run` |

**Additional stop signal:** `docs/INTEGRATION_EVENTS_AND_WEBHOOKS.md` references `CoordinatorRunCommitCompleted` in automation text — customer-visible naming must follow deprecation policy before removal.
