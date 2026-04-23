> **Scope:** Operator UI — shared components (archlucid-ui) - full detail, tables, and links in the sections below.

> **Spine doc:** [Five-document onboarding spine](../FIRST_5_DOCS.md). Read this file only if you have a specific reason beyond those five entry documents.


# Operator UI — shared components (`archlucid-ui`)

**Purpose:** Document reusable React components that are not covered in depth by **`archlucid-ui/docs/COMPONENT_REFERENCE.md`** (shadcn primitives) but are important for operator workflows.

**Scope:** `archlucid-ui/src/components/` application-level wrappers. Primitives live under **`src/components/ui/`**.

---

## `ConfirmationDialog`

**File:** `archlucid-ui/src/components/ConfirmationDialog.tsx`  
**Underlying primitive:** Radix **`AlertDialog`** (`archlucid-ui/src/components/ui/alert-dialog.tsx`)

### Props (`ConfirmationDialogProps`)

| Prop | Type | Default | Notes |
|------|------|---------|--------|
| **`open`** | `boolean` | — | Controlled visibility. |
| **`onOpenChange`** | `(open: boolean) => void` | — | Fires when the dialog should close (e.g. cancel). |
| **`title`** | `string` | — | **`AlertDialogTitle`** |
| **`description`** | `string` | — | **`AlertDialogDescription`** |
| **`confirmLabel`** | `string` | `"Confirm"` | Primary action label (or **`Processing…`** + spinner when **`busy`**). |
| **`cancelLabel`** | `string` | `"Cancel"` | **`AlertDialogCancel`** |
| **`variant`** | `"destructive"` \| `"default"` | `"destructive"` | **`destructive`**: red/destructive action styling. **`default`**: neutral primary action. |
| **`onConfirm`** | `() => void` | — | Invoked when the confirm action is activated. |
| **`busy`** | `boolean` | `false` | Disables both actions; confirm shows a spinner and **Processing…**. |

### When to use

- **Destructive or high-impact actions** where accidental clicks must be avoided (governance **promote** / **activate**, future delete/archive flows).
- Prefer **`ConfirmationDialog`** over the generic **`Dialog`** when overlay-dismiss or casual dismissal would be unsafe — **`AlertDialog`** traps focus and does not treat outside-click as consent.

### Usage example

```tsx
const [open, setOpen] = useState(false);

return (
  <>
    <button type="button" onClick={() => setOpen(true)}>Promote</button>
    <ConfirmationDialog
      open={open}
      onOpenChange={setOpen}
      title="Promote manifest?"
      description="This will replace the active manifest in the target environment."
      variant="default"
      confirmLabel="Promote"
      busy={isSubmitting}
      onConfirm={() => {
        void submitPromote().finally(() => setOpen(false));
      }}
    />
  </>
);
```

**Tests:** `archlucid-ui/src/components/ConfirmationDialog.test.tsx`

---

## `RunProgressTracker`

**File:** `archlucid-ui/src/components/RunProgressTracker.tsx`

### Props (`RunProgressTrackerProps`)

| Prop | Type | Notes |
|------|------|--------|
| **`runId`** | `string` | Run whose summary is polled. |
| **`initialSummary`** | `RunSummary \| null` | Server-fetched snapshot from **`getRunSummary`**. If all four pipeline flags are already **`true`**, the component **renders nothing** and does not poll. |

### Polling behavior

- **Interval:** 3 seconds (`POLL_INTERVAL_MS`).
- **Source:** `getRunSummary(runId)` → **`GET /v1/authority/runs/{runId}/summary`**.
- **UI:** Same four-stage model as **`WizardStepTrack`**: **Context**, **Graph**, **Findings**, **Manifest** — each **`Badge`** **Ready** / **Pending**, plus a **`Progress`** bar (0–100% over four stages).

### Auto-stop conditions

1. **No polling:** Initial summary already has **`hasContextSnapshot`**, **`hasGraphSnapshot`**, **`hasFindingsSnapshot`**, and **`hasGoldenManifest`** all **`=== true`** → component returns **`null`** (run treated as pipeline-complete for this view).
2. **Success stop:** A poll returns all four flags **`true`** → polling stops; copy prompts the user to **refresh** for full run detail.
3. **Timeout stop:** After **3 minutes** from mount (`POLL_MAX_MS` = 180_000 ms) without all stages ready → polling stops; message asks the user to **refresh manually**.

Errors from **`getRunSummary`** during polling are ignored until timeout (same pattern as **`NewRunWizardClient`** polling).

### Server integration

The run detail page (`archlucid-ui/src/app/runs/[runId]/page.tsx`) loads an initial **`RunSummary`** on the server and renders **`RunProgressTracker`** only when the run is not yet fully reflected as complete (no **`goldenManifestId`** and/or summary flags incomplete).

**Tests:** `archlucid-ui/src/components/RunProgressTracker.test.tsx`

---

## `AuthorityPipelineTimeline`

**File:** `archlucid-ui/src/components/AuthorityPipelineTimeline.tsx`

### Props (`AuthorityPipelineTimelineProps`)

| Prop | Type | Notes |
|------|------|--------|
| **`items`** | `PipelineTimelineItem[] \| null` | From **`getRunPipelineTimeline`** → **`GET /v1/authority/runs/{runId}/pipeline-timeline`**. |
| **`loadErrorMessage`** | `string \| null \| undefined` | When set, shows a warning line instead of the list. |

### Behavior

- **Empty list:** Explains that no audit rows exist yet (normal for new runs or sparse auditing).
- **Rows:** Ordered list — **timestamp**, **`eventType`**, **actor**, optional **correlation** id.

The run detail page loads the timeline on the server (alongside other run sections).

**Tests:** `archlucid-ui/src/components/AuthorityPipelineTimeline.test.tsx`

---

## `ComplianceDriftChart`

**File:** `archlucid-ui/src/components/ComplianceDriftChart.tsx`

### Props (`ComplianceDriftChartProps`)

| Prop | Type | Notes |
|------|------|--------|
| **`points`** | `ComplianceDriftTrendPoint[]` | From **`getComplianceDriftTrend`** → **`GET /v1/governance/compliance-drift-trend`**. |

### Behavior

- **Empty `points`:** Renders a short message (“No compliance drift data for this period.”).
- **Chart:** CSS bar column layout (no extra charting dependency). Bar height is proportional to **`changeCount`** vs. the max in the series; non-zero bars have a **2px** minimum height. Each bar’s **`title`** summarizes counts and top **`changesByType`** keys.
- **Accessibility:** Root uses **`role="img"`** with an **`aria-label`** describing the trend.

### Where it is used

- **`archlucid-ui/src/app/governance/dashboard/page.tsx`** — “Compliance drift trend (last 30 days)” section (daily buckets).

**Tests:** `archlucid-ui/src/components/ComplianceDriftChart.test.tsx`

---

## Related documents

- [ACCESSIBILITY.md](ACCESSIBILITY.md) — Alert dialog focus behavior and live region for progress.
- [ACCESSIBILITY.md](../../ACCESSIBILITY.md) — WCAG baseline, axe CI, and general operator UI accessibility policy (repo root).
- `archlucid-ui/docs/COMPONENT_REFERENCE.md` — full component inventory for the shell.
