import { render, screen } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";

import AdminUsersPage from "./page";

describe("AdminUsersPage", () => {
  afterEach(() => {
    vi.unstubAllGlobals();
  });

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
        await screen.findByText(/User directory unavailable/i, {}, { timeout: 5_000 }),
      ).toBeInTheDocument();

      expect(screen.getByTestId("admin-users-api-note")).toBeInTheDocument();
  });

  it("renders user rows when GET /v1/admin/users succeeds", async () => {
    const payload = {
      users: [
        { userId: "u1", displayName: "Ada Lovelace", email: "ada@example.com", role: "Admin" },
        { userId: "u2", displayName: "Bob", email: "bob@example.com", authorityRank: 1 },
      ],
    };
    vi.stubGlobal(
      "fetch",
      vi.fn(async (input: string | URL) => {
        if (String(input).includes("/v1/admin/users")) {
          return new Response(JSON.stringify(payload), {
            status: 200,
            headers: { "Content-Type": "application/json" },
          });
        }
        return new Response("n", { status: 404 });
      }),
    );
    render(<AdminUsersPage />);
    expect(await screen.findByText("Ada Lovelace")).toBeInTheDocument();
    expect(screen.getByText("bob@example.com")).toBeInTheDocument();
    expect(screen.getByText("Admin")).toBeInTheDocument();
    expect(screen.getByText(/Rank 1/i)).toBeInTheDocument();
  });
});
