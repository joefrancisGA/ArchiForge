/**
 * Canonical keyboard shortcuts for the operator shell.
 *
 * **Why Alt (not Ctrl/Cmd)?** Ctrl/Cmd collide with browser chrome (new tab/window, copy, etc.).
 * Alt+letter is rarely bound in the page content area on Chrome/Edge/Firefox, which suits an
 * internal operator UI. See also `useKeyboardShortcuts.ts`.
 */
export type ShortcutEntry = {
  key: string;
  label: string;
  description: string;
  route?: string;
};

export const SHORTCUTS: ShortcutEntry[] = [
  {
    key: "alt+n",
    label: "New request",
    route: "/reviews/new",
    description: "Create request (new request wizard)",
  },
  {
    key: "alt+r",
    label: "Reviews list",
    route: "/reviews?projectId=default",
    description: "Open reviews",
  },
  {
    key: "alt+c",
    label: "Compare",
    route: "/compare",
    description: "Compare two runs",
  },
  {
    key: "alt+p",
    label: "Replay",
    route: "/replay",
    description: "Replay a run",
  },
  {
    key: "alt+a",
    label: "Ask",
    route: "/ask",
    description: "Open Ask",
  },
  {
    key: "alt+g",
    label: "Governance findings",
    route: "/governance/findings",
    description: "Open governance findings",
  },
  {
    key: "alt+y",
    label: "Graph",
    route: "/graph",
    description: "Open graph",
  },
  {
    key: "alt+l",
    label: "Alerts",
    route: "/alerts",
    description: "Open alerts",
  },
  {
    key: "alt+h",
    label: "Home",
    route: "/",
    description: "Open home",
  },
  {
    key: "shift+?",
    label: "Help",
    description: "Open help",
  },
];

/**
 * Maps a registry combo string to the `aria-keyshortcuts` / tooltip form (e.g. `alt+n` → `Alt+N`).
 * Keeps ShellNav and other hints aligned with `SHORTCUTS[].key`.
 */
export function registryKeyToAriaKeyShortcuts(combo: string): string {
  const parts = combo
    .split("+")
    .map((segment) => segment.trim())
    .filter((segment) => segment.length > 0);

  return parts
    .map((part) => {
      const lower = part.toLowerCase();

      if (lower === "?") {
        return "?";
      }

      if (lower.length === 1 && /^[a-z0-9]$/i.test(lower)) {
        return lower.toUpperCase();
      }

      return lower.charAt(0).toUpperCase() + lower.slice(1);
    })
    .join("+");
}

function normalizeCombo(combo: string): string {
  return combo.toLowerCase().trim();
}

export function findShortcutByKey(combo: string): ShortcutEntry | undefined {
  const needle = normalizeCombo(combo);

  return SHORTCUTS.find((entry) => normalizeCombo(entry.key) === needle);
}

/**
 * Page-scoped shortcuts (documented in the global help dialog). These do not use `route`; they apply only
 * in context (e.g. when an alert card has focus on `/alerts`).
 */
export type PageShortcutEntry = {
  key: string;
  label: string;
  description: string;
};

export const ALERTS_PAGE_SHORTCUTS: PageShortcutEntry[] = [
  {
    key: "alt+1",
    label: "Acknowledge",
    description:
      "Acknowledge the focused alert card on the Alerts page when Execute+ triage shortcuts are enabled in the shell",
  },
  {
    key: "alt+2",
    label: "Resolve",
    description:
      "Resolve the focused alert card on the Alerts page when Execute+ triage shortcuts are enabled in the shell",
  },
  {
    key: "alt+3",
    label: "Suppress",
    description:
      "Suppress the focused alert card on the Alerts page when Execute+ triage shortcuts are enabled in the shell",
  },
  {
    key: "alt+j",
    label: "Next alert",
    description: "Move focus to the next alert card (Alerts page)",
  },
  {
    key: "alt+k",
    label: "Previous alert",
    description: "Move focus to the previous alert card (Alerts page); stays on the first card",
  },
];
