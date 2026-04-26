import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";

import { RunProvenanceInline } from "./RunProvenanceInline";

import type { RunSummary } from "@/types/authority";

const minimalRun = (overrides: Partial<RunSummary> = {}): RunSummary => ({
  runId: "00000000-0000-0000-0000-000000000001",
  projectId: "p",
  createdUtc: "2026-01-01T00:00:00.000Z",
  ...overrides,
});

describe("RunProvenanceInline", () => {
  it("renders four stage markers", () => {
    render(<RunProvenanceInline run={minimalRun({ hasContextSnapshot: true })} />);
    const list = screen.getByRole("list", { name: /pipeline artifact progress/i });

    expect(list.querySelectorAll("li")).toHaveLength(4);
  });
});
