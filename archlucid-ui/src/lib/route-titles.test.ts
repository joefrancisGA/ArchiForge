import { describe, expect, it } from "vitest";

import { getRouteTitle } from "./route-titles";

describe("getRouteTitle — static routes", () => {
  it("returns known titles", () => {
    expect(getRouteTitle("/")).toBe("Home");
    expect(getRouteTitle("/alerts")).toBe("Alerts");
    expect(getRouteTitle("/runs/new")).toBe("New architecture request");
  });
});

describe("getRouteTitle — dynamic run detail", () => {
  it("returns Run detail for uuid path", () => {
    expect(getRouteTitle("/runs/e2e-fixture-run-001")).toBe("Run detail");
  });
});

describe("getRouteTitle — manifest detail", () => {
  it("returns Manifest detail", () => {
    expect(getRouteTitle("/manifests/abc-123")).toBe("Manifest detail");
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
