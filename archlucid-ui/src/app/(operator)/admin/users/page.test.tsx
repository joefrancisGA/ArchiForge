import { render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";

import AdminUsersPage from "./page";

describe("AdminUsersPage", () => {
  it("shows API note when user list is unavailable", async () => {
    const fetchMock = vi.fn(async (input: string | URL) => {
      if (String(input).includes("/v1/admin/users")) {
        return new Response("not found", { status: 404 });
      }
      return new Response("n", { status: 404 });
    });
    vi.stubGlobal("fetch", fetchMock);
    render(<AdminUsersPage />);
    expect(
      await screen.findByText(/User management API endpoints are required to enable editing/i, {}, { timeout: 5_000 }),
    ).toBeInTheDocument();
    vi.unstubAllGlobals();
  });
});
