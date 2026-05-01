import { describe, expect, it } from "vitest";

import { onboardingTourAnchorForHref } from "./onboarding-tour";

describe("onboardingTourAnchorForHref", () => {
  it("maps new run and help hrefs", () => {
    expect(onboardingTourAnchorForHref("/reviews/new")).toBe("tour-new-run");
    expect(onboardingTourAnchorForHref("/help")).toBe("tour-help");
    expect(onboardingTourAnchorForHref("/reviews")).toBeUndefined();
  });
});
