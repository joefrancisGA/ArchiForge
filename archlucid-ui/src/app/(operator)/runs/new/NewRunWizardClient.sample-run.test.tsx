import { render, screen } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

const WIZARD_MODE_STORAGE_KEY = "archlucid_new_run_wizard_mode_v1";

vi.mock("next/navigation", () => ({
  useSearchParams: () => ({
    get: (key: string) => (key === "sampleRunId" ? "6e8c4a10-2b1f-4c9a-9d3e-10b2a4f0c501" : null),
  }),
}));

vi.mock("next/link", () => ({
  default: ({
    href,
    children,
    ...rest
  }: {
    href: string;
    children: import("react").ReactNode;
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

describe("NewRunWizardClient (sampleRunId query)", () => {
  beforeEach(() => {
    window.localStorage.setItem(WIZARD_MODE_STORAGE_KEY, "full");
  });

  afterEach(() => {
    window.localStorage.removeItem(WIZARD_MODE_STORAGE_KEY);
  });

  it("shows trial sample callout on step 1 when sampleRunId is present", async () => {
    render(<NewRunWizardClient />);

    const link = await screen.findByTestId("wizard-open-trial-sample-run");
    expect(link).toHaveAttribute("href", "/runs/6e8c4a10-2b1f-4c9a-9d3e-10b2a4f0c501");
  });
});
