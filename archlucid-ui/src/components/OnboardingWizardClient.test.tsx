import { fireEvent, render, screen } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";

import { OnboardingWizardClient } from "@/components/OnboardingWizardClient";

describe("OnboardingWizardClient", () => {
  afterEach(() => {
    vi.restoreAllMocks();
    localStorage.clear();
  });

  it("advances steps and persists step index", async () => {
    render(<OnboardingWizardClient />);

    await screen.findByRole("button", { name: "Next" });

    fireEvent.click(screen.getByRole("button", { name: "Next" }));

    expect(await screen.findByText("Authentication")).toBeInTheDocument();
    expect(localStorage.getItem("archlucid_onboarding_wizard_step")).toBe("1");
  });

  it("mark complete sets completion flag", async () => {
    localStorage.setItem("archlucid_onboarding_wizard_step", "4");
    render(<OnboardingWizardClient />);

    expect(await screen.findByText("First run & demo")).toBeInTheDocument();

    fireEvent.click(screen.getByRole("button", { name: "Mark complete" }));

    expect(localStorage.getItem("archlucid_onboarding_wizard_completed")).toBe("1");
  });
});
