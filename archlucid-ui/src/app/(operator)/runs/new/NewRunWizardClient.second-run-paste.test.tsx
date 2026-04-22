import { act, fireEvent, render, screen, waitFor } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";

vi.mock("next/navigation", () => ({
  useSearchParams: () => new URLSearchParams(),
}));

vi.mock("next/link", () => ({
  default: ({
    href,
    children,
    ...rest
  }: {
    href: string;
    children: React.ReactNode;
    className?: string;
  }) => (
    <a href={href} {...rest}>
      {children}
    </a>
  ),
}));

vi.mock("@/lib/api", () => ({
  createArchitectureRun: vi.fn(),
  getRunSummary: vi.fn(),
}));

import { NewRunWizardClient } from "./NewRunWizardClient";

describe("NewRunWizardClient (SECOND_RUN paste)", () => {
  it("step 1 exposes paste path and applying TOML pre-fills system name on identity step", async () => {
    render(<NewRunWizardClient />);

    expect(screen.getByTestId("second-run-paste-textarea")).toBeInTheDocument();

    const toml = `
name = "Pasted.Service"
description = "Ten chars min for the architecture goals here."
components = ["api"]
`;

    await act(async () => {
      fireEvent.change(screen.getByTestId("second-run-paste-textarea"), { target: { value: toml } });
      fireEvent.click(screen.getByTestId("second-run-apply-paste"));
    });

    await act(async () => {
      fireEvent.click(screen.getByRole("button", { name: "Next" }));
    });

    await waitFor(() => {
      expect(screen.getByLabelText("System name")).toHaveValue("Pasted.Service");
    });
  });
});
