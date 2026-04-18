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
      screen.getByText(/Evidence for sponsors and audit—still not required for Core Pilot\./i),
    ).toBeInTheDocument();
  });
});
