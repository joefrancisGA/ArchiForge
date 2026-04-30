import { fireEvent, render, screen } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";

import { OperatorFirstRunWorkflowPanel } from "./OperatorFirstRunWorkflowPanel";

describe("OperatorFirstRunWorkflowPanel", () => {
  afterEach(() => {
    localStorage.clear();
    vi.restoreAllMocks();
  });

  it("after hydrate shows workflow heading and primary wizard link", async () => {
    render(<OperatorFirstRunWorkflowPanel />);

    const heading = await screen.findByRole("heading", { name: "First Manifest Guide" });
    expect(heading).toBeInTheDocument();

    const workflowSummary = heading.nextElementSibling;
    expect(workflowSummary?.tagName.toLowerCase()).toBe("p");
    expect(workflowSummary).toHaveTextContent("Create → Run → Finalize → Review");

    expect(screen.getByText("Start here")).toBeInTheDocument();

    const wizard = screen.getByRole("link", { name: "Start new request" });
    expect(wizard).toHaveAttribute("href", "/runs/new");
  });

  it("accordion toggles step body when clicking the step title", async () => {
    render(<OperatorFirstRunWorkflowPanel />);

    await screen.findByRole("heading", { name: "First Manifest Guide" });

    expect(screen.getByRole("link", { name: "Start new request" })).toBeVisible();

    const step2Title = screen.getByRole("button", { name: /Step 2 —/i });
    fireEvent.click(step2Title);

    expect(screen.getByRole("link", { name: "Open runs list" })).toBeVisible();

    const step1Title = screen.getByRole("button", { name: /Step 1 —/i });
    fireEvent.click(step1Title);

    expect(await screen.findByRole("link", { name: "Start new request" })).toBeVisible();
  });

  it("hide guide persists and show restores panel", async () => {
    render(<OperatorFirstRunWorkflowPanel />);

    await screen.findByRole("heading", { name: "First Manifest Guide" });

    fireEvent.click(screen.getByRole("button", { name: "Hide" }));

    expect(screen.queryByRole("heading", { name: "First Manifest Guide" })).not.toBeInTheDocument();
    expect(localStorage.getItem("archlucid_operator_workflow_guide_v1")).toBe("1");

    fireEvent.click(screen.getByRole("button", { name: "Show First Manifest Guide" }));

    expect(screen.getByRole("heading", { name: "First Manifest Guide" })).toBeInTheDocument();
    expect(localStorage.getItem("archlucid_operator_workflow_guide_v1")).toBeNull();
  });
});
