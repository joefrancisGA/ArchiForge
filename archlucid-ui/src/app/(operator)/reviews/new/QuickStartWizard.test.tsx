import { zodResolver } from "@hookform/resolvers/zod";
import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { FormProvider, useForm } from "react-hook-form";
import { describe, expect, it, vi } from "vitest";

import { buildDefaultWizardValues, wizardFormSchema, type WizardFormValues } from "@/lib/wizard-schema";

const createRun = vi.fn();

vi.mock("@/lib/api", () => ({
  createArchitectureRun: (...args: unknown[]) => createRun(...args),
}));

vi.mock("@/lib/first-tenant-funnel-telemetry", () => ({
  recordFirstTenantFunnelEvent: vi.fn(),
}));

vi.mock("@/lib/toast", () => ({
  showSuccess: vi.fn(),
  showError: vi.fn(),
}));

import { QuickStartWizard } from "./QuickStartWizard";

function Harness() {
  const form = useForm<WizardFormValues>({
    resolver: zodResolver(wizardFormSchema),
    defaultValues: buildDefaultWizardValues(),
    mode: "onBlur",
  });

  return (
    <FormProvider {...form}>
      <QuickStartWizard
        onRunCreated={() => {
          /* test double */
        }}
      />
    </FormProvider>
  );
}

describe("QuickStartWizard", () => {
  it("shows three steps and submits createArchitectureRun with valid payload", async () => {
    createRun.mockResolvedValue({ run: { runId: "quick-run-1" } });

    render(<Harness />);

    expect(screen.getByTestId("quick-start-progress")).toHaveTextContent(/step 1 of 3/i);

    const system = screen.getByLabelText("System name");
    fireEvent.change(system, { target: { value: "MyRetailApp" } });

    fireEvent.click(screen.getByRole("button", { name: "Next" }));

    await waitFor(() => {
      expect(screen.getByTestId("quick-start-progress")).toHaveTextContent(/step 2 of 3/i);
    });

    const desc = screen.getByLabelText("Description");
    fireEvent.change(desc, {
      target: {
        value:
          "Ten char min: design a secure retail API on Azure with SQL, Redis, and App Service for the pilot scope.",
      },
    });

    fireEvent.click(screen.getByRole("button", { name: "Next" }));

    await waitFor(() => {
      expect(screen.getByTestId("quick-start-progress")).toHaveTextContent(/step 3 of 3/i);
    });

    fireEvent.click(screen.getByRole("button", { name: "Create request" }));

    await waitFor(() => {
      expect(createRun).toHaveBeenCalled();
    });

    const body = createRun.mock.calls[0][0] as { systemName: string; description: string };
    expect(body.systemName).toBe("MyRetailApp");
    expect(body.description.length).toBeGreaterThanOrEqual(10);
  });
});
