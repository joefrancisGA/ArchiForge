import { render, screen } from "@testing-library/react";
import { beforeAll, describe, expect, it } from "vitest";

import { GraphViewer } from "./GraphViewer";

beforeAll(() => {
  globalThis.ResizeObserver = class {
    observe(): void {}
    unobserve(): void {}
    disconnect(): void {}
  };
});

describe("GraphViewer", () => {
  it("renders node detail panel when the graph has nodes (normal case)", () => {
    const graph = {
      nodes: [{ id: "n1", label: "Service A", type: "Service", metadata: { region: "east" } }],
      edges: [] as { source: string; target: string; type: string }[],
    };

    render(<GraphViewer graph={graph} />);

    expect(screen.getByText("Node detail")).toBeInTheDocument();
    expect(screen.getByText(/Select a node to inspect/)).toBeInTheDocument();
  });

  it("renders empty-state when the graph has no nodes", () => {
    render(<GraphViewer graph={{ nodes: [], edges: [] }} />);

    expect(
      screen.getByText(/The API returned a graph with no nodes/),
    ).toBeInTheDocument();
  });

  it("renders filtered empty-state when the type filter removes every node", () => {
    const graph = {
      nodes: [{ id: "n1", label: "Only Service", type: "Service" }],
      edges: [] as { source: string; target: string; type: string }[],
    };

    render(<GraphViewer graph={graph} typeFilter="Decision" />);

    expect(screen.getByText(/No nodes match type "Decision"/)).toBeInTheDocument();
  });
});
