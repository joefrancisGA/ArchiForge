import { render, waitFor } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

vi.mock("@/lib/auth-config", () => ({
  AUTH_MODE: "development-bypass",
}));

vi.mock("@/lib/oidc/config", () => ({
  isJwtAuthMode: () => false,
}));

vi.mock("@/lib/oidc/session", () => ({
  isLikelySignedIn: () => true,
}));

const { routerReplace } = vi.hoisted(() => ({
  routerReplace: vi.fn(),
}));

vi.mock("next/navigation", () => ({
  useRouter: () => ({ replace: routerReplace }),
}));

import { TrialWelcomeRunDeepLink } from "./TrialWelcomeRunDeepLink";

describe("TrialWelcomeRunDeepLink", () => {
  beforeEach(() => {
    routerReplace.mockClear();
    window.sessionStorage.clear();
  });

  afterEach(() => {
    vi.unstubAllGlobals();
    vi.clearAllMocks();
    window.sessionStorage.clear();
  });

  it("redirects once to /runs/{id} when trialWelcomeRunId is present", async () => {
    const welcomeId = "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee";

    vi.stubGlobal(
      "fetch",
      vi.fn(async () => {
        return new Response(JSON.stringify({ trialWelcomeRunId: welcomeId }), {
          status: 200,
          headers: { "Content-Type": "application/json" },
        });
      }),
    );

    render(<TrialWelcomeRunDeepLink />);

    await waitFor(() => {
      expect(routerReplace).toHaveBeenCalledWith(`/runs/${welcomeId}`);
    });

    expect(window.sessionStorage.getItem("archlucid_trial_welcome_home_redirect_v1")).toBe(welcomeId);
  });

  it("does not redirect when session already recorded the same welcome run id", async () => {
    const welcomeId = "bbbbbbbb-cccc-dddd-eeee-ffffffffffff";
    window.sessionStorage.setItem("archlucid_trial_welcome_home_redirect_v1", welcomeId);

    const fetchMock = vi.fn(async () => {
      return new Response(JSON.stringify({ trialWelcomeRunId: welcomeId }), {
        status: 200,
        headers: { "Content-Type": "application/json" },
      });
    });

    vi.stubGlobal("fetch", fetchMock);

    render(<TrialWelcomeRunDeepLink />);

    await waitFor(() => {
      expect(fetchMock).toHaveBeenCalled();
    });

    expect(routerReplace).not.toHaveBeenCalled();
  });
});
