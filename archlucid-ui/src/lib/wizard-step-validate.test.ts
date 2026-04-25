import { describe, expect, it } from "vitest";

import { buildDefaultWizardValues } from "@/lib/wizard-schema";

import { validateWizardStep } from "./wizard-step-validate";

describe("validateWizardStep", () => {
  it("returns errors for step 1 when systemName is empty", () => {
    const v = buildDefaultWizardValues();
    v.systemName = "";

    const err = validateWizardStep(1, v);
    expect(err.some((e) => e.field === "systemName")).toBe(true);
  });

  it("returns empty for step 1 when defaults are valid", () => {
    const v = buildDefaultWizardValues();
    expect(validateWizardStep(1, v)).toEqual([]);
  });
});
