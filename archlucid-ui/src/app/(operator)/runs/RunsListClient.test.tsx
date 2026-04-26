import { fireEvent, render, screen, within } from "@testing-library/react";
import { describe, expect, it } from "vitest";

import { RunsListClient } from "./RunsListClient";

import type { RunSummary } from "@/types/authority";

const sampleRun: RunSummary = {
  runId: "00000000-0000-0000-0000-000000000099",
  projectId: "default",
  description: "Demo",
  createdUtc: "2026-01-15T12:00:00.000Z",
  hasContextSnapshot: true,
  hasGraphSnapshot: false,
  hasFindingsSnapshot: true,
  hasGoldenManifest: false,
};

describe("RunsListClient inspector", () => {
  it("shows empty inspector copy until a row is selected", () => {
    render(
      <RunsListClient runs={[sampleRun]} projectId="default" page={1} pageSize={20} totalCount={1} />,
    );
    expect(screen.getByTestId("run-inspector-empty")).toBeInTheDocument();
  });

  it("opens inspector preview when a row is clicked (not the Open run link)", () => {
    render(
      <RunsListClient runs={[sampleRun]} projectId="default" page={1} pageSize={20} totalCount={1} />,
    );
    fireEvent.click(screen.getByTestId(`runs-row-${sampleRun.runId}`));
    const preview = screen.getByTestId("run-inspector-preview");
    expect(preview).toBeInTheDocument();
    expect(within(preview).getByText("Project")).toBeInTheDocument();
  });

  it("closes inspector when X is clicked", () => {
    render(
      <RunsListClient runs={[sampleRun]} projectId="default" page={1} pageSize={20} totalCount={1} />,
    );
    fireEvent.click(screen.getByTestId(`runs-row-${sampleRun.runId}`));
    expect(screen.getByTestId("run-inspector-preview")).toBeInTheDocument();
    fireEvent.click(screen.getByTestId("inspector-panel-close"));
    expect(screen.queryByTestId("run-inspector-preview")).toBeNull();
    expect(screen.getByTestId("run-inspector-empty")).toBeInTheDocument();
  });

  it("closes inspector on Escape after selection", () => {
    render(
      <RunsListClient runs={[sampleRun]} projectId="default" page={1} pageSize={20} totalCount={1} />,
    );
    fireEvent.click(screen.getByTestId(`runs-row-${sampleRun.runId}`));
    expect(screen.getByTestId("run-inspector-preview")).toBeInTheDocument();
    fireEvent.keyDown(window, { key: "Escape" });
    expect(screen.queryByTestId("run-inspector-preview")).toBeNull();
  });

  it("shows work-queue section for needs-attention runs", () => {
    render(
      <RunsListClient runs={[sampleRun]} projectId="default" page={1} pageSize={20} totalCount={1} />,
    );
    expect(screen.getByRole("heading", { name: /needs attention/i })).toBeInTheDocument();
    expect(within(screen.getByTestId(`runs-row-${sampleRun.runId}`)).getByTestId("run-provenance-inline")).toBeInTheDocument();
  });

  it("renders primary title as Untitled run when description is empty", () => {
    const untitled: RunSummary = {
      ...sampleRun,
      description: "   ",
    };
    render(<RunsListClient runs={[untitled]} projectId="default" page={1} pageSize={20} totalCount={1} />);
    expect(within(screen.getByTestId(`runs-row-${untitled.runId}`)).getByText("Untitled run")).toBeInTheDocument();
  });

  it("partitions multiple runs into ordered queue sections", () => {
    const committed: RunSummary = {
      ...sampleRun,
      runId: "00000000-0000-0000-0000-000000000001",
      hasFindingsSnapshot: true,
      hasGoldenManifest: true,
    };
    const inProgress: RunSummary = {
      ...sampleRun,
      runId: "00000000-0000-0000-0000-000000000002",
      hasFindingsSnapshot: false,
      hasGoldenManifest: false,
      hasGraphSnapshot: true,
    };

    render(
      <RunsListClient runs={[committed, sampleRun, inProgress]} projectId="default" page={1} pageSize={20} totalCount={3} />,
    );

    expect(screen.getByRole("heading", { name: /needs attention/i })).toBeInTheDocument();
    expect(screen.getByRole("heading", { name: /^in progress$/i })).toBeInTheDocument();
    expect(screen.getByRole("heading", { name: /^committed$/i })).toBeInTheDocument();
  });
});
