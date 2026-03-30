import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";

import { GraphViewer } from "./GraphViewer";

describe("GraphViewer", () => {
  it("renders empty-state when the graph has no nodes", () => {
    render(<GraphViewer graph={{ nodes: [], edges: [] }} />);

    expect(screen.getByText("No nodes in this graph.")).toBeInTheDocument();
  });

  it("renders filtered empty-state when the type filter removes every node", () => {
    const graph = {
      nodes: [{ id: "n1", label: "Only Service", type: "Service" }],
      edges: [] as { source: string; target: string; type: string }[],
    };

    render(<GraphViewer graph={graph} typeFilter="Decision" />);

    expect(
      screen.getByText('No nodes match type "Decision". Clear the filter or reload.'),
    ).toBeInTheDocument();
  });
});
