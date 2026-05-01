import { act, fireEvent, render, screen, waitFor } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

/** Must match `NewRunWizardClient` — stored "quick" overrides `listRunsByProjectPaged` and hides the import path. */
const WIZARD_MODE_STORAGE_KEY = "archlucid_new_run_wizard_mode_v1";

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
  // Align with NewRunWizardClient mode bootstrap: a prior committed run → full 7-step wizard (import lives on step 0).
  listRunsByProjectPaged: vi.fn().mockResolvedValue({
    items: [
      {
        runId: "prior-run",
        projectId: "default",
        createdUtc: "2026-01-01T00:00:00.000Z",
        hasGoldenManifest: true,
      },
    ],
    totalCount: 1,
    page: 1,
    pageSize: 50,
    hasMore: false,
  }),
}));

import { NewRunWizardClient } from "./NewRunWizardClient";

describe("NewRunWizardClient (SECOND_RUN paste)", () => {
  beforeEach(() => {
    window.localStorage.setItem(WIZARD_MODE_STORAGE_KEY, "full");
  });

  afterEach(() => {
    window.localStorage.removeItem(WIZARD_MODE_STORAGE_KEY);
  });

  it("step 1 exposes paste path and applying TOML pre-fills system name on identity step", async () => {
    render(<NewRunWizardClient />);

    await waitFor(() => {
      expect(screen.queryByText("Loading wizard…")).not.toBeInTheDocument();
    });

    const importToggle = await screen.findByTestId("wizard-import-request-toggle");

    await act(async () => {
      fireEvent.click(importToggle);
    });

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

    await waitFor(() => {
      expect(screen.getByLabelText("System name")).toHaveValue("Pasted.Service");
    });
  });
});
