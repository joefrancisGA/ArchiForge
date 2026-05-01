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
  it("renders sorted artifact names and download links on successful load", () => {
    const artifacts = [
      { ...sample, artifactId: "b", name: "zebra.txt" },
      { ...sample, artifactId: "a", name: "alpha.txt" },
    ];
    render(<ArtifactListTable manifestId="manifest-1" artifacts={artifacts} />);

    const rows = screen.getAllByRole("row");
    expect(rows.length).toBeGreaterThanOrEqual(3);

    const links = screen.getAllByRole("link", { name: "Download" });
    expect(links).toHaveLength(2);
    expect(links[0].getAttribute("href")).toContain("/api/proxy/v1/artifacts/manifests/manifest-1/artifact/");
  });

  it("renders headers with zero data rows when artifact list is empty", () => {
    render(<ArtifactListTable manifestId="manifest-1" artifacts={[]} />);

    expect(screen.getByRole("columnheader", { name: "Artifact" })).toBeInTheDocument();
    expect(screen.queryAllByRole("link", { name: "Preview" })).toHaveLength(0);
  });

  it("uses manifest-scoped Preview href when runId is omitted", () => {
    render(<ArtifactListTable manifestId="manifest-1" artifacts={[sample]} />);

    const preview = screen.getByRole("link", { name: "Preview" });
    expect(preview.getAttribute("href")).toBe(
      "/manifests/manifest-1/artifacts/artifact-guid-1",
    );
  });

  it("uses run-scoped Preview href when runId is set", () => {
    render(
      <ArtifactListTable manifestId="manifest-1" artifacts={[sample]} runId="run-guid-1" />,
    );

    const preview = screen.getByRole("link", { name: "Preview" });
    expect(preview.getAttribute("href")).toBe("/reviews/run-guid-1/artifacts/artifact-guid-1");
  });
});
