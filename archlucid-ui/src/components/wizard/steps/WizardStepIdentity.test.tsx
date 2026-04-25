import { fireEvent, render, screen } from "@testing-library/react";
import { useFormContext } from "react-hook-form";
import { describe, expect, it } from "vitest";

import { WizardStepIdentity } from "@/components/wizard/steps/WizardStepIdentity";
import { WizardFormTestHarness } from "@/components/wizard/wizard-form-test-utils";
import { TooltipProvider } from "@/components/ui/tooltip";

function SystemNameErrorProbe() {
  const { trigger } = useFormContext();

  return (
    <div>
      <button type="button" onClick={() => void trigger("systemName")}>
        validate-system
      </button>
    </div>
  );
}

function IdentityHarness() {
  return (
    <TooltipProvider delayDuration={0}>
      <WizardFormTestHarness>
        <WizardStepIdentity />
        <SystemNameErrorProbe />
      </WizardFormTestHarness>
    </TooltipProvider>
  );
}

describe("WizardStepIdentity", () => {
  it("renders system name, environment, and cloud provider controls", () => {
    render(<IdentityHarness />);

    expect(screen.getByLabelText("System name")).toBeInTheDocument();
    expect(screen.getByText("Environment")).toBeInTheDocument();
    expect(screen.getByText("Cloud provider")).toBeInTheDocument();
    expect(screen.getByLabelText("Prior manifest version (optional)")).toBeInTheDocument();
  });

  it("shows Azure as the selected cloud and lists AWS and GCP as disabled coming-soon options", () => {
    render(<IdentityHarness />);

    const triggers = screen.getAllByRole("combobox");
    expect(triggers.length).toBeGreaterThanOrEqual(2);

    const cloudTrigger = triggers[1];
    expect(cloudTrigger).toHaveTextContent("Microsoft Azure");

    fireEvent.click(cloudTrigger);

    const aws = screen.getByRole("option", { name: /Amazon Web Services/i });
    const gcp = screen.getByRole("option", { name: /Google Cloud/i });
    expect(aws).toHaveAttribute("data-disabled");
    expect(gcp).toHaveAttribute("data-disabled");
  });

  it("surfaces a validation error when system name is cleared and validated", async () => {
    render(<IdentityHarness />);

    const input = screen.getByLabelText("System name");
    fireEvent.change(input, { target: { value: "" } });
    fireEvent.blur(input);

    fireEvent.click(screen.getByRole("button", { name: "validate-system" }));

    expect(await screen.findByText(/System name is required/i)).toBeInTheDocument();
  });
});
