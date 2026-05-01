import { describe, expect, it } from "vitest";

import { effectiveNavDisclosureForPathname } from "./nav-disclosure-for-path";

describe("effectiveNavDisclosureForPathname", () => {
  it("forces essential tier on /reviews/new without changing stored preference semantics at call site", () => {
    expect(effectiveNavDisclosureForPathname("/reviews/new", true, true)).toEqual({
      showExtended: false,
      showAdvanced: false,
    });
  });

  it("passes through flags on other routes", () => {
    expect(effectiveNavDisclosureForPathname("/reviews", true, true)).toEqual({
      showExtended: true,
      showAdvanced: true,
    });
  });
});
