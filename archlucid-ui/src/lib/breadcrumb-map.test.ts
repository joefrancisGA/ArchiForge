import { describe, expect, it } from "vitest";

import { getBreadcrumbs } from "./breadcrumb-map";

describe("getBreadcrumbs", () => {
  it("returns only Home for root", () => {
    expect(getBreadcrumbs("/")).toEqual([{ label: "Home" }]);
  });

  it("shortens the new-run path to Home / New request", () => {
    expect(getBreadcrumbs("/runs/new")).toEqual([
      { label: "Home", href: "/" },
      { label: "New request" },
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

  it("uses policy-pack registry trail for governance-scoped pack routes (no workflow parent link)", () => {
    expect(getBreadcrumbs("/governance/policy-packs/undefined")).toEqual([
      { label: "Home", href: "/" },
      { label: "Policy packs", href: "/policy-packs" },
      { label: "Policy pack detail" },
    ]);
  });

  it("redirect target path breadcrumb resolves to registry only", () => {
    expect(getBreadcrumbs("/governance/policy-packs")).toEqual([
      { label: "Home", href: "/" },
      { label: "Policy packs", href: "/policy-packs" },
    ]);
  });

  it("labels showcase demo slug before uuid-style titles", () => {
    expect(getBreadcrumbs("/showcase/claims-intake-modernization")).toEqual([
      { label: "Home", href: "/" },
      { label: "Showcase", href: "/showcase" },
      { label: "Claims Intake Modernization" },
    ]);
  });

  it("labels E2E demo finding segment under Runs", () => {
    expect(
      getBreadcrumbs("/runs/e2e-fixture-run-001/findings/e2e-finding-001"),
    ).toEqual([
      { label: "Home", href: "/" },
      { label: "Runs", href: "/runs" },
      { label: "Claims Intake Modernization", href: "/runs/e2e-fixture-run-001" },
      { label: "Findings", href: "/runs/e2e-fixture-run-001/findings" },
      { label: "Demonstration finding" },
    ]);
  });
});
