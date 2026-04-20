> **Scope:** Accessibility — operator UI patterns (supplement) - full detail, tables, and links in the sections below.

# Accessibility — operator UI patterns (supplement)

**Purpose:** Operator-shell patterns that complement the repo-wide accessibility baseline in **[`ACCESSIBILITY.md`](../ACCESSIBILITY.md)** (WCAG 2.1 AA target, axe Playwright gates, eslint-plugin-jsx-a11y).

---

## Destructive and high-impact confirmations (Radix Alert Dialog)

The **`ConfirmationDialog`** wrapper uses **`@radix-ui/react-alert-dialog`** (see **`archlucid-ui/src/components/ui/alert-dialog.tsx`**), not the generic **`Dialog`**:

- **Focus is trapped** inside the modal while it is open; focus returns to a sensible trigger when it closes.
- **No passive dismiss:** users cannot complete the flow by clicking the overlay. They must choose **Cancel** or the labeled confirm action — appropriate for **irreversible or significant** operations (e.g. governance **promote** / **activate**).
- **Primary vs neutral confirm:** **`variant="destructive"`** uses destructive emphasis on the confirm control; **`variant="default"`** uses neutral primary styling when the action is significant but not framed as destructive.

For general modal patterns (dismissible overlays, optional close), continue to use **`Dialog`** from **`components/ui/dialog.tsx`**.

---

## Live region — run pipeline progress

**`RunProgressTracker`** (`archlucid-ui/src/components/RunProgressTracker.tsx`) exposes pipeline status in a region with **`aria-live="polite"`** and **`aria-atomic="true"`** so screen readers announce updates when polling advances stages (e.g. “3 of 4 authority pipeline stages complete”, then completion or timeout messages) without interrupting mid-sentence like **`assertive`** would.

Details of intervals, stop conditions, and props: **[`docs/UI_COMPONENTS.md`](UI_COMPONENTS.md)**.

---

## Related documents

- [`ACCESSIBILITY.md`](../ACCESSIBILITY.md) — baseline policy, scanned routes, and expansion checklist.
- [`UI_COMPONENTS.md`](UI_COMPONENTS.md) — **`ConfirmationDialog`** and **`RunProgressTracker`** reference.
