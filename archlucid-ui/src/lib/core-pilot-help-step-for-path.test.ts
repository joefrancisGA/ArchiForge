import { describe, expect, it } from "vitest";

import { corePilotHelpStepForPath } from "@/lib/core-pilot-help-step-for-path";
import { CORE_PILOT_STEPS } from "@/lib/core-pilot-steps";

describe("corePilotHelpStepForPath", () => {
  it("maps home and onboarding to step 0", () => {
    expect(corePilotHelpStepForPath("/")?.stepIndex).toBe(0);
    expect(corePilotHelpStepForPath("/onboarding")?.step.title).toBe(CORE_PILOT_STEPS[0].title);
  });

  it("maps new review wizard to step 0", () => {
    expect(corePilotHelpStepForPath("/reviews/new")?.stepIndex).toBe(0);
  });

  it("maps reviews list to step 1", () => {
    expect(corePilotHelpStepForPath("/reviews")?.stepIndex).toBe(1);
  });

  it("maps review detail to finalize step", () => {
    const ctx = corePilotHelpStepForPath("/reviews/abc");
    expect(ctx?.stepIndex).toBe(2);
    expect(ctx?.step.title).toBe(CORE_PILOT_STEPS[2].title);
  });

  it("returns null for unrelated routes", () => {
    expect(corePilotHelpStepForPath("/compare")).toBeNull();
  });
});
