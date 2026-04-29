import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";

vi.mock("@/lib/toast", () => ({
  showError: vi.fn(),
  showSuccess: vi.fn()
}));

import { BaselineSettingsClient } from "./BaselineSettingsClient";

describe("BaselineSettingsPage", () => {
  it("renders form after load and submits valid values", async () => {
    const fetchMock = vi.fn(
      async (input: string | URL, init?: RequestInit) => {
        if (String(input).endsWith("/api/proxy/v1/tenant/baseline") && (!init || init.method === "GET" || !init.method)) {
          return new Response(
            JSON.stringify({
              manualPrepHoursPerReview: null,
              peoplePerReview: null,
              capturedUtc: null
            }),
            { status: 200, headers: { "Content-Type": "application/json" } }
          );
        }
        if (String(input).endsWith("/api/proxy/v1/tenant/baseline") && init?.method === "PUT") {
          return new Response(
            JSON.stringify({
              manualPrepHoursPerReview: 2,
              peoplePerReview: 3,
              capturedUtc: "2026-01-01T00:00:00Z"
            }),
            { status: 200, headers: { "Content-Type": "application/json" } }
          );
        }
        return new Response("not found", { status: 404 });
      }
    );
    vi.stubGlobal("fetch", fetchMock);
    render(<BaselineSettingsClient />);
    expect(await screen.findByTestId("baseline-manual-prep")).toBeInTheDocument();
    fireEvent.change(screen.getByTestId("baseline-manual-prep"), { target: { value: "2" } });
    fireEvent.change(screen.getByTestId("baseline-people"), { target: { value: "3" } });
    fireEvent.click(screen.getByTestId("baseline-save"));
    await waitFor(() => {
      const puts = fetchMock.mock.calls.filter((c) => (c[1] as RequestInit | undefined)?.method === "PUT");
      expect(puts.length).toBeGreaterThan(0);
    });
    vi.unstubAllGlobals();
  });
});
