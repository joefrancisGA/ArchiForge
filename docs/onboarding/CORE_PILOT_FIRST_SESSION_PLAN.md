> **Scope:** Core Pilot — first session plan (analysis) - full detail, tables, and links in the sections below.

# Core Pilot — first session plan (analysis)

**Objective.** Reduce time-to-value on the default **Core Pilot** path: architecture request → run → committed manifest → reviewable artifacts and aggregate explanation.

## CLI ↔ API ↔ UI map

| Step | `ArchLucid.Cli` (see `Program.cs` cases) | REST (`ArchLucid.Api`) | Operator UI |
|------|------------------------------------------|-------------------------|-------------|
| Create / drive run | `run`, `status`, `submit`, `commit`, `seed`, `artifacts` | `/v1/...` authority and architecture routes (versioned under `/v1` per `README.md`) | [`archlucid-ui/src/app/(operator)/runs/new/page.tsx`](../../archlucid-ui/src/app/(operator)/runs/new/page.tsx) → run detail [`runs/[runId]/page.tsx`](../../archlucid-ui/src/app/(operator)/runs/[runId]/page.tsx) |
| Health / support | `health`, `doctor` / `check`, `trace <runId>`, `support-bundle` | `GET /health/live`, `GET /health/ready`, `GET /version` | Same run page surfaces pipeline + downloads |
| Aggregate explanation | — | `GET /v1/explain/runs/{runId}/aggregate` (`ExplanationController`, **`ReadAuthority`**) | Collapsible “Explanation (aggregate)” on run detail |

**Policies:** API uses `ReadAuthority` / `ExecuteAuthority` / `AdminAuthority` (see `README.md`); the operator shell shapes visibility from `GET /api/auth/me` but **401/403 remain authoritative**.

## Current friction (approximate)

- Landing on **Run detail** requires **navigating Runs → selecting a run** (or deep link). First-time users must understand **pipeline completeness** (authority timeline, progress tracker) before the **manifest** and **explanation** sections unlock full value.
- **Explanation** is **collapsed by default** (`defaultOpen={false}` on the collapsible) — one extra click to see aggregate narrative and **citations** (after this change set).
- **Core vs Advanced** disclosure is intentional; users who expand Advanced/Enterprise early may feel **cognitive load** before Core Pilot success — align with [../OPERATOR_DECISION_GUIDE.md](../OPERATOR_DECISION_GUIDE.md).

## Five concrete improvements (file-level)

1. **`runs/[runId]/page.tsx` — open “Explanation” by default when `goldenManifestId` is present** so committed runs show citations without an extra expand (reduces one click post-commit).
2. **`(operator)/page.tsx` — add a one-line “Next: open your latest run”** when `getRunSummary` / recent run list is available (copy-only; optional follow-up).
3. **`RunProgressTracker`** — ensure empty/error states link to **`docs/CORE_PILOT.md`** anchor (copy in component or tooltip).
4. **CLI `archlucid doctor`** — already canonical; add **README** pointer to this doc for “first session under 15 minutes” expectation (docs-only).
5. **Progressive disclosure** — keep Advanced/Enterprise links **collapsed** on first visit (`nav-shell-visibility` / tier model unchanged).

*This file is analysis and planning; implementation toggles should be reviewed for a11y (collapsible default) and sponsor demo flow.*
