import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

/** Same values as `@/lib/scope` dev defaults (avoid vi.mock hoisting issues). */
const DEV_TENANT = "11111111-1111-1111-1111-111111111111";
const DEV_WORKSPACE = "22222222-2222-2222-2222-222222222222";
const DEV_PROJECT = "33333333-3333-3333-3333-333333333333";

vi.mock("@/components/OperatorNavAuthorityProvider", () => ({
  useOperatorNavAuthority: () => ({
    callerAuthorityRank: 2,
    isAuthorityLoading: false,
  }),
}));

vi.mock("@/lib/operator-scope-storage", async (importOriginal) => {
  const mod = await importOriginal<typeof import("@/lib/operator-scope-storage")>();

  return {
    ...mod,
    getEffectiveBrowserProxyScopeHeaders: vi.fn(() => ({
      "x-tenant-id": DEV_TENANT,
      "x-workspace-id": DEV_WORKSPACE,
      "x-project-id": DEV_PROJECT,
    })),
    readOperatorScopeFromStorage: vi.fn(() => null),
  };
});

import { ScopeSwitcher } from "@/components/ScopeSwitcher";

describe("ScopeSwitcher", () => {
  beforeEach(() => {
    vi.stubGlobal(
      "fetch",
      vi.fn(async () => new Response(JSON.stringify({ workspaces: [] }), { status: 200 })),
    );
  });

  afterEach(() => {
    vi.unstubAllGlobals();
    vi.clearAllMocks();
  });

  it("shows neutral workspace labels on the trigger when effective scope is dev defaults", () => {
    render(<ScopeSwitcher />);
    const trigger = screen.getByTestId("operator-scope-switcher-trigger");

    expect(trigger).toHaveTextContent("Workspace");
    expect(trigger).toHaveTextContent("Default project");
  });

  it("opens the panel and surfaces workspace list API guidance when list is empty", async () => {
    render(<ScopeSwitcher />);
    fireEvent.click(screen.getByTestId("operator-scope-switcher-trigger"));

    await waitFor(() => {
      expect(screen.getByTestId("operator-scope-switcher-panel")).toBeInTheDocument();
    });
    expect(await screen.findByTestId("operator-scope-list-note")).toHaveTextContent(/empty/i);
    expect(screen.getByText(new RegExp(DEV_TENANT.slice(0, 8), "i"))).toBeInTheDocument();
  });
});
