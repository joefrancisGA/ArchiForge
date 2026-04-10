# Day one — Developer (week one)

**Goal:** Ship a small, safe change or run the **ArchLucid** stack locally with confidence. **Not** full domain mastery. (Repo and projects: `ArchLucid.*`.)

**Ticket:** `ONBOARD-DEV-001` (copy into your work tracker)

---

## Scope (3–5 outcomes — check off by end of week one)

- [ ] **1. Toolchain** — .NET 10 SDK, `git clone`, `dotnet restore` + `dotnet build` at repo root succeed ([CONTRIBUTOR_ONBOARDING.md](../CONTRIBUTOR_ONBOARDING.md)).
- [ ] **2. Local API + SQL** — SQL reachable (Docker `archlucid dev up` or compose), `ConnectionStrings:ArchLucid` set (user secrets), `dotnet run --project ArchLucid.Api`, **`GET /health/ready`** returns healthy ([GOLDEN_PATH.md](../GOLDEN_PATH.md) Phase 1).
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
| Auth locally | Root [README.md](../../README.md#api-authentication-archlucidauth) |

**Last reviewed:** 2026-04-04

---

## Mental model: `POST /v1/architecture/request`

**What this endpoint does:** Creates an architecture **run**, an **evidence bundle**, and **starter agent tasks** (when not deferred). It does **not** run the full agent simulation loop or manifest commit; those are separate execute/commit flows (`docs/API_CONTRACTS.md`).

**Flow (nodes and edges):**

1. **HTTP** — `POST /v1/architecture/request` on `RunsController` (`v1/architecture`). Policy **`ExecuteAuthority`**; **`EnableRateLimiting("fixed")`** on the controller. Optional **`Idempotency-Key`** for deduplication.
2. **Application** — `IArchitectureRunService.CreateRunAsync` checks idempotency, then calls **`ICoordinatorService.CreateRunAsync`**.
3. **Coordinator** — Validates `ArchitectureRequest`, builds an evidence bundle shell, calls **`IAuthorityRunOrchestrator.ExecuteAsync`** with a mapped context-ingestion request.
4. **Authority pipeline (Persistence)** — `AuthorityRunOrchestrator` opens **`IArchLucidUnitOfWork`**, persists the run, then either:
   - **Deferred path:** enqueues **authority pipeline work** (feature + resolver + non-empty deferred bundle id), **commits** the UoW, returns early with empty starter tasks; **or**
   - **Inline path:** **`IAuthorityPipelineStagesExecutor`** runs stages (context → graph → findings → decisions → manifest/trace → agents/artifacts as configured) inside the same orchestration, then finalizes commit, audit, and **retrieval indexing outbox** when supported.
5. **Application persistence (Data repos)** — On coordination success, `ArchitectureRunService` persists request/run/tasks (and related rows) inside a **`System.Transactions.TransactionScope`** — a **separate** transactional boundary from the authority UoW (two layers on purpose today; see `ArchitectureRunService` and ADR 0004 for manifest/trace split).

**Simulator vs Real:** `AgentExecution:Mode=Simulator` affects **`IAgentExecutor`** used on **execute**, not the create-request path directly. Create-request still runs the **authority** pipeline (or defers it) depending on storage and feature flags.

```mermaid
flowchart TD
  subgraph HTTP["HTTP"]
    R["RunsController POST request"]
    RL["Rate limit + auth policies"]
  end

  subgraph App["Application"]
    ARS["IArchitectureRunService"]
    IDEM["Idempotency check"]
    PERSIST_APP["PersistCreateRunRowsAsync<br/>(TransactionScope)"]
  end

  subgraph Coord["Coordinator"]
    CS["ICoordinatorService"]
    VAL["Validate ArchitectureRequest"]
  end

  subgraph Auth["Persistence / Authority"]
    ARO["IAuthorityRunOrchestrator"]
    UOW["IArchLucidUnitOfWork"]
    DEF{"Defer pipeline<br/>(queue work)?"}
    Q["Authority pipeline work queue"]
    STG["Pipeline stages<br/>(context → graph → findings →<br/>decisions → manifest / trace →<br/>artifacts)"]
    OUT["Retrieval indexing outbox<br/>(when supported)"]
  end

  R --> RL
  RL --> ARS
  ARS --> IDEM
  IDEM --> CS
  CS --> VAL
  VAL --> ARO
  ARO --> UOW
  UOW --> DEF
  DEF -->|yes| Q
  DEF -->|no| STG
  STG --> OUT
  Q --> UOW
  STG --> UOW
  CS --> ARS
  ARS --> PERSIST_APP
```

**Where to read code:** `ArchLucid.Api/Controllers/RunsController.cs`, `ArchLucid.Application/ArchitectureRunService.cs`, `ArchLucid.Coordinator/Services/CoordinatorService.cs`, `ArchLucid.Persistence/Orchestration/AuthorityRunOrchestrator.cs`.
