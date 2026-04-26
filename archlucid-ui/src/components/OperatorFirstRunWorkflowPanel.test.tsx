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

    expect(await screen.findByRole("heading", { name: "First Manifest Checklist" })).toBeInTheDocument();

    const wizard = screen.getByRole("link", { name: "Start new run wizard" });
    expect(wizard).toHaveAttribute("href", "/runs/new");
  });

  it("accordion toggles step body when clicking the step title", async () => {
    render(<OperatorFirstRunWorkflowPanel />);

    await screen.findByRole("heading", { name: "First Manifest Checklist" });

    expect(screen.getByRole("link", { name: "Start new run wizard" })).toBeVisible();

    const step2Title = screen.getByRole("button", { name: /Step 2 —/i });
    fireEvent.click(step2Title);

    expect(screen.queryByRole("link", { name: "Start new run wizard" })).not.toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Open runs list" })).toBeVisible();

    const step1Title = screen.getByRole("button", { name: /Step 1 —/i });
    fireEvent.click(step1Title);

    expect(await screen.findByRole("link", { name: "Start new run wizard" })).toBeVisible();
  });

  it("hide guide persists and show restores panel", async () => {
    render(<OperatorFirstRunWorkflowPanel />);

    await screen.findByRole("heading", { name: "First Manifest Checklist" });

    fireEvent.click(screen.getByRole("button", { name: "Hide" }));

    expect(screen.queryByRole("heading", { name: "First Manifest Checklist" })).not.toBeInTheDocument();
    expect(localStorage.getItem("archlucid_operator_workflow_guide_v1")).toBe("1");

    fireEvent.click(screen.getByRole("button", { name: "Show First Manifest Checklist" }));

    expect(screen.getByRole("heading", { name: "First Manifest Checklist" })).toBeInTheDocument();
    expect(localStorage.getItem("archlucid_operator_workflow_guide_v1")).toBeNull();
  });
});
