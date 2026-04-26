import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { axe, toHaveNoViolations } from "jest-axe";

import { StatusPill } from "./StatusPill";

expect.extend(toHaveNoViolations);

const pipelineStatuses = ["Finalized", "Ready to finalize", "In pipeline", "Starting"] as const;
const governanceStatuses = ["Submitted", "Approved", "Rejected", "Promoted", "Activated", "Draft"] as const;

describe("StatusPill", () => {
  it.each(pipelineStatuses)("renders pipeline status %s", (status) => {
    render(<StatusPill status={status} domain="pipeline" />);
    expect(screen.getByText(status)).toBeInTheDocument();
  });

  it.each(governanceStatuses)("renders governance status %s", (status) => {
    render(<StatusPill status={status} domain="governance" />);
    expect(screen.getByText(status)).toBeInTheDocument();
  });

  it("falls back for unknown status in general domain", () => {
    render(<StatusPill status="CustomThing" domain="general" />);
    expect(screen.getByText("CustomThing")).toBeInTheDocument();
  });

  it("uses default aria-label Status: {status}", () => {
    render(<StatusPill status="Healthy" domain="health" />);
    expect(screen.getByLabelText("Status: Healthy")).toBeInTheDocument();
  });

  it("respects custom ariaLabel", () => {
    render(<StatusPill status="Open" domain="health" ariaLabel="Breaker: Open" />);
    expect(screen.getByLabelText("Breaker: Open")).toBeInTheDocument();
  });

  it("has no serious axe violations in light and dark shells", async () => {
    const { container: lightRoot } = render(
      <div className="bg-neutral-50 p-4">
        <StatusPill status="Healthy" domain="health" />
        <StatusPill status="Submitted" domain="governance" />
        <StatusPill status="Finalized" domain="pipeline" />
        <StatusPill status="Unknown" domain="general" />
      </div>,
    );
    expect(await axe(lightRoot)).toHaveNoViolations();

    const { container: darkRoot } = render(
      <div className="dark bg-neutral-950 p-4 text-neutral-100">
        <StatusPill status="Unhealthy" domain="health" />
        <StatusPill status="Rejected" domain="governance" />
        <StatusPill status="In pipeline" domain="pipeline" />
      </div>,
    );
    expect(await axe(darkRoot)).toHaveNoViolations();
  });
});
