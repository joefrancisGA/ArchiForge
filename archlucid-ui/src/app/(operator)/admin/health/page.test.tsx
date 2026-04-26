import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";

import AdminHealthPage from "./page";

function jsonResponse(data: unknown, status = 200) {
  return new Response(JSON.stringify(data), { status, headers: { "Content-Type": "application/json" } });
}

describe("AdminHealthPage", () => {
  it("renders readiness entries from /health/ready", async () => {
    const fetchMock = vi.fn(
      async (url: string | URL) => {
        const s = String(url);
        if (s.includes("health/ready")) {
          return jsonResponse({
            status: "Healthy",
            entries: [{ name: "database", status: "Healthy", durationMs: 12.3 }],
          });
        }
        if (s.includes("/version")) {
          return jsonResponse({ informationalVersion: "1.0.0+abc", commitSha: "abc123" });
        }
        if (s.includes("/api/proxy/health") && !s.includes("ready") && !s.includes("live")) {
          return jsonResponse({ status: "Healthy", entries: [] });
        }
        if (s.includes("operator-task-success-rates")) {
          return jsonResponse({
            windowNote: "n",
            firstRunCommittedTotal: 1,
            firstSessionCompletedTotal: 1,
            firstRunCommittedPerSessionRatio: 1,
          });
        }
        return new Response("n", { status: 404 });
      },
    );
    vi.stubGlobal("fetch", fetchMock);
    render(<AdminHealthPage />);
    expect(await screen.findByTestId("admin-health-ready-table")).toBeInTheDocument();
    expect(await screen.findByText("database")).toBeInTheDocument();
    expect(await screen.findByText("12 ms")).toBeInTheDocument();
    vi.unstubAllGlobals();
  });

  it("shows Degraded when overall readiness status is Degraded", async () => {
    const fetchMock = vi.fn(
      async (url: string | URL) => {
        const s = String(url);
        if (s.includes("/health/ready")) {
          return jsonResponse({ status: "Degraded", entries: [{ name: "database", status: "Degraded" }] });
        }
        if (s.includes("/version")) {
          return jsonResponse({});
        }
        if (s.includes("/api/proxy/health") && !s.includes("ready") && !s.includes("live")) {
          return jsonResponse({ status: "Healthy", entries: [] });
        }
        if (s.includes("operator-task-success-rates")) {
          return jsonResponse({
            windowNote: "n",
            firstRunCommittedTotal: 0,
            firstSessionCompletedTotal: 0,
            firstRunCommittedPerSessionRatio: 0,
          });
        }
        return new Response("n", { status: 404 });
      },
    );
    vi.stubGlobal("fetch", fetchMock);
    render(<AdminHealthPage />);
    const badge = await screen.findByTestId("admin-health-overall-badge");
    expect(badge.textContent).toMatch(/degraded/i);
    vi.unstubAllGlobals();
  });

  it("shows Unhealthy when overall readiness status is Unhealthy", async () => {
    const fetchMock = vi.fn(
      async (url: string | URL) => {
        const s = String(url);
        if (s.includes("/health/ready")) {
          return jsonResponse({ status: "Unhealthy", entries: [{ name: "database", status: "Unhealthy" }] });
        }
        if (s.includes("/version")) {
          return jsonResponse({});
        }
        if (s.includes("/api/proxy/health") && !s.includes("ready") && !s.includes("live")) {
          return new Response("forbidden", { status: 403 });
        }
        if (s.includes("operator-task-success-rates")) {
          return new Response("n", { status: 401 });
        }
        return new Response("n", { status: 404 });
      },
    );
    vi.stubGlobal("fetch", fetchMock);
    render(<AdminHealthPage />);
    const badge = await screen.findByTestId("admin-health-overall-badge");
    expect(badge.textContent).toMatch(/unhealthy/i);
    vi.unstubAllGlobals();
  });

  it("notes auth when /health returns 401", async () => {
    const fetchMock = vi.fn(
      async (url: string | URL) => {
        const s = String(url);
        if (s.includes("/health/ready")) {
          return jsonResponse({ status: "Healthy", entries: [] });
        }
        if (s.includes("/version")) {
          return jsonResponse({});
        }
        if (s.includes("/api/proxy/health") && !s.includes("ready") && !s.includes("live")) {
          return new Response("unauth", { status: 401 });
        }
        if (s.includes("operator-task-success-rates")) {
          return jsonResponse({
            windowNote: "n",
            firstRunCommittedTotal: 0,
            firstSessionCompletedTotal: 0,
            firstRunCommittedPerSessionRatio: 0,
          });
        }
        return new Response("n", { status: 404 });
      },
    );
    vi.stubGlobal("fetch", fetchMock);
    render(<AdminHealthPage />);
    expect(await screen.findByTestId("admin-health-circuit-note")).toHaveTextContent(/requires API authentication/i);
    vi.unstubAllGlobals();
  });

  it("renders operator task success rates table", async () => {
    const fetchMock = vi.fn(
      async (url: string | URL) => {
        const s = String(url);
        if (s.includes("/health/ready")) {
          return jsonResponse({ status: "Healthy", entries: [] });
        }
        if (s.includes("/version")) {
          return jsonResponse({});
        }
        if (s.includes("/api/proxy/health") && !s.includes("ready") && !s.includes("live")) {
          return jsonResponse({
            status: "Healthy",
            entries: [
              {
                name: "circuit_breakers",
                status: "Healthy",
                data: { gates: [{ name: "g1", state: "Closed", breakDurationSeconds: 20 }] },
              },
            ],
          });
        }
        if (s.includes("operator-task-success-rates")) {
          return jsonResponse({
            windowNote: "Process lifetime",
            firstRunCommittedTotal: 2,
            firstSessionCompletedTotal: 4,
            firstRunCommittedPerSessionRatio: 0.5,
          });
        }
        return new Response("n", { status: 404 });
      },
    );
    vi.stubGlobal("fetch", fetchMock);
    render(<AdminHealthPage />);
    const rates = await screen.findByTestId("admin-health-rates-table");
    expect(rates).toBeInTheDocument();
    expect(rates).toHaveTextContent("2");
    expect(rates).toHaveTextContent("4");
    vi.unstubAllGlobals();
  });

  it("refresh re-fetches", async () => {
    const fetchMock = vi.fn(
      async (url: string | URL) => {
        const s = String(url);
        if (s.includes("/health/ready")) {
          return jsonResponse({ status: "Healthy", entries: [] });
        }
        if (s.includes("/version")) {
          return jsonResponse({});
        }
        if (s.includes("/api/proxy/health") && !s.includes("ready") && !s.includes("live")) {
          return jsonResponse({ status: "Healthy", entries: [] });
        }
        if (s.includes("operator-task-success-rates")) {
          return jsonResponse({
            windowNote: "n",
            firstRunCommittedTotal: 0,
            firstSessionCompletedTotal: 0,
            firstRunCommittedPerSessionRatio: 0,
          });
        }
        return new Response("n", { status: 404 });
      },
    );
    vi.stubGlobal("fetch", fetchMock);
    render(<AdminHealthPage />);
    await waitFor(() => {
      expect(fetchMock.mock.calls.length).toBeGreaterThan(0);
    });
    const before = fetchMock.mock.calls.length;
    fireEvent.click(await screen.findByTestId("admin-health-refresh"));
    await waitFor(() => {
      expect(fetchMock.mock.calls.length).toBeGreaterThan(before);
    });
    vi.unstubAllGlobals();
  });
});
