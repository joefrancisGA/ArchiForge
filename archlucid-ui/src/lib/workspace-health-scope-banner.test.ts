import { describe, expect, it } from "vitest";

import { formatExecutiveWorkspaceScopeDescription } from "@/lib/workspace-health-scope-banner";

describe("formatExecutiveWorkspaceScopeDescription", () => {
  it("uses stored labels when operator scope record is present", () => {
    const text = formatExecutiveWorkspaceScopeDescription(
      {
        tenantId: "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee",
        workspaceId: "11111111-2222-3333-4444-555555555555",
        projectId: "66666666-7777-8888-9999-aaaaaaaaaaaa",
        workspaceLabel: "North America",
        projectLabel: "Core platform",
      },
      { tenantId: "", workspaceId: "", projectId: "" },
    );

    expect(text).toContain("North America");
    expect(text).toContain("Core platform");
    expect(text).toContain("not a cross-workspace executive rollup");
  });

  it("falls back to header IDs when no operator record", () => {
    const text = formatExecutiveWorkspaceScopeDescription(null, {
      tenantId: "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee",
      workspaceId: "bbbbbbbb-cccc-dddd-eeee-ffffffffffff",
      projectId: "cccccccc-dddd-eeee-ffff-000000000000",
    });

    expect(text).toContain("aaaaaaaa…");
    expect(text).toContain("bbbbbbbb…");
  });
});
