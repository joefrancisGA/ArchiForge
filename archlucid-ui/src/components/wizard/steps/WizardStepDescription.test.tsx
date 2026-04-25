import { fireEvent, render, screen } from "@testing-library/react";
import { useFormContext } from "react-hook-form";
import { describe, expect, it } from "vitest";

import { WizardStepDescription } from "@/components/wizard/steps/WizardStepDescription";
import { WizardFormTestHarness } from "@/components/wizard/wizard-form-test-utils";
import { TooltipProvider } from "@/components/ui/tooltip";

function ValidateDescriptionButton() {
  const { trigger } = useFormContext();
  return (
    <button type="button" onClick={() => void trigger("description")}>
      validate-description
    </button>
  );
}

describe("WizardStepDescription", () => {
  it("shows a character counter that tracks description length", () => {
    render(
      <TooltipProvider delayDuration={0}>
        <WizardFormTestHarness>
          <WizardStepDescription />
        </WizardFormTestHarness>
      </TooltipProvider>,
    );

    const textarea = screen.getByLabelText("Description");
    const defaultLen = (textarea as HTMLTextAreaElement).value.length;
    expect(screen.getByText(new RegExp(`${defaultLen} / 4000`))).toBeInTheDocument();

    fireEvent.change(textarea, { target: { value: "1234567890" } });
    expect(screen.getByText(/10 \/ 4000/)).toBeInTheDocument();
  });

  it("adds and removes inline requirement rows", () => {
    render(
      <TooltipProvider delayDuration={0}>
        <WizardFormTestHarness>
          <WizardStepDescription />
        </WizardFormTestHarness>
      </TooltipProvider>,
    );

    expect(screen.queryByRole("button", { name: "Remove" })).not.toBeInTheDocument();

    fireEvent.click(screen.getByRole("button", { name: "Add requirement" }));
    const textareas = screen.getAllByRole("textbox");
    const inlineTa = textareas.find((el) => el !== screen.getByLabelText("Description"));
    expect(inlineTa).toBeTruthy();

    fireEvent.change(inlineTa as HTMLTextAreaElement, { target: { value: "Must support SSO" } });
    expect((inlineTa as HTMLTextAreaElement).value).toBe("Must support SSO");

    fireEvent.click(screen.getByRole("button", { name: "Remove" }));
    expect(screen.queryByRole("button", { name: "Remove" })).not.toBeInTheDocument();
  });

  it("exposes a validation error when description is shorter than 10 characters", async () => {
    render(
      <TooltipProvider delayDuration={0}>
        <WizardFormTestHarness>
          <WizardStepDescription />
          <ValidateDescriptionButton />
        </WizardFormTestHarness>
      </TooltipProvider>,
    );

    const textarea = screen.getByLabelText("Description");
    fireEvent.change(textarea, { target: { value: "short" } });
    fireEvent.blur(textarea);

    fireEvent.click(screen.getByRole("button", { name: "validate-description" }));

    expect(await screen.findByRole("alert")).toBeInTheDocument();
    expect(screen.getByText(/at least 10 characters/)).toBeInTheDocument();
  });
});
