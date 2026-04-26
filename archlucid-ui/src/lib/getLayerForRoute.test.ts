import { describe, expect, it } from "vitest";

import { getLayerForRoute } from "./getLayerForRoute";

describe("getLayerForRoute", () => {
  it("returns pilot for the home path", () => {
    expect(getLayerForRoute("/")).toBe("pilot");
  });

  it("returns operate-analysis for a known analysis nav path", () => {
    expect(getLayerForRoute("/ask")).toBe("operate-analysis");
    expect(getLayerForRoute("/search")).toBe("operate-analysis");
  });

  it("returns operator-admin for tenant admin nav paths", () => {
    expect(getLayerForRoute("/settings/tenant-cost")).toBe("operator-admin");
    expect(getLayerForRoute("/settings/baseline")).toBe("operator-admin");
    expect(getLayerForRoute("/settings/tenant")).toBe("operator-admin");
    expect(getLayerForRoute("/admin/support")).toBe("operator-admin");
    expect(getLayerForRoute("/admin/users")).toBe("operator-admin");
  });

  it("returns operate-governance for a known governance nav path and nested routes", () => {
    expect(getLayerForRoute("/alerts")).toBe("operate-governance");
    expect(getLayerForRoute("/governance")).toBe("operate-governance");
    expect(getLayerForRoute("/governance/approval-requests/1")).toBe("operate-governance");
    expect(getLayerForRoute("/governance/dashboard/weekly")).toBe("operate-governance");
    expect(getLayerForRoute("/governance/findings")).toBe("operate-governance");
    expect(getLayerForRoute("/audit")).toBe("operate-governance");
  });

  it("prefers the longer nav path when multiple prefixes could match (runs/new over runs)", () => {
    expect(getLayerForRoute("/runs/new")).toBe("pilot");
  });

  it("maps runs detail under the Runs list path", () => {
    expect(getLayerForRoute("/runs/550e8400-e29b-41d4-a716-446655440000")).toBe("pilot");
  });

  it("returns pilot for paths not in NAV_GROUPS", () => {
    expect(getLayerForRoute("/this-route-not-in-config")).toBe("pilot");
  });

  it("treats empty pathname as /", () => {
    expect(getLayerForRoute("")).toBe("pilot");
  });
});
