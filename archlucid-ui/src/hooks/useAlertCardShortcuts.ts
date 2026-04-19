"use client";

import { useMemo } from "react";

import { useKeyboardShortcuts, type KeyboardShortcutsMap } from "./useKeyboardShortcuts";

export type UseAlertCardShortcutsOptions = {
  /** Invoked with the same action names the alerts API expects: Acknowledge, Resolve, Suppress. */
  onAction: (alertId: string, action: string) => void;
  /**
   * When false, Alt+1/2/3 are not registered (read-tier principals still use J/K to move between cards).
   * The alerts inbox should pass **`useEnterpriseMutationCapability()`** so shortcuts match the same Execute+ floor as
   * triage **Confirm**; page buttons may still open a read-only preview when false, without binding triage hotkeys.
   */
  mutationsEnabled?: boolean;
};

function getAlertCardFromActiveElement(): HTMLElement | null {
  const active = document.activeElement;

  if (!(active instanceof HTMLElement)) {
    return null;
  }

  // Prefer the card root so Alt+1 works when focus landed on an action button inside the card.
  return active.closest<HTMLElement>("[data-alert-id]");
}

function getFocusedAlertId(): string | null {
  const card = getAlertCardFromActiveElement();

  if (card === null) {
    return null;
  }

  const id = card.getAttribute("data-alert-id");

  if (id === null || id === "") {
    return null;
  }

  return id;
}

function focusAdjacentAlertCard(delta: number): void {
  const nodes = Array.from(document.querySelectorAll<HTMLElement>("[data-alert-id]"));

  if (nodes.length === 0) {
    return;
  }

  const current = getAlertCardFromActiveElement();
  const idx = current !== null ? nodes.indexOf(current) : -1;

  if (idx < 0) {
    return;
  }

  let nextIdx = idx + delta;

  if (nextIdx < 0) {
    nextIdx = 0;
  } else if (nextIdx >= nodes.length) {
    nextIdx = 0;
  }

  nodes[nextIdx]?.focus();
}

/**
 * Alerts page: Alt+1/2/3 act on the focused (or containing) alert card; Alt+J/K move focus between cards.
 * Skips when focus is not inside a `[data-alert-id]` card (or when `useKeyboardShortcuts` blocks inputs).
 */
export function useAlertCardShortcuts(options: UseAlertCardShortcutsOptions): void {
  const onAction = options.onAction;
  const mutationsEnabled = options.mutationsEnabled !== false;

  const map = useMemo((): KeyboardShortcutsMap => {
    const navigation: KeyboardShortcutsMap = {
      "alt+j": {
        description: "Focus next alert card",
        handler: () => {
          focusAdjacentAlertCard(1);
        },
      },
      "alt+k": {
        description: "Focus previous alert card",
        handler: () => {
          focusAdjacentAlertCard(-1);
        },
      },
    };

    if (!mutationsEnabled) {
      return navigation;
    }

    return {
      "alt+1": {
        description: "Acknowledge focused alert",
        handler: () => {
          const id = getFocusedAlertId();

          if (id !== null) {
            onAction(id, "Acknowledge");
          }
        },
      },
      "alt+2": {
        description: "Resolve focused alert",
        handler: () => {
          const id = getFocusedAlertId();

          if (id !== null) {
            onAction(id, "Resolve");
          }
        },
      },
      "alt+3": {
        description: "Suppress focused alert",
        handler: () => {
          const id = getFocusedAlertId();

          if (id !== null) {
            onAction(id, "Suppress");
          }
        },
      },
      ...navigation,
    };
  }, [onAction, mutationsEnabled]);

  useKeyboardShortcuts(map);
}
