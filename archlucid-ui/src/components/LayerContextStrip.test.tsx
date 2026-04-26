import { render } from "@testing-library/react";
import { describe, expect, it } from "vitest";

import { LayerContextStrip } from "./LayerContextStrip";

describe("LayerContextStrip", () => {
  it.each(
    [
      {
        id: "pilot" as const,
        wantLabel: "Pilot",
        wantQuestion: "Can we go from request to committed manifest faster?"
      },
      {
        id: "operate-analysis" as const,
        wantLabel: "Analysis",
        wantQuestion: "What changed, why, and what does the architecture look like?"
      },
      {
        id: "operate-governance" as const,
        wantLabel: "Governance",
        wantQuestion: "How do we govern, audit, and operationalize architecture decisions?"
      }
    ] as const
  )("renders label and question for $id", ({ id, wantLabel, wantQuestion }) => {
    const { getByTestId, queryByTestId, unmount } = render(<LayerContextStrip layerId={id} />);
    const strip = getByTestId("layer-context-strip");
    const t = (strip.textContent ?? "").replace(/\s+/g, " ");
    expect(t).toContain(wantLabel);
    expect(t).toContain(wantQuestion);
    if (id === "pilot") {
      expect(queryByTestId("layer-context-back-pilot")).toBeNull();
    } else {
      expect(getByTestId("layer-context-back-pilot")).toBeInTheDocument();
    }
    unmount();
  });
});
