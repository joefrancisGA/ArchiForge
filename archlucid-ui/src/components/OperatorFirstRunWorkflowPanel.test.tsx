import { fireEvent, render, screen } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";

import {
  CORE_PILOT_FIRST_REVIEW_HEADING,
  CORE_PILOT_FIRST_REVIEW_MINIMIZED_BUTTON,
  CORE_PILOT_RUN_BRIDGE_LINE,
  CORE_PILOT_WORKFLOW_SUMMARY_LINE,
} from "@/lib/core-pilot-first-review-copy";

import { OperatorFirstRunWorkflowPanel } from "./OperatorFirstRunWorkflowPanel";

describe("OperatorFirstRunWorkflowPanel", () => {
  afterEach(() => {
    localStorage.clear();
    vi.restoreAllMocks();
  });

  it("after hydrate shows workflow heading and primary wizard link", async () => {
    render(<OperatorFirstRunWorkflowPanel />);

    const heading = await screen.findByRole("heading", { name: CORE_PILOT_FIRST_REVIEW_HEADING });
    expect(heading).toBeInTheDocument();

    const section = heading.closest("section");
    expect(section).toHaveTextContent(CORE_PILOT_WORKFLOW_SUMMARY_LINE);
    expect(section).toHaveTextContent(CORE_PILOT_RUN_BRIDGE_LINE);
    expect(section).toHaveTextContent("architecture review package");

    expect(screen.getByText("Start here")).toBeInTheDocument();

    const wizard = screen.getByRole("link", { name: "Start new request" });
    expect(wizard).toHaveAttribute("href", "/reviews/new");
  });

  it(
    "accordion toggles step body when clicking the step title",
    async () => {
      render(<OperatorFirstRunWorkflowPanel />);

      await screen.findByRole("heading", { name: CORE_PILOT_FIRST_REVIEW_HEADING });

      expect(screen.getByRole("link", { name: "Start new request" })).toBeVisible();

      const step2Title = screen.getByRole("button", { name: /Step 2 —/i });
      fireEvent.click(step2Title);

      expect(screen.getByRole("link", { name: "Open reviews list" })).toBeVisible();

      expect(screen.getByRole("button", { name: /Finalize the review package/i })).toBeInTheDocument();

      const step1Title = screen.getByRole("button", { name: /Step 1 —/i });
      fireEvent.click(step1Title);

      expect(await screen.findByRole("link", { name: "Start new request" })).toBeVisible();
    },
    20_000,
  );

  it("hide guide persists and show restores panel", async () => {
    render(<OperatorFirstRunWorkflowPanel />);

    await screen.findByRole("heading", { name: CORE_PILOT_FIRST_REVIEW_HEADING });

    fireEvent.click(screen.getByRole("button", { name: "Hide" }));

    expect(screen.queryByRole("heading", { name: CORE_PILOT_FIRST_REVIEW_HEADING })).not.toBeInTheDocument();
    expect(localStorage.getItem("archlucid_operator_workflow_guide_v1")).toBe("1");

    fireEvent.click(screen.getByRole("button", { name: CORE_PILOT_FIRST_REVIEW_MINIMIZED_BUTTON }));

    expect(screen.getByRole("heading", { name: CORE_PILOT_FIRST_REVIEW_HEADING })).toBeInTheDocument();
    expect(localStorage.getItem("archlucid_operator_workflow_guide_v1")).toBeNull();
  });
});
