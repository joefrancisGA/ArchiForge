/**
 * First-run Core Pilot wizard — persisted preferences (resume + hide navigator).
 * Storage key namespaces schema version (`archlucid.corePilotWizard.v1`).
 */

export const CORE_PILOT_WIZARD_STORAGE_KEY = "archlucid.corePilotWizard.v1";
export const CORE_PILOT_WIZARD_OPEN_EVENT = "archlucid-core-pilot-wizard-open";

/** Matches V1_SCOPE §4.1 pilot path + welcome framing (seven screens). */
export const CORE_PILOT_WIZARD_STEP_COUNT = 7;

export type CorePilotWizardStatus = "in_progress" | "completed";

export type CorePilotWizardPreferences = {
  /** When true the floating launcher is suppressed (operator opted out permanently). */
  dontShowNavigator: boolean;
};

export type CorePilotWizardStateV1 = {
  schemaVersion: 1;
  /** Progress through the wizard. */
  status: CorePilotWizardStatus;
  /** 0-based index in `CORE_PILOT_WIZARD_STEP_COUNT` steps (clamped when parsing). */
  stepIndex: number;
  preferences: CorePilotWizardPreferences;
  updatedAtUtc: string;
};

export type CorePilotWizardAction =
  | { type: "next" }
  | { type: "back" }
  | { type: "goto"; stepIndex: number }
  /** Close dialog but retain step + in_progress (resume later). */
  | { type: "closePreserveProgress" }
  /** Completed all steps voluntarily. */
  | { type: "markCompleted" }
  /** Operator chose not to surface the FAB / launcher again. */
  | { type: "suppressNavigator" }
  | { type: "hydrate"; state: CorePilotWizardStateV1 };

function clampStep(step: number): number {
  if (!Number.isFinite(step) || !Number.isInteger(step)) {
    return 0;
  }

  return Math.max(0, Math.min(CORE_PILOT_WIZARD_STEP_COUNT - 1, step));
}

/** Pure state machine transitions for `'useReducer'` / tests. */
export function corePilotWizardReducer(
  prev: CorePilotWizardStateV1,
  action: CorePilotWizardAction,
): CorePilotWizardStateV1 {
  const nowUtc = new Date().toISOString();

  if (action.type === "hydrate") {
    return action.state;
  }

  switch (action.type) {
    case "next": {
      if (prev.status === "completed") {
        return { ...prev, updatedAtUtc: nowUtc };
      }

      if (prev.stepIndex >= CORE_PILOT_WIZARD_STEP_COUNT - 1) {
        return {
          ...prev,
          status: "completed",
          stepIndex: CORE_PILOT_WIZARD_STEP_COUNT - 1,
          updatedAtUtc: nowUtc,
        };
      }

      return {
        ...prev,
        status: "in_progress",
        stepIndex: prev.stepIndex + 1,
        updatedAtUtc: nowUtc,
      };
    }

    case "back": {
      if (prev.stepIndex <= 0) {
        return { ...prev, updatedAtUtc: nowUtc };
      }

      return {
        ...prev,
        status: "in_progress",
        stepIndex: prev.stepIndex - 1,
        updatedAtUtc: nowUtc,
      };
    }

    case "goto":
      return {
        ...prev,
        status: "in_progress",
        stepIndex: clampStep(action.stepIndex),
        updatedAtUtc: nowUtc,
      };

    case "closePreserveProgress":
      return {
        ...prev,
        status: prev.status === "completed" ? "completed" : "in_progress",
        updatedAtUtc: nowUtc,
      };

    case "markCompleted":
      return {
        ...prev,
        status: "completed",
        stepIndex: CORE_PILOT_WIZARD_STEP_COUNT - 1,
        updatedAtUtc: nowUtc,
      };

    case "suppressNavigator":
      return {
        ...prev,
        preferences: { dontShowNavigator: true },
        updatedAtUtc: nowUtc,
      };

    default:
      return prev;
  }
}

export function createInitialCorePilotWizardState(): CorePilotWizardStateV1 {
  return {
    schemaVersion: 1,
    status: "in_progress",
    stepIndex: 0,
    preferences: { dontShowNavigator: false },
    updatedAtUtc: new Date().toISOString(),
  };
}

/**
 * Parses localStorage blob; validates `schemaVersion` and clamps corrupt indices.
 * Returns `null` when missing or malformed (caller restores defaults).
 */
export function parseStoredCorePilotWizardState(raw: string | null): CorePilotWizardStateV1 | null {
  if (raw === null || raw.trim().length === 0) {
    return null;
  }

  let parsed: unknown;

  try {
    parsed = JSON.parse(raw);
  } catch {
    return null;
  }

  if (parsed === null || typeof parsed !== "object") {
    return null;
  }

  const o = parsed as Record<string, unknown>;

  if (o.schemaVersion !== 1) {
    return null;
  }

  const dontShow =
    typeof o.preferences === "object" &&
    o.preferences !== null &&
    typeof (o.preferences as Record<string, unknown>).dontShowNavigator === "boolean"
      ? (o.preferences as { dontShowNavigator: boolean }).dontShowNavigator
      : false;

  const statusUnknown = o.status;

  const migrated =
    statusUnknown === "idle" ? "in_progress" : statusUnknown === "completed" ? "completed" : "in_progress";

  const status: CorePilotWizardStatus = migrated;

  return {
    schemaVersion: 1,
    status,
    stepIndex: clampStep(typeof o.stepIndex === "number" ? (o.stepIndex as number) : 0),
    preferences: { dontShowNavigator: dontShow },
    updatedAtUtc: typeof o.updatedAtUtc === "string" ? o.updatedAtUtc : new Date().toISOString(),
  };
}

export function stringifyCorePilotWizardState(state: CorePilotWizardStateV1): string {
  return JSON.stringify(state);
}
