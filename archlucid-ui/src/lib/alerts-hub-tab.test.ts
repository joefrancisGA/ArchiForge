import { describe, expect, it } from "vitest";

import { ALERT_HUB_TAB_IDS, alertHubTabFromSearchParam } from "./alerts-hub-tab";

describe("alertHubTabFromSearchParam", () => {
  it("defaults to inbox when param is null or empty", () => {
    expect(alertHubTabFromSearchParam(null)).toBe("inbox");
    expect(alertHubTabFromSearchParam("")).toBe("inbox");
  });

  it("maps each known ?tab= value to the same id", () => {
    for (const id of ALERT_HUB_TAB_IDS) {
      expect(alertHubTabFromSearchParam(id)).toBe(id);
    }
  });

  it("falls back to inbox for unknown tab values", () => {
    expect(alertHubTabFromSearchParam("widgets")).toBe("inbox");
    expect(alertHubTabFromSearchParam("tuning")).toBe("inbox");
  });
});
