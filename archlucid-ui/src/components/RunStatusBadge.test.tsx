import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";

import { RunStatusBadge, deriveRunListPipelineLabel } from "@/components/RunStatusBadge";
import type { RunSummary } from "@/types/authority";

const base: RunSummary = {
  runId: "00000000-0000-0000-0000-000000000001",
  projectId: "default",
  createdUtc: "2026-01-01T00:00:00.000Z",
};

describe("deriveRunListPipelineLabel", () => {
  it("returns Finalized when golden manifest flag is true", () => {
    expect(deriveRunListPipelineLabel({ ...base, hasGoldenManifest: true })).toBe("Finalized");
  });

  it("returns Ready to finalize when findings present but no manifest", () => {
    expect(
      deriveRunListPipelineLabel({
        ...base,
        hasFindingsSnapshot: true,
        hasGoldenManifest: false,
      }),
    ).toBe("Ready to finalize");
  });
});

describe("RunStatusBadge", () => {
  it("exposes pipeline status in aria-label via StatusPill", () => {
    render(<RunStatusBadge run={{ ...base, hasGoldenManifest: true }} />);

    expect(screen.getByLabelText(/Run pipeline status: Finalized/i)).toBeInTheDocument();
  });

  it("delegates to StatusPill pipeline domain (Finalized styling)", () => {
    const { container } = render(<RunStatusBadge run={{ ...base, hasGoldenManifest: true }} />);
    const pill = container.querySelector('[aria-label="Run pipeline status: Finalized"]');

    expect(pill).not.toBeNull();
    expect(pill?.className).toMatch(/rounded-full/);
    expect(pill?.className).toMatch(/emerald-600/);
  });
});
