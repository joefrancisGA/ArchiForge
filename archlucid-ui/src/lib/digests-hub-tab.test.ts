import { describe, expect, it } from "vitest";

import { digestsHubTabFromSearchParam } from "@/lib/digests-hub-tab";

describe("digestsHubTabFromSearchParam", () => {
  it("defaults to browse for missing, empty, browse, or unknown tab", () => {
    expect(digestsHubTabFromSearchParam(null)).toBe("browse");
    expect(digestsHubTabFromSearchParam("")).toBe("browse");
    expect(digestsHubTabFromSearchParam("browse")).toBe("browse");
    expect(digestsHubTabFromSearchParam("nope")).toBe("browse");
  });

  it("resolves subscriptions and schedule", () => {
    expect(digestsHubTabFromSearchParam("subscriptions")).toBe("subscriptions");
    expect(digestsHubTabFromSearchParam("schedule")).toBe("schedule");
  });
});
