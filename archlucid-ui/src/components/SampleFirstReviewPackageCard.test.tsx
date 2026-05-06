import { fireEvent, render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";

const recordCorePilotRailChecklistStep = vi.fn();

vi.mock("@/lib/core-pilot-rail-telemetry", () => ({
  recordCorePilotRailChecklistStep: (stepIndex: number) => recordCorePilotRailChecklistStep(stepIndex),
}));

vi.mock("next/link", () => ({
  default: ({
    href,
    children,
    ...rest
  }: {
    href: string;
    children: import("react").ReactNode;
  } & Record<string, unknown>) => (
    <a href={href} {...rest}>
      {children}
    </a>
  ),
}));

import { SampleFirstReviewPackageCard } from "./SampleFirstReviewPackageCard";

describe("SampleFirstReviewPackageCard", () => {
  it("links to the curated sample review and labels demo evidence", () => {
    render(<SampleFirstReviewPackageCard />);

    expect(screen.getByRole("heading", { name: "Start with a completed architecture review package" })).toBeInTheDocument();
    expect(screen.getByText(/Illustrative sample review/i)).toBeInTheDocument();
    expect(screen.getByText("9")).toBeInTheDocument();
    expect(screen.getByText("12")).toBeInTheDocument();
    expect(screen.getByText("1")).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Start with sample review" })).toHaveAttribute(
      "href",
      "/reviews/claims-intake-modernization",
    );
    expect(screen.getByRole("link", { name: "Use my own input" })).toHaveAttribute("href", "/reviews/new");
  });

  it("records review-output telemetry when the sample is opened", () => {
    render(<SampleFirstReviewPackageCard />);

    fireEvent.click(screen.getByRole("link", { name: "Start with sample review" }));

    expect(recordCorePilotRailChecklistStep).toHaveBeenCalledWith(3);
  });
});
