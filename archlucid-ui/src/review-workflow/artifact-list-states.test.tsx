import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";

import { OperatorEmptyState, OperatorWarningCallout } from "@/components/OperatorShellMessage";

/**
 * Mirrors run/manifest pages: artifact list failure vs valid empty list (no table).
 * Keeps copy aligned with OperatorShell usage in app routes.
 */
describe("Artifact list page states (55R smoke)", () => {
  it("failed artifact list: warning callout, no artifact table", () => {
    render(
      <main>
        <OperatorWarningCallout>
          <strong>Artifact list could not be loaded.</strong>
          <p style={{ margin: "8px 0 0" }}>Connection refused</p>
        </OperatorWarningCallout>
      </main>,
    );

    expect(screen.getByText(/Artifact list could not be loaded/)).toBeInTheDocument();
    expect(screen.getByText("Connection refused")).toBeInTheDocument();
    expect(screen.queryByRole("columnheader", { name: "Artifact" })).not.toBeInTheDocument();
  });

  it("successful empty artifact list: empty state copy used on run detail", () => {
    render(
      <main>
        <OperatorEmptyState title="No artifacts for this manifest">
          <p style={{ margin: 0 }}>The manifest exists but the API returned zero artifact descriptors.</p>
        </OperatorEmptyState>
      </main>,
    );

    expect(screen.getByText("No artifacts for this manifest")).toBeInTheDocument();
    expect(screen.getByText(/zero artifact descriptors/)).toBeInTheDocument();
  });
});
