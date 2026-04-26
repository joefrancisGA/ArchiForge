import { render, screen, waitFor } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

import { SystemHealthStatusStrip } from "./SystemHealthStatusStrip";

const originalFetch = globalThis.fetch;

describe("SystemHealthStatusStrip", () => {
  beforeEach(() => {
    globalThis.fetch = vi.fn(async (input: RequestInfo | URL) => {
      const url = typeof input === "string" ? input : input instanceof URL ? input.toString() : input.url;

      if (url.includes("/api/proxy/health/ready")) {
        return new Response(JSON.stringify({ status: "Healthy", entries: [] }), { status: 200 });
      }

      return new Response("not found", { status: 404 });
    }) as unknown as typeof fetch;
  });

  afterEach(() => {
    globalThis.fetch = originalFetch;
  });

  it("renders readiness strip", async () => {
    render(<SystemHealthStatusStrip />);

    await waitFor(() => {
      expect(screen.getByTestId("command-center-health-card")).toBeInTheDocument();
    });
    expect(screen.getByText(/platform services:/i)).toBeInTheDocument();
  });
});
