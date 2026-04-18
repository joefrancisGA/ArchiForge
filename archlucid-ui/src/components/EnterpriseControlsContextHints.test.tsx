import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";

import {
  EnterpriseExecutePlusPageCue,
  GovernanceDashboardReaderActionCue,
  GovernanceResolutionRankCue,
} from "./EnterpriseControlsContextHints";

describe("EnterpriseControlsContextHints (page-level rank cues)", () => {
  it("GovernanceResolutionRankCue renders operator-oriented line outside shell provider (default Admin rank)", () => {
    render(<GovernanceResolutionRankCue />);

    expect(
      screen.getByText(/Changing assignments uses policy packs or governance workflow/i),
    ).toBeInTheDocument();
  });

  it("GovernanceDashboardReaderActionCue renders nothing outside shell provider (default Admin rank)", () => {
    const { container } = render(<GovernanceDashboardReaderActionCue />);

    expect(container.querySelector('[role="note"]')).toBeNull();
  });

  it("EnterpriseExecutePlusPageCue renders message outside shell provider (default Admin rank)", () => {
    render(<EnterpriseExecutePlusPageCue message="Operator-only test line." />);

    expect(screen.getByText("Operator-only test line.")).toBeInTheDocument();
  });
});
