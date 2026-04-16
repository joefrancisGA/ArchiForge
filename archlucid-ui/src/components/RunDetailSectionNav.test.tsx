import { render, screen } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

import { RunDetailSectionNav } from "@/components/RunDetailSectionNav";

describe("RunDetailSectionNav", () => {
  beforeEach(() => {
    vi.stubGlobal(
      "IntersectionObserver",
      class {
        observe = vi.fn();

        disconnect = vi.fn();

        takeRecords = vi.fn().mockReturnValue([]);

        constructor(cb: IntersectionObserverCallback, opts?: IntersectionObserverInit) {
          void cb;
          void opts;
        }
      },
    );
  });

  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it("renders section links when at least three sections are available", () => {
    render(
      <RunDetailSectionNav
        sections={[
          { id: "run-metadata", label: "Run", available: true },
          { id: "pipeline-timeline", label: "Timeline", available: true },
          { id: "run-actions", label: "Actions", available: true },
        ]}
      />,
    );

    expect(screen.getByRole("link", { name: "Run" })).toHaveAttribute("href", "#run-metadata");
    expect(screen.getByRole("link", { name: "Timeline" })).toHaveAttribute("href", "#pipeline-timeline");
  });

  it("returns null when fewer than three sections are available", () => {
    const { container } = render(
      <RunDetailSectionNav
        sections={[
          { id: "a", label: "A", available: true },
          { id: "b", label: "B", available: true },
          { id: "c", label: "C", available: false },
        ]}
      />,
    );

    expect(container.firstChild).toBeNull();
  });
});
