# Day one — Developer (week one)

**Goal:** Ship a small, safe change or run the stack locally with confidence. **Not** full domain mastery.

**Ticket:** `ONBOARD-DEV-001` (copy into your work tracker)

---

## Scope (3–5 outcomes — check off by end of week one)

- [ ] **1. Toolchain** — .NET 10 SDK, `git clone`, `dotnet restore` + `dotnet build` at repo root succeed ([CONTRIBUTOR_ONBOARDING.md](../CONTRIBUTOR_ONBOARDING.md)).
- [ ] **2. Local API + SQL** — SQL reachable (Docker `archiforge dev up` or compose), `ConnectionStrings:ArchiForge` set (user secrets), `dotnet run --project ArchiForge.Api`, **`GET /health/ready`** returns healthy ([GOLDEN_PATH.md](../GOLDEN_PATH.md) Phase 1).
- [ ] **3. Fast tests** — Run the Core corset (matches CI fast job):  
  `dotnet test --filter "Suite=Core&Category!=Slow&Category!=Integration"` ([TEST_EXECUTION_MODEL.md](../TEST_EXECUTION_MODEL.md)).
- [ ] **4. One contract** — Skim [API_CONTRACTS.md](../API_CONTRACTS.md) (versioning `/v1`, correlation ID, one status code you will handle).
- [ ] **5. Small change** — Open a PR with a **tiny** change (doc typo, test name, log message) so you practice the full loop (build + Core tests + green CI).

---

## Escalation

| Blocker | Where |
|---------|--------|
| Build / packages | [BUILD.md](../BUILD.md), [TROUBLESHOOTING.md](../TROUBLESHOOTING.md) |
| SQL / migrations | [SQL_SCRIPTS.md](../SQL_SCRIPTS.md) |
| Auth locally | Root [README.md](../../README.md#api-authentication-archiforgeauth) |

**Last reviewed:** 2026-04-04
