import type { ReactNode } from "react";
import { render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";

import { OperatorEvidenceLimitsFooter } from "./OperatorEvidenceLimitsFooter";

vi.mock("next/link", () => ({
  default: ({
    href,
    children,
    className,
  }: {
    href: string;
    children: ReactNode;
    className?: string;
  }) => (
    <a href={href} className={className}>
      {children}
    </a>
  ),
}));

describe("OperatorEvidenceLimitsFooter", () => {
  it("always exposes provenance and aggregate explain links as readable anchors", () => {
    render(<OperatorEvidenceLimitsFooter runId="abc-123" />);

    expect(screen.getByTestId("operator-evidence-limits-footer")).toBeInTheDocument();

    const provenance = screen.getByRole("link", { name: /review trail \(provenance graph\)/i });

    expect(provenance).toHaveAttribute("href", "/reviews/abc-123/provenance");

    const explain = screen.getByRole("link", { name: /architecture review summary \(explain aggregate\)/i });

    expect(explain).toHaveAttribute("href", "/reviews/abc-123#run-explanation");
  });

  it("adds finding inspect link when finding id is provided", () => {
    render(<OperatorEvidenceLimitsFooter runId="run-z" findingIdForInspectLink="fid-9" />);

    const inspect = screen.getByRole("link", { name: /technical inspection trail/i });

    expect(inspect).toHaveAttribute("href", "/reviews/run-z/findings/fid-9/inspect");
  });

  it("shows fallback disclaimer only when API flag realModeFellBackToSimulator is true", () => {
    const { rerender } = render(
      <OperatorEvidenceLimitsFooter runId="r1" execution={{ realModeFellBackToSimulator: false }} />,
    );

    expect(screen.queryByTestId("operator-evidence-limits-fallback-disclaimer")).not.toBeInTheDocument();

    rerender(<OperatorEvidenceLimitsFooter runId="r1" execution={{}} />);

    expect(screen.queryByTestId("operator-evidence-limits-fallback-disclaimer")).not.toBeInTheDocument();

    rerender(
      <OperatorEvidenceLimitsFooter
        runId="r1"
        execution={{
          realModeFellBackToSimulator: true,
          pilotAoaiDeploymentSnapshot: "gpt-test",
        }}
      />,
    );

    expect(screen.getByTestId("operator-evidence-limits-fallback-disclaimer")).toHaveTextContent(
      /real-mode fallback/i,
    );
    expect(screen.getByTestId("operator-evidence-limits-fallback-disclaimer")).toHaveTextContent("gpt-test");
  });

  it("lists inspect metadata only when model deployment or prompt version strings are present", () => {
    const { rerender } = render(<OperatorEvidenceLimitsFooter runId="r1" inspectMetadata={{}} />);

    expect(screen.queryByTestId("operator-evidence-limits-inspect-metadata")).not.toBeInTheDocument();

    rerender(
      <OperatorEvidenceLimitsFooter
        runId="r1"
        inspectMetadata={{ modelDeploymentName: "dep-a", promptTemplateVersion: null }}
      />,
    );

    expect(screen.getByTestId("operator-evidence-limits-inspect-metadata")).toHaveTextContent(/dep-a/);

    rerender(
      <OperatorEvidenceLimitsFooter runId="r1" inspectMetadata={{ modelDeploymentName: "", promptTemplateVersion: "v3" }} />,
    );

    expect(screen.getByTestId("operator-evidence-limits-inspect-metadata")).toHaveTextContent(/v3/);
  });
});
