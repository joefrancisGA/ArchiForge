import { describe, expect, it } from "vitest";

import { advisoryHubTabFromSearchParam } from "@/lib/advisory-hub-tab";

describe("advisoryHubTabFromSearchParam", () => {
  it("defaults to scans for missing, empty, scans, or unknown tab", () => {
    expect(advisoryHubTabFromSearchParam(null)).toBe("scans");
    expect(advisoryHubTabFromSearchParam("")).toBe("scans");
    expect(advisoryHubTabFromSearchParam("scans")).toBe("scans");
    expect(advisoryHubTabFromSearchParam("unknown")).toBe("scans");
  });

  it("resolves schedules", () => {
    expect(advisoryHubTabFromSearchParam("schedules")).toBe("schedules");
  });
});
