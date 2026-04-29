import { describe, expect, it } from "vitest";

import {
  CORE_PILOT_WIZARD_STEP_COUNT,
  corePilotWizardReducer,
  createInitialCorePilotWizardState,
  parseStoredCorePilotWizardState,
} from "./core-pilot-wizard-state";

describe("corePilotWizardReducer", () => {
  it("next advances step without completing until the final step advance", () => {
    let s = createInitialCorePilotWizardState();

    expect(s.stepIndex).toBe(0);
    expect(s.status).toBe("in_progress");

    s = corePilotWizardReducer(s, { type: "next" });
    expect(s.stepIndex).toBe(1);
    expect(s.status).toBe("in_progress");
  });

  it("advancing on the final step completes the wizard", () => {
    let s = createInitialCorePilotWizardState();

    for (let i = 0; i < CORE_PILOT_WIZARD_STEP_COUNT - 1; i++) {
      s = corePilotWizardReducer(s, { type: "next" });
    }

    expect(s.stepIndex).toBe(CORE_PILOT_WIZARD_STEP_COUNT - 1);
    expect(s.status).toBe("in_progress");

    s = corePilotWizardReducer(s, { type: "next" });

    expect(s.status).toBe("completed");
    expect(s.stepIndex).toBe(CORE_PILOT_WIZARD_STEP_COUNT - 1);
  });

  it("does not advance beyond completed", () => {
    let s = createInitialCorePilotWizardState();

    for (let k = 0; k < CORE_PILOT_WIZARD_STEP_COUNT; k++) {
      s = corePilotWizardReducer(s, { type: "next" });
    }

    expect(s.status).toBe("completed");

    const again = corePilotWizardReducer(s, { type: "next" });

    expect(again.stepIndex).toBe(CORE_PILOT_WIZARD_STEP_COUNT - 1);
    expect(again.status).toBe("completed");
  });

  it("back from step zero is a noop on step index", () => {
    let s = createInitialCorePilotWizardState();
    s = corePilotWizardReducer(s, { type: "back" });

    expect(s.stepIndex).toBe(0);
  });

  it("suppressNavigator merges preferences flag", () => {
    let s = createInitialCorePilotWizardState();
    s = corePilotWizardReducer(s, { type: "suppressNavigator" });

    expect(s.preferences.dontShowNavigator).toBe(true);
  });

  it("goto clamps malformed indices via bounded step helpers", () => {
    let s = createInitialCorePilotWizardState();
    s = corePilotWizardReducer(s, { type: "goto", stepIndex: 99 });

    expect(s.stepIndex).toBe(CORE_PILOT_WIZARD_STEP_COUNT - 1);
  });

  it("hydrate replaces wholesale", () => {
    const seeded = corePilotWizardReducer(createInitialCorePilotWizardState(), { type: "goto", stepIndex: 3 });

    const wiped = corePilotWizardReducer(createInitialCorePilotWizardState(), { type: "hydrate", state: seeded });

    expect(wiped.stepIndex).toBe(3);
  });
});

describe("parseStoredCorePilotWizardState", () => {
  it("returns null on invalid JSON", () => {
    expect(parseStoredCorePilotWizardState("{")).toBeNull();
  });

  it("returns null when schemaVersion mismatches", () => {
    expect(parseStoredCorePilotWizardState(JSON.stringify({ schemaVersion: 2, stepIndex: 0 }))).toBeNull();
  });

  it("defaults corrupt stepIndex inputs to a safe clamp", () => {
    const raw = JSON.stringify({
      schemaVersion: 1,
      status: "in_progress",
      stepIndex: 9001,
      preferences: { dontShowNavigator: false },
      updatedAtUtc: new Date().toISOString(),
    });

    const s = parseStoredCorePilotWizardState(raw);

    expect(s).not.toBeNull();
    expect(s?.stepIndex).toBe(CORE_PILOT_WIZARD_STEP_COUNT - 1);
  });

  it('migrates legacy "idle" status to in_progress', () => {
    const raw = JSON.stringify({
      schemaVersion: 1,
      status: "idle",
      stepIndex: 2,
      preferences: { dontShowNavigator: false },
      updatedAtUtc: new Date().toISOString(),
    });

    expect(parseStoredCorePilotWizardState(raw)?.status).toBe("in_progress");
  });
});
