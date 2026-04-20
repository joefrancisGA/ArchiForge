> **Scope:** First-run walkthrough (operator UI) - full detail, tables, and links in the sections below.

# First-run walkthrough (operator UI)

## Objective

Give operators a **linear checklist** for creating the first authority run using the **New run** wizard, without relying on screenshots (which go stale quickly).

## Assumptions

- The UI is available at **`/runs/new`** (see **`docs/FIRST_RUN_WIZARD.md`** for design intent).
- The API is reachable with a configured auth mode (**`docs/SECURITY.md`**, **`docs/PILOT_GUIDE.md`**).

## Constraints

- This walkthrough does not replace **`docs/ONBOARDING_HAPPY_PATH.md`** or **`docs/LIVE_E2E_HAPPY_PATH.md`** for HTTP-level detail.

## Steps

1. **Open the shell** — Sign in per your environment (Entra, API key, or DevelopmentBypass in local dev only).
2. **Navigate to New run** — Use **`/runs/new`** or the primary nav entry **New run**.
3. **Pick a preset or template** — Choose the closest sample if you are evaluating; customize fields only where you have real system facts.
4. **Complete each wizard step** — Advance only when required fields validate; note inline errors reference **`correlationId`** when the proxy surfaces API failures (**`docs/TROUBLESHOOTING.md`**).
5. **Submit** — The wizard calls **`POST /v1/architecture/request`**; capture the returned **run id** from the success path or runs list.
6. **Execute and commit** — From run detail, drive **Execute** then **Commit** when the pipeline reports **Ready for commit** (**`docs/operator-shell.md`**).
7. **Verify artifacts** — Confirm manifest + artifacts appear; use **Compare**/**Replay** only after you have two runs or an export need (**`docs/V1_SCOPE.md`**).

## Related

- **`docs/FIRST_RUN_WIZARD.md`** — design and UX notes.
- **`docs/PILOT_GUIDE.md`** — pilot-facing scope and support boundaries.
- **`docs/operator-shell.md`** — operator shell patterns and empty states.
