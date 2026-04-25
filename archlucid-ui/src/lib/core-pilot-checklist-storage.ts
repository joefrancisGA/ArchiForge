/**
 * Shared keys and helpers for the Core Pilot checklist on Home
 * (OperatorFirstRunWorkflowPanel). Used by hints that react when all steps are marked done.
 */

export const CORE_PILOT_STEP_COUNT = 4;

export const CORE_PILOT_CHECKLIST_CHANGED_EVENT = "archlucid-core-pilot-checklist-changed";

/** When set to `"1"`, `AfterCorePilotChecklistHint` stays hidden (operator dismissed the "what's next" panel). */
export const AFTER_CORE_PILOT_WHATS_NEXT_DISMISSED_KEY = "archlucid_after_core_pilot_whats_next_dismissed_v1";

export function readAfterCorePilotWhatsNextDismissed(): boolean {
  if (typeof window === "undefined") {
    return false;
  }
  try {
    return window.localStorage.getItem(AFTER_CORE_PILOT_WHATS_NEXT_DISMISSED_KEY) === "1";
  } catch {
    return false;
  }
}

export function corePilotStepDoneStorageKey(index: number): string {
  return `archlucid_onboarding_step_${index}_done`;
}

/** True when every checklist step has localStorage value "1". */
export function readCorePilotChecklistAllDone(): boolean {
  if (typeof window === "undefined") {
    return false;
  }

  try {
    for (let i = 0; i < CORE_PILOT_STEP_COUNT; i++) {
      if (window.localStorage.getItem(corePilotStepDoneStorageKey(i)) !== "1") {
        return false;
      }
    }

    return true;
  } catch {
    return false;
  }
}

export function emitCorePilotChecklistChanged(): void {
  if (typeof window === "undefined") {
    return;
  }

  try {
    window.dispatchEvent(new CustomEvent(CORE_PILOT_CHECKLIST_CHANGED_EVENT));
  } catch {
    /* ignore */
  }
}
