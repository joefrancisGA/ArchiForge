import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";

import { LayerHeader } from "./LayerHeader";

describe("LayerHeader", () => {
  it("renders Advanced Analysis guidance for compare", () => {
    render(<LayerHeader pageKey="compare" />);

    expect(screen.getByText("Advanced Analysis")).toBeInTheDocument();
    expect(screen.getByText(/what changed between two committed runs/i)).toBeInTheDocument();
  });

  it("renders Enterprise responsibility footnote on audit", () => {
    render(<LayerHeader pageKey="audit" />);

    expect(
      screen.getByText(
        /Evidence and export surface—use when governance or audit requires it, not for Core Pilot by default\./i,
      ),
    ).toBeInTheDocument();
  });

  it("renders governance resolution Enterprise footnote", () => {
    render(<LayerHeader pageKey="governance-resolution" />);

    expect(screen.getByText(/Read-oriented governance evidence/i)).toBeInTheDocument();
  });

  it("renders Enterprise rank-aware note under footnote on audit (default rank outside provider)", () => {
    render(<LayerHeader pageKey="audit" />);

    expect(
      screen.getByText(/Operator\/admin surface when your operating model needs it—not required for Core Pilot\./i),
    ).toBeInTheDocument();
  });
});
