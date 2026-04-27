import { fireEvent, render, screen, within } from "@testing-library/react";
import { useFormContext } from "react-hook-form";
import { describe, expect, it, vi } from "vitest";

import { WizardStepPreset } from "@/components/wizard/steps/WizardStepPreset";
import { WizardFormTestHarness } from "@/components/wizard/wizard-form-test-utils";
import { wizardPresets } from "@/lib/wizard-presets";
import type { WizardFormValues } from "@/lib/wizard-schema";

function FormValuesProbe() {
  const { watch } = useFormContext<WizardFormValues>();
  const systemName = watch("systemName");

  return <span data-testid="probe-system">{systemName}</span>;
}

describe("WizardStepPreset", () => {
  it("renders start-from-scratch, industry starters, quick shapes, and import toggle", () => {
    render(
      <WizardFormTestHarness>
        <WizardStepPreset />
      </WizardFormTestHarness>,
    );

    expect(screen.getByRole("heading", { name: "Start your architecture request" })).toBeInTheDocument();

    expect(screen.getByRole("button", { name: "Start from scratch" })).toBeInTheDocument();

    for (const preset of wizardPresets) {
      expect(screen.getByText(preset.label)).toBeInTheDocument();
      expect(screen.getByText(preset.description)).toBeInTheDocument();
      expect(
        screen.getByRole("button", { name: `Use ${preset.label.toLowerCase()}` }),
      ).toBeInTheDocument();
    }

    expect(screen.getByRole("button", { name: /Import prepared request/i })).toBeInTheDocument();
  });

  it("calls onPresetSelect with the correct preset id when a quick-shape button is clicked", () => {
    const onPresetSelect = vi.fn();

    render(
      <WizardFormTestHarness>
        <WizardStepPreset onPresetSelect={onPresetSelect} />
      </WizardFormTestHarness>,
    );

    const greenfieldCard = screen.getByText("Greenfield web app").closest('[class*="rounded-xl"]');
    expect(greenfieldCard).toBeTruthy();
    const useBtn = within(greenfieldCard as HTMLElement).getByRole("button", { name: "Use greenfield web app" });
    fireEvent.click(useBtn);

    expect(onPresetSelect).toHaveBeenCalledTimes(1);
    expect(onPresetSelect).toHaveBeenCalledWith("greenfield-web-app");
  });

  it("merges preset values into the form when a quick shape is selected", () => {
    render(
      <WizardFormTestHarness>
        <WizardStepPreset />
        <FormValuesProbe />
      </WizardFormTestHarness>,
    );

    const modernizeCard = screen.getByText("Modernize legacy system").closest('[class*="rounded-xl"]');
    expect(modernizeCard).toBeTruthy();
    fireEvent.click(within(modernizeCard as HTMLElement).getByRole("button", { name: "Use modernize legacy system" }));

    expect(screen.getByTestId("probe-system")).toHaveTextContent("LegacyModernization");
  });

  it("Start from scratch resets to buildDefaultWizardValues system name", () => {
    render(
      <WizardFormTestHarness
        values={{
          systemName: "CustomBefore",
        }}
      >
        <WizardStepPreset />
        <FormValuesProbe />
      </WizardFormTestHarness>,
    );

    expect(screen.getByTestId("probe-system")).toHaveTextContent("CustomBefore");

    fireEvent.click(screen.getByRole("button", { name: "Start from scratch" }));

    expect(screen.getByTestId("probe-system")).toHaveTextContent("TargetSystem");
  });
});
