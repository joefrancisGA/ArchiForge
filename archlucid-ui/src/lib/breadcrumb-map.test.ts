import { describe, expect, it } from "vitest";

import { getBreadcrumbs } from "./breadcrumb-map";

describe("getBreadcrumbs", () => {
  it("returns only Home for root", () => {
    expect(getBreadcrumbs("/")).toEqual([{ label: "Home" }]);
  });

  it("builds a chain for nested routes", () => {
    expect(getBreadcrumbs("/runs/new")).toEqual([
      { label: "Home", href: "/" },
      { label: "Runs", href: "/runs" },
      { label: "New run" },
    ]);
  });

  it("labels UUID run segments as Run detail", () => {
    const id = "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee";
    expect(getBreadcrumbs(`/runs/${id}`)).toEqual([
      { label: "Home", href: "/" },
      { label: "Runs", href: "/runs" },
      { label: "Run detail" },
    ]);
  });

  it("maps governance dashboard segments", () => {
    expect(getBreadcrumbs("/governance/dashboard")).toEqual([
      { label: "Home", href: "/" },
      { label: "Governance", href: "/governance" },
      { label: "Dashboard" },
    ]);
  });
});
