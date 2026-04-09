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

    expect(await screen.findByRole("heading", { name: "First-run workflow" })).toBeInTheDocument();

    const wizard = screen.getByRole("link", { name: "Start new run wizard" });
    expect(wizard).toHaveAttribute("href", "/runs/new");
  });

  it("hide guide persists and show restores panel", async () => {
    render(<OperatorFirstRunWorkflowPanel />);

    await screen.findByRole("heading", { name: "First-run workflow" });

    fireEvent.click(screen.getByRole("button", { name: "Hide guide" }));

    expect(screen.queryByRole("heading", { name: "First-run workflow" })).not.toBeInTheDocument();
    expect(localStorage.getItem("archlucid_operator_workflow_guide_v1")).toBe("1");

    fireEvent.click(screen.getByRole("button", { name: "Show first-run workflow" }));

    expect(screen.getByRole("heading", { name: "First-run workflow" })).toBeInTheDocument();
    expect(localStorage.getItem("archlucid_operator_workflow_guide_v1")).toBeNull();
  });
});
