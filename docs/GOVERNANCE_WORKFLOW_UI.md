> **Scope:** Governance workflow UI (/governance) - full detail, tables, and links in the sections below.

# Governance workflow UI (`/governance`)

The **Governance workflow** page (`archlucid-ui/src/app/governance/page.tsx`) is an operator-facing surface for the **manifest promotion lifecycle**: approval requests, optional human review, recorded promotions, and per-environment activations. It complements the read-only **Governance resolution** page (`/governance-resolution`), which shows merged policy resolution for the current scope.

## Governance dashboard (`/governance/dashboard`)

The **Governance dashboard** (`archlucid-ui/src/app/governance/dashboard/page.tsx`) is a **cross-run, read-only** overview for operators:

| Area | Source |
|------|--------|
| Pending approvals | `GET /v1/governance/dashboard` → `pendingApprovals` / `pendingCount` (Draft + Submitted across runs; not filtered by run in the API) |
| Recent decisions | Same response → `recentDecisions` (Approved, Rejected, Promoted with review timestamps) |
| Policy pack changes | Same response → `recentChanges` (tenant-scoped rows from `PolicyPackChangeLog`) |

The page **auto-refreshes every 30 seconds**, shows **loading skeletons** on first load, and surfaces failures with **OperatorApiProblem**. Each pending card has **Review**, which navigates to **`/governance?runId={runId}`** so the run-scoped workflow opens with the run ID prefilled.

### Navigation and shortcuts

- **Shell** → **Alerts & governance** → **Dashboard** (`/governance/dashboard`), **before** **Governance workflow**.
- Global keyboard shortcut **Alt+G** opens the dashboard (registry: `shortcut-registry.ts`). **Graph** uses **Alt+Y** so **Alt+G** stays unambiguous for governance.

Types: `archlucid-ui/src/types/governance-dashboard.ts`. API helper: `getGovernanceDashboard` in `archlucid-ui/src/lib/api.ts`.

## Workflow steps

1. **Submit** — Operator enters run ID, manifest version, source/target deployment environments (mapped to API values `dev`, `test`, `prod`), and an optional comment. The UI calls **`POST /v1/governance/approval-requests`** (`submitApprovalRequest`). The backend stamps `requestedBy` from the authenticated actor.
2. **Review** — For rows in **`Submitted`** status, operators can **Approve** or **Reject** via inline forms (`reviewedBy`, optional `reviewComment`). These call **`POST /v1/governance/approval-requests/{id}/approve`** and **`.../reject`** (`approveRequest` / `rejectRequest`).
3. **Promote** — For **`Approved`** rows, **Promote** opens an inline panel with **`promotedBy`** (defaults from “Acting as”), optional notes, and **`POST /v1/governance/promotions`** (`promoteManifest`), linking **`approvalRequestId`** when present.
4. **Activate** — On each **promotion** timeline card, **Activate** calls **`POST /v1/governance/activations`** (`activateEnvironment`) for that promotion’s **run**, **manifest version**, and **target environment**. The JSON body only includes `runId`, `manifestVersion`, and `environment`; the UI also collects **`activatedBy`** in the “Acting as” field to satisfy the client contract (the API resolves the real actor from auth).

**Status values** (from `GovernanceApprovalStatus`): `Draft`, `Submitted`, `Approved`, `Rejected`, `Promoted`, `Activated`. The UI colors badges for the main lifecycle states; `Draft` uses a neutral style.

## Data loading

After **Load** for a run ID, the page fetches in parallel:

| Data | API |
|------|-----|
| Approval requests | `GET /v1/governance/runs/{runId}/approval-requests` |
| Promotions | `GET /v1/governance/runs/{runId}/promotions` |
| Activations | `GET /v1/governance/runs/{runId}/activations` |

Lists are sorted by time descending where applicable. **OperatorApiProblem** surfaces load failures; **OperatorEmptyState** covers valid empty results.

## Auth

The page uses the same **`api.ts`** helpers as the rest of the shell: browser requests go through **`/api/proxy`** with JWT when OIDC is enabled; server components / RSC paths use the configured API base URL, API key, and scope headers. No separate auth path is required for this page.

## Navigation

**Shell** → **Alerts & governance** → **Dashboard** (`/governance/dashboard`), then **Governance workflow** (`/governance`), between **Governance resolution** and **Audit log**.

## Related

- Types and API wrappers: `archlucid-ui/src/types/governance-workflow.ts`, `archlucid-ui/src/types/governance-dashboard.ts`, `archlucid-ui/src/lib/api.ts` (functions under `v1/governance`, including `getGovernanceDashboard`).
- Backend: `ArchLucid.Api/Controllers/Governance/GovernanceController.cs`, `GovernanceWorkflowService`.
