import { describe, expect, it } from "vitest";

import { getRouteTitle } from "./route-titles";

describe("getRouteTitle — static routes", () => {
  it("returns known titles", () => {
    expect(getRouteTitle("/")).toBe("Home");
    expect(getRouteTitle("/alerts")).toBe("Alerts");
    expect(getRouteTitle("/reviews/new")).toBe("New review");
  });
});

describe("getRouteTitle — dynamic review detail", () => {
  it("returns Review detail for uuid path", () => {
    expect(getRouteTitle("/reviews/e2e-fixture-run-001")).toBe("Review detail");
  });
});

describe("getRouteTitle — manifest detail", () => {
  it("returns Architecture package", () => {
    expect(getRouteTitle("/manifests/abc-123")).toBe("Architecture package");
  });
});

describe("getRouteTitle — governance policy pack detail", () => {
  it("returns Not found for leaked literal tokens in the path tail", () => {
    expect(getRouteTitle("/governance/policy-packs/undefined")).toBe("Not found");
    expect(getRouteTitle("/governance/policy-packs/null")).toBe("Not found");
  });

  it("returns Policy pack detail for normal ids", () => {
    expect(getRouteTitle("/governance/policy-packs/pack-1")).toBe("Policy pack detail");
  });
});

describe("getRouteTitle — unknown path", () => {
  it("capitalizes last segment", () => {
    expect(getRouteTitle("/foo/bar-baz")).toBe("Bar baz");
  });

  it("strips trailing slash", () => {
    expect(getRouteTitle("/planning/")).toBe("Planning");
  });
});

describe("getRouteTitle — executive shell", () => {
  it("returns Executive reviews for list", () => {
    expect(getRouteTitle("/executive/reviews")).toBe("Executive reviews");
  });

  it("returns Risk review for one review", () => {
    expect(getRouteTitle("/executive/reviews/run-abc")).toBe("Risk review");
  });

  it("returns Finding (executive) for finding detail", () => {
    expect(getRouteTitle("/executive/reviews/run-abc/findings/f-1")).toBe("Finding (executive)");
  });
});
