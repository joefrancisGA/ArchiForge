import { render, screen, waitFor } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

vi.mock("@/lib/proxy-fetch-registration-scope", () => ({
  mergeRegistrationScopeForProxy: (init?: RequestInit) => init ?? {},
}));

import { BeforeAfterDeltaPanel } from "./BeforeAfterDeltaPanel";

type FetchHandler = (input: RequestInfo | URL) => Promise<Response>;

function jsonResponse(body: unknown, ok = true): Response {
  return {
    ok,
    json: async () => body,
  } as unknown as Response;
}

function urlOf(input: RequestInfo | URL): string {
  if (typeof input === "string") return input;
  if (input instanceof URL) return input.toString();

  return (input as Request).url;
}

function installFetch(handler: FetchHandler): void {
  vi.stubGlobal("fetch", vi.fn(handler));
}

describe("BeforeAfterDeltaPanel", () => {
  afterEach(() => {
    vi.unstubAllGlobals();
    vi.clearAllMocks();
  });

  it("renders nothing when neither baseline nor measured value is available", async () => {
    installFetch(async (input) => {
      const url = urlOf(input);

      if (url.includes("/v1/tenant/trial-status")) {
        return jsonResponse({
          trialWelcomeRunId: null,
          baselineReviewCycleHours: null,
        });
      }

      return jsonResponse({}, false);
    });

    const { container } = render(<BeforeAfterDeltaPanel />);

    await waitFor(() => {
      expect(vi.mocked(fetch)).toHaveBeenCalled();
    });

    expect(container.querySelector('[data-testid="before-after-delta-panel"]')).toBeNull();
  });

  it("renders both columns and a positive delta when baseline and measured values are present", async () => {
    installFetch(async (input) => {
      const url = urlOf(input);

      if (url.includes("/v1/tenant/trial-status")) {
        return jsonResponse({
          trialWelcomeRunId: "run-welcome-1",
          baselineReviewCycleHours: 16,
          baselineReviewCycleSource: "team estimate",
        });
      }

      if (url.includes("/v1/pilots/runs/run-welcome-1/pilot-run-deltas")) {
        return jsonResponse({
          timeToCommittedManifestTotalSeconds: 4 * 3600,
        });
      }

      return jsonResponse({}, false);
    });

    render(<BeforeAfterDeltaPanel />);

    await waitFor(() => {
      expect(screen.getByTestId("before-after-delta-panel")).toBeInTheDocument();
    });

    expect(screen.getByTestId("before-after-delta-baseline-hours")).toHaveTextContent("16.00 h");
    expect(screen.getByTestId("before-after-delta-measured-hours")).toHaveTextContent("4.00 h");
    expect(screen.getByTestId("before-after-delta-summary")).toHaveTextContent(/12\.00 h saved per run/);
    expect(screen.getByTestId("before-after-delta-summary")).toHaveTextContent(/75\.0% improvement/);
  });

  it("uses the explicit runId prop over trialWelcomeRunId from trial-status", async () => {
    const calls: string[] = [];

    installFetch(async (input) => {
      const url = urlOf(input);

      calls.push(url);

      if (url.includes("/v1/tenant/trial-status")) {
        return jsonResponse({
          trialWelcomeRunId: "should-not-be-used",
          baselineReviewCycleHours: 8,
        });
      }

      if (url.includes("/v1/pilots/runs/explicit-run/pilot-run-deltas")) {
        return jsonResponse({
          timeToCommittedManifestTotalSeconds: 2 * 3600,
        });
      }

      return jsonResponse({}, false);
    });

    render(<BeforeAfterDeltaPanel runId="explicit-run" />);

    await waitFor(() => {
      expect(screen.getByTestId("before-after-delta-summary")).toBeInTheDocument();
    });

    expect(calls.some((u) => u.includes("/runs/explicit-run/pilot-run-deltas"))).toBe(true);
    expect(calls.some((u) => u.includes("/runs/should-not-be-used/pilot-run-deltas"))).toBe(false);
  });

  it("renders the baseline-only state when no commit measurement is available", async () => {
    installFetch(async (input) => {
      const url = urlOf(input);

      if (url.includes("/v1/tenant/trial-status")) {
        return jsonResponse({
          trialWelcomeRunId: "run-welcome-1",
          baselineReviewCycleHours: 16,
        });
      }

      if (url.includes("/pilot-run-deltas")) {
        return jsonResponse({ timeToCommittedManifestTotalSeconds: null });
      }

      return jsonResponse({}, false);
    });

    render(<BeforeAfterDeltaPanel />);

    await waitFor(() => {
      expect(screen.getByTestId("before-after-delta-panel")).toBeInTheDocument();
    });

    expect(screen.getByTestId("before-after-delta-baseline-hours")).toHaveTextContent("16.00 h");
    expect(screen.getByTestId("before-after-delta-measured-hours")).toHaveTextContent("— h");
    expect(screen.queryByTestId("before-after-delta-summary")).toBeNull();
  });

  it("renders nothing when fetch throws", async () => {
    installFetch(async () => {
      throw new Error("network");
    });

    const { container } = render(<BeforeAfterDeltaPanel />);

    await waitFor(() => {
      expect(vi.mocked(fetch)).toHaveBeenCalled();
    });

    expect(container.querySelector('[data-testid="before-after-delta-panel"]')).toBeNull();
  });
});
