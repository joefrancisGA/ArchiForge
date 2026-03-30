import { render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";

import { ArtifactListTable } from "./ArtifactListTable";

vi.mock("next/link", () => ({
  default: ({
    href,
    children,
  }: {
    href: string;
    children: import("react").ReactNode;
  }) => <a href={href}>{children}</a>,
}));

const sample = {
  artifactId: "artifact-guid-1",
  artifactType: "Inventory",
  name: "inventory.json",
  format: "json",
  createdUtc: "2020-01-01T00:00:00Z",
  contentHash: "abcdef123456",
};

describe("ArtifactListTable", () => {
  it("uses manifest-scoped Review href when runId is omitted", () => {
    render(<ArtifactListTable manifestId="manifest-1" artifacts={[sample]} />);

    const review = screen.getByRole("link", { name: "Review" });
    expect(review.getAttribute("href")).toBe(
      "/manifests/manifest-1/artifacts/artifact-guid-1",
    );
  });

  it("uses run-scoped Review href when runId is set", () => {
    render(
      <ArtifactListTable manifestId="manifest-1" artifacts={[sample]} runId="run-guid-1" />,
    );

    const review = screen.getByRole("link", { name: "Review" });
    expect(review.getAttribute("href")).toBe("/runs/run-guid-1/artifacts/artifact-guid-1");
  });
});
