# ADR 0012: Runs / authority convergence — `ArchitectureRuns` write freeze inventory

- **Status:** Accepted
- **Date:** 2026-04-11
- **Supersedes / extends:** [0002-dual-persistence-architecture-runs-and-runs.md](0002-dual-persistence-architecture-runs-and-runs.md) (operational inventory and freeze enforcement)

## Context

Dual persistence remains: **`dbo.Runs`** (GUID, authority) is the **target** source of truth; **`dbo.ArchitectureRuns`** (string `RunId`) is **legacy** but still mutated by several production paths.

All writes to **`dbo.ArchitectureRuns`** in product code go through **`IArchitectureRunRepository`**:

- **`CreateAsync`**
- **`UpdateStatusAsync`**
- **`ApplyDeferredAuthoritySnapshotsAsync`**

The only production **Dapper** implementation is **`ArchitectureRunRepository`** (`ArchLucid.Persistence/Data/Repositories/ArchitectureRunRepository.cs`), which issues **`INSERT INTO ArchitectureRuns`** and **`UPDATE ArchitectureRuns`**. No other **`.cs`** file contains **`INSERT INTO`** / **`UPDATE`** targeting **`dbo.ArchitectureRuns`** directly.

**In-memory:** **`InMemoryArchitectureRunRepository`** is registered when **`ArchLucid:StorageProvider=InMemory`** (`ServiceCollectionExtensions.CoordinatorAndArtifacts.cs`); it implements the same mutating interface for non-SQL hosts.

**Not write paths (injection only):** **`RunsController`** and **`RunDetailQueryService`** resolve **`IArchitectureRunRepository`** for **`GetByIdAsync`** / **`ListAsync`** only.

**Out of scope (not `ArchitectureRuns` DML):**

- **`ArchLucid.Persistence.Tests/Support/ArchitectureCommitTestSeed.cs`** — test SQL **`INSERT INTO dbo.ArchitectureRuns`** for FK fixtures.
- **`DapperProductLearningPlanningPlanLinkRepository`** — **`INSERT INTO dbo.ProductLearningImprovementPlanArchitectureRuns`** (junction); validates **`ArchitectureRuns.RunId`** exists but does not insert into **`ArchitectureRuns`**.

## Decision

1. **Write freeze:** From **2026-09-30**, no new product features or net-new code paths may **write** to **`dbo.ArchitectureRuns`**, per **`docs/DATA_CONSISTENCY_MATRIX.md`**. Changes to listed writers require an **ADR** and a **dated removal task**.
2. **This ADR** is the **canonical inventory** of production write call sites as of the audit date. Re-run **`rg 'CreateAsync|UpdateStatusAsync|ApplyDeferredAuthoritySnapshotsAsync'`** on **`IArchitectureRunRepository`** usages when the epic closes.

## Inventory — production write call sites

| Caller | Method | Current behavior | Convergence action | Status |
|--------|--------|------------------|-------------------|--------|
| **ArchitectureRunCreateOrchestrator** | **`CreateAsync`** (with/without UoW connection) | **Already dual-writes:** after **`CoordinatorService` → `IAuthorityRunOrchestrator`**, authority persisted **`dbo.Runs`**; this path inserts legacy **`ArchitectureRuns`** plus request, evidence bundle, tasks. | On convergence: remove legacy insert or gate behind explicit compatibility; **`dbo.Runs`** remains sole insert for new work after freeze. | TODO |
| **ArchitectureRunCommitOrchestrator** | **`UpdateStatusAsync`** | Failure path → **`Failed`**; commit path → **`Committed`** with **`expectedStatus: ReadyForCommit`**. | **Converge to Runs:** mirror the same status transition on **`dbo.Runs`** via **`IRunRepository`** (or single-writer facade) before dropping legacy updates. | TODO |
| **ArchitectureRunExecuteOrchestrator** | **`UpdateStatusAsync`** | After execute phase → **`ReadyForCommit`** with expected prior status. | **Converge to Runs:** mirror status on **`dbo.Runs`** / authority aggregate. | TODO |
| **ReplayRunService** | **`CreateAsync`** | Inserts a new **`ArchitectureRuns`** row for **`replayRunId`**; this service does not insert a matching **`dbo.Runs`** / **`RunRecord`** row for the replay id. | **Requires ADR:** define whether replay must create/update **`RunRecord` / `dbo.Runs`** and authority artifacts (Runs-first replay). | TODO |
| **ReplayRunService** | **`UpdateStatusAsync`** | When **`commitReplay`**, sets **`Committed`** with manifest version + **`completedUtc`**. | **Requires ADR** (depends on replay create decision), then **converge to Runs:** mirror commit on **`dbo.Runs`**. | TODO |
| **DemoSeedService** | **`CreateAsync`**, **`UpdateStatusAsync`** | Dev/demo Contoso baseline: legacy **`ArchitectureRuns`** + related coordinator rows. | **Requires ADR:** seed **`dbo.Runs`** (and related) in parity or retire demo’s legacy dependency before freeze. | TODO |
| **ArchitectureApplicationService** | **`UpdateStatusAsync`** (result submission + fake-result seed branches) | Transitions **`WaitingForResults` ↔ `ReadyForCommit`** after agent results. | **Converge to Runs:** mirror transitions on **`dbo.Runs`**. | TODO |
| **AuthorityPipelineWorkProcessor** | **`ApplyDeferredAuthoritySnapshotsAsync`** | Deferred authority: updates snapshot pointer columns + **`TasksGenerated`** on legacy row when status was **`Created`**. | **Converge to Runs:** apply equivalent snapshot/status updates to **`dbo.Runs`** (or single persistence path). | TODO |
| **ArchitectureRunRepository** | **`CreateAsync`**, **`UpdateStatusAsync`**, **`ApplyDeferredAuthoritySnapshotsAsync`** | Sole production **`INSERT`/`UPDATE`** implementation for **`ArchitectureRuns`**. | Retire type after callers migrate; no new SQL writers to this table. | TODO |
| **InMemoryArchitectureRunRepository** | **`CreateAsync`**, **`UpdateStatusAsync`**, **`ApplyDeferredAuthoritySnapshotsAsync`** | In-memory implementation when **`StorageProvider=InMemory`**. | **Converge to Runs:** align with SQL path; lifecycle writes should target **`IRunRepository`** only post-convergence. | TODO |

## Consequences

- **Positive:** Single checklist for code review and security/SRE audits before the write freeze; no guesswork about “mystery writers.”
- **Negative:** Replay and demo paths need explicit design choices (**ADR**), not only mechanical dual-writes.
- **After write freeze:** Any new feature touching **`ArchitectureRuns`** requires an **ADR extension** naming the exception and a **dated task** to remove it.

## Links

- `docs/DATA_CONSISTENCY_MATRIX.md` — milestones (**RunsAuthorityConvergence**)
- [0002-dual-persistence-architecture-runs-and-runs.md](0002-dual-persistence-architecture-runs-and-runs.md)

## Audit method

- **`rg IArchitectureRunRepository`** across **`*.cs`**, exclude `*.Tests`, then inspect each non-test file for **`CreateAsync`**, **`UpdateStatusAsync`**, **`ApplyDeferredAuthoritySnapshotsAsync`**.
- **`rg "INSERT INTO.*ArchitectureRuns|UPDATE.*ArchitectureRuns"`** across **`*.cs`** to confirm no stray SQL.

## CI enforcement (RunsAuthorityConvergence pragma)

**Compiler:** The solution already uses **`TreatWarningsAsErrors`** (**`Directory.Build.props`**), so **`[Obsolete]`** write members on **`IArchitectureRunRepository`** (and obsolete repository types) produce **CS0618** as a build error when unsuppressed. That is unchanged by this ADR.

**CI documentation guard (this ADR):** **`.github/workflows/ci.yml`** (`.NET: fast core`) runs **`scripts/ci/assert_architecture_run_write_pragma.py`** instead of adding a second compiler-only gate. The script scans non-test **`*.cs`** files (excluding the interface and both repository implementation files) and **fails** if any of the following appears **outside** an active **`#pragma warning disable CS0618`** block whose line contains **`RunsAuthorityConvergence`**:

- **`runRepository.`** / **`_runRepository.`** calls to **`CreateAsync`** or **`UpdateStatusAsync`**
- **`architectureRunRepository.ApplyDeferredAuthoritySnapshotsAsync`**
- **`new InMemoryArchitectureRunRepository(`** or **`AddScoped<…, ArchitectureRunRepository>(…)`**-style registration of the Dapper implementation

**Remediation:** Add the dated suppression (see existing call sites), then migrate off legacy writes per this ADR — do not drop the marker without an ADR update.

**Rationale:** The compiler accepts any **`CS0618`** suppression text; this script ensures new code cannot “silence” obsolete **`ArchitectureRuns`** writes without explicitly tying them to the **2026-09-30** convergence milestone.
