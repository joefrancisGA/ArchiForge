import { afterEach, describe, expect, it, vi } from "vitest";

import { getEffectiveBrowserProxyScopeHeaders, writeOperatorScopeToStorage } from "./operator-scope-storage";
import { DEV_SCOPE_PROJECT_ID, DEV_SCOPE_TENANT_ID, DEV_SCOPE_WORKSPACE_ID } from "./scope";

describe("operator-scope-storage", () => {
  afterEach(() => {
    vi.unstubAllEnvs();
    localStorage.clear();
  });

  it("getEffectiveBrowserProxyScopeHeaders_usesLocalStorageWhenAllIdsSet", () => {
    writeOperatorScopeToStorage({
      tenantId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
      workspaceId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
      projectId: "cccccccc-cccc-cccc-cccc-cccccccccccc",
      workspaceLabel: "WS",
      projectLabel: "PR",
    });
    const h = getEffectiveBrowserProxyScopeHeaders();
    expect(h["x-tenant-id"]).toBe("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    expect(h["x-workspace-id"]).toBe("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    expect(h["x-project-id"]).toBe("cccccccc-cccc-cccc-cccc-cccccccccccc");
  });

  it("getEffectiveBrowserProxyScopeHeaders_fallsBackToDevDefaultsWhenNoOverride", () => {
    const h = getEffectiveBrowserProxyScopeHeaders();
    expect(h["x-tenant-id"]).toBe(DEV_SCOPE_TENANT_ID);
    expect(h["x-workspace-id"]).toBe(DEV_SCOPE_WORKSPACE_ID);
    expect(h["x-project-id"]).toBe(DEV_SCOPE_PROJECT_ID);
  });
});
