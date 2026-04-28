import { describe, expect, it } from "vitest";

import { manifestStatusForDisplay } from "@/lib/manifest-status-display";

describe("manifestStatusForDisplay", () => {
  it("maps committed to Finalized", () => {
    expect(manifestStatusForDisplay("Committed")).toBe("Finalized");
    expect(manifestStatusForDisplay("committed")).toBe("Finalized");
  });

  it("passes through other statuses", () => {
    expect(manifestStatusForDisplay("Draft")).toBe("Draft");
  });

  it("returns em dash when empty", () => {
    expect(manifestStatusForDisplay("")).toBe("—");
    expect(manifestStatusForDisplay(null)).toBe("—");
  });
});
