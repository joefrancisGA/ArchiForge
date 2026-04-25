import { redirect } from "next/navigation";
import { beforeEach, describe, expect, it, vi } from "vitest";

vi.mock("next/navigation", () => ({
  redirect: vi.fn(),
}));

import OnboardRedirectPage from "./onboard/page";
import OnboardingRedirectPage from "./onboarding/page";
import OnboardingStartRedirectPage from "./onboarding/start/page";

describe("legacy onboarding routes redirect to /getting-started", () => {
  beforeEach(() => {
    vi.mocked(redirect).mockClear();
  });

  it("redirects /onboarding to /getting-started", () => {
    OnboardingRedirectPage();
    expect(redirect).toHaveBeenCalledWith("/getting-started");
  });

  it("redirects /onboard to /getting-started", () => {
    OnboardRedirectPage();
    expect(redirect).toHaveBeenCalledWith("/getting-started");
  });

  it("redirects /onboarding/start to /getting-started and preserves query", async () => {
    await OnboardingStartRedirectPage({
      searchParams: Promise.resolve({ source: "registration" }),
    });
    expect(redirect).toHaveBeenCalledWith("/getting-started?source=registration");
  });
});
