import { fireEvent, render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";

import { InspectorPanel } from "./InspectorPanel";

describe("InspectorPanel", () => {
  it("renders title and children", () => {
    render(
      <InspectorPanel title="Test title" onClose={() => {}} listenEscape={false}>
        <p>Body content</p>
      </InspectorPanel>,
    );
    expect(screen.getByRole("heading", { name: "Test title" })).toBeInTheDocument();
    expect(screen.getByText("Body content")).toBeInTheDocument();
  });

  it("calls onClose when close button is clicked", () => {
    const onClose = vi.fn();
    render(
      <InspectorPanel title="T" onClose={onClose} listenEscape={false}>
        <span>x</span>
      </InspectorPanel>,
    );
    fireEvent.click(screen.getByTestId("inspector-panel-close"));
    expect(onClose).toHaveBeenCalledTimes(1);
  });

  it("calls onClose on Escape when listenEscape is true", () => {
    const onClose = vi.fn();
    render(
      <InspectorPanel title="T" onClose={onClose} listenEscape>
        <span>x</span>
      </InspectorPanel>,
    );
    fireEvent.keyDown(window, { key: "Escape" });
    expect(onClose).toHaveBeenCalled();
  });

  it("does not register Escape when listenEscape is false", () => {
    const onClose = vi.fn();
    render(
      <InspectorPanel title="T" onClose={onClose} listenEscape={false}>
        <span>x</span>
      </InspectorPanel>,
    );
    fireEvent.keyDown(window, { key: "Escape" });
    expect(onClose).not.toHaveBeenCalled();
  });
});
