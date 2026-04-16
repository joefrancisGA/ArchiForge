import { describe, expect, it } from "vitest";

import { getRouteTitle } from "./route-titles";

describe("getRouteTitle — static routes", () => {
  it("returns known titles", () => {
    expect(getRouteTitle("/")).toBe("Home");
    expect(getRouteTitle("/alerts")).toBe("Alerts");
    expect(getRouteTitle("/runs/new")).toBe("New run wizard");
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

describe("getRouteTitle — unknown path", () => {
  it("capitalizes last segment", () => {
    expect(getRouteTitle("/foo/bar-baz")).toBe("Bar baz");
  });

  it("strips trailing slash", () => {
    expect(getRouteTitle("/planning/")).toBe("Planning");
  });
});
