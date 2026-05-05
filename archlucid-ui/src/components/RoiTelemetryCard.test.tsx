import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";

import { RoiTelemetryCard } from "@/components/RoiTelemetryCard";

describe("RoiTelemetryCard", () => {
  it("hides USD controls for non-admin", () => {
    render(
      <RoiTelemetryCard
        domSuffix="t1"
        title="Test"
        windowLabel="30d"
        severity={{ critical: 1, high: 0, medium: 0 }}
        precommitBlocks={1}
        precommitBlocksExact
        isAdmin={false}
      />,
    );

    expect(screen.queryByLabelText(/Loaded engineering cost per hour/i)).toBeNull();
    expect(screen.getByText(/Model:/)).toBeInTheDocument();
  });

  it("shows USD controls for admin after mount", async () => {
    render(
      <RoiTelemetryCard
        domSuffix="t2"
        title="Test"
        windowLabel="30d"
        severity={{ critical: 0, high: 0, medium: 0 }}
        precommitBlocks={0}
        precommitBlocksExact
        isAdmin={true}
      />,
    );

    expect(await screen.findByLabelText(/Loaded engineering cost per hour/i)).toBeInTheDocument();
  });

  it("labels sampled pre-commit blocks", () => {
    render(
      <RoiTelemetryCard
        domSuffix="t3"
        title="Test"
        windowLabel="30d"
        severity={{ critical: 0, high: 0, medium: 0 }}
        precommitBlocks={400}
        precommitBlocksExact={false}
        isAdmin={false}
      />,
    );

    expect(screen.getByText(/400 \(sampled\)/)).toBeInTheDocument();
  });
});
