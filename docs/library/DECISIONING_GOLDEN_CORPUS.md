> **Scope:** Decisioning golden corpus — CI contract, layout, recording workflow, and maintenance rules for `tests/golden-corpus/decisioning/`.

# Decisioning golden corpus

**Audience:** Engineers changing authority decisioning, merge integration, or manifest emission who need a merge-blocking correctness signal without LLMs or SQL integration tests.

**Status:** Active hard gate in `.github/workflows/ci.yml` (`dotnet-fast-core` → **Test — fast core**). Tests live in `ArchLucid.Decisioning.Tests` and run under `Suite=Core` with `Category!=GoldenCorpusRecord`.

---

## Why this exists

The pipeline **agent output → typed findings → manifest decisions → audit** is the highest-risk place for silent regressions. The corpus freezes **deterministic** JSON for each curated input bundle so any drift fails CI before it reaches production.

**Non-goals:** No live LLM calls, no integration-tier SQL, and no edits to `ArchLucid.Decisioning` / `ArchLucid.Decisioning.Merge` production code or `AuditEvents` schema solely to satisfy a case (if behavior is intentional, update expected files and document in the case `README.md`).

---

## Corpus contract

Each case is a directory under `tests/golden-corpus/decisioning/` named `case-NN` (two-digit index, 30 cases total).

| File | Purpose |
|------|---------|
| `input.json` | Agent-result bundle: graph snapshot, run identifiers, optional merge payload (same shape as `GoldenCorpusInputDocument`). |
| `expected-findings.json` | Normalized typed finding rows (stable sort order). |
| `expected-decisions.json` | Manifest decision payload after authority + optional merge (stable shape). |
| `expected-audit-types.json` | Sorted list of `AuditEventType` string names emitted during the harness run. |
| `README.md` | What the case exercises; note any intentional quirks of frozen behavior. |

On assertion failure, `GoldenCorpusRegressionTests` writes sibling files with an `.actual` suffix for trivial diffing.

---

## Where the harness lives

| Component | Location |
|-----------|----------|
| **Full pipeline** (orchestrator, engines, decision engine, optional merge, audit capture) | `ArchLucid.Decisioning.Tests/GoldenCorpus/GoldenCorpusHarness.cs` |
| **Shared primitives** (frozen clock provider, collecting audit service, JSON options) | `ArchLucid.TestSupport/GoldenCorpus/` |

`ArchLucid.TestSupport` intentionally does **not** reference `ArchLucid.Decisioning`, so the heavy wiring stays in the Decisioning test project and the solution graph stays clean.

---

## How to refresh or add cases

1. **Add or edit definitions** in `GoldenCorpusGraphFactory` (and related DTOs) so new scenarios are generated with stable semantics.
2. **Record** (local only): set `ARCHLUCID_RECORD_DECISIONING_GOLDEN=1` and run:
   - `dotnet test ArchLucid.Decisioning.Tests/ArchLucid.Decisioning.Tests.csproj --filter "FullyQualifiedName~GoldenCorpusMaterializerTests"`
3. **Commit** the updated `tests/golden-corpus/decisioning/**` tree and case `README.md` files.
4. **Verify** regression: `dotnet test ArchLucid.Decisioning.Tests/ArchLucid.Decisioning.Tests.csproj --filter "FullyQualifiedName~GoldenCorpusRegressionTests"` (or rely on CI `Suite=Core`).

`GoldenCorpusMaterializerTests` is tagged `Category=GoldenCorpusRecord` and is **excluded** from the fast-core CI filter so accidental env in CI does not rewrite the repo.

---

## Maintenance rule: no case deletion

**Do not delete** an existing `case-NN` directory. The corpus only **grows** (new indices or new files). If a scenario is obsolete, keep the folder and mark the `README.md` as superseded or redirect to the replacement case. Rationale: git history and reviewers can always see what was locked and when.

---

## Diagram (high level)

```mermaid
flowchart LR
  subgraph inputs["inputs"]
    JSON["input.json"]
  end
  subgraph harness["GoldenCorpusHarness"]
    FO[FindingsOrchestrator]
    DE[RuleBasedDecisionEngine]
    MG[Merge optional]
    AUD[CollectingAuditService]
  end
  subgraph frozen["expected files"]
    EF[expected-findings.json]
    ED[expected-decisions.json]
    EA[expected-audit-types.json]
  end
  JSON --> FO --> DE
  DE --> MG
  DE --> AUD
  FO --> EF
  MG --> ED
  AUD --> EA
```

---

## Related

- Typed findings detail: [DECISIONING_TYPED_FINDINGS.md](./DECISIONING_TYPED_FINDINGS.md)
- CI tiering comment in `.github/workflows/ci.yml` (fast core job)
