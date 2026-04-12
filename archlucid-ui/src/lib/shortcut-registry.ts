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
    label: "New run",
    route: "/runs/new",
    description: "Open the guided run wizard",
  },
  {
    key: "alt+r",
    label: "Runs list",
    route: "/runs?projectId=default",
    description: "Browse architecture runs",
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
    description: "Replay authority chain",
  },
  {
    key: "alt+a",
    label: "Ask",
    route: "/ask",
    description: "Open Q&A",
  },
  {
    key: "alt+g",
    label: "Governance dashboard",
    route: "/governance/dashboard",
    description: "Open cross-run governance dashboard",
  },
  {
    key: "alt+y",
    label: "Graph",
    route: "/graph",
    description: "Open architecture graph",
  },
  {
    key: "alt+l",
    label: "Alerts",
    route: "/alerts",
    description: "Open alert inbox",
  },
  {
    key: "alt+h",
    label: "Home",
    route: "/",
    description: "Return to operator home",
  },
  {
    key: "shift+?",
    label: "Help",
    description: "Show keyboard shortcuts overlay",
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
    description: "Acknowledge the focused alert card (Alerts page)",
  },
  {
    key: "alt+2",
    label: "Resolve",
    description: "Resolve the focused alert card (Alerts page)",
  },
  {
    key: "alt+3",
    label: "Suppress",
    description: "Suppress the focused alert card (Alerts page)",
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
