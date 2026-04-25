import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";

import type { ExecDigestPreferencesResponse, ExecDigestPreferencesUpsertRequest } from "@/types/exec-digest-preferences";

vi.mock("@/lib/toast", () => ({
  showError: vi.fn(),
  showSuccess: vi.fn(),
}));

vi.mock("@/components/OperatorNavAuthorityProvider", () => ({
  useOperatorNavAuthority: () => ({
    currentPrincipal: {
      provenance: "auth-me" as const,
      name: "Test User",
      roleClaimValues: ["Operator"],
      primaryAppRole: "Operator" as const,
      maxAuthority: "ExecuteAuthority" as const,
      authorityRank: 2,
      hasEnterpriseOperatorSurfaces: true,
    },
    callerAuthorityRank: 2,
    isAuthorityLoading: false,
  }),
}));

const { digestLoad, saveExecDigestMock } = vi.hoisted(() => {
  const d: ExecDigestPreferencesResponse = {
    schemaVersion: 1,
    tenantId: "t1",
    isConfigured: true,
    emailEnabled: false,
    recipientEmails: ["a@example.com"],
    ianaTimeZoneId: "Etc/UTC",
    dayOfWeek: 1,
    hourOfDay: 9,
    updatedUtc: "2026-01-01T00:00:00Z",
  };
  const save = vi.fn(async (body: ExecDigestPreferencesUpsertRequest): Promise<ExecDigestPreferencesResponse> => {
    return { ...d, ...body, updatedUtc: "2026-01-02T00:00:00Z" };
  });
  return { digestLoad: d, saveExecDigestMock: save };
});

vi.mock("@/lib/api", () => ({
  getExecDigestPreferences: vi.fn(() => Promise.resolve(digestLoad)),
  saveExecDigestPreferences: (b: ExecDigestPreferencesUpsertRequest) => saveExecDigestMock(b),
}));

import TenantSettingsPage from "./page";

describe("TenantSettingsPage", () => {
  it("renders and saves digest preferences", async () => {
    const fetchMock = vi.fn(
      async (input: string | URL) => {
        if (String(input).includes("/v1/tenant/trial-status")) {
          return new Response(JSON.stringify({ status: "Active", daysRemaining: 7 }), {
            status: 200,
            headers: { "Content-Type": "application/json" },
          });
        }
        return new Response("n", { status: 404 });
      },
    );
    vi.stubGlobal("fetch", fetchMock);
    render(<TenantSettingsPage />);
    expect(await screen.findByTestId("tenant-settings-page")).toBeInTheDocument();
    expect(await screen.findByText(/Status:/i)).toBeInTheDocument();
    const save = await screen.findByTestId("tenant-digest-save");
    fireEvent.click(save);
    await waitFor(() => {
      expect(saveExecDigestMock).toHaveBeenCalled();
    });
    vi.unstubAllGlobals();
  });
});
