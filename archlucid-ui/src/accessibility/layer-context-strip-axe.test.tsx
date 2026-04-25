import { render } from "@testing-library/react";
import { axe, toHaveNoViolations } from "jest-axe";
import { describe, expect, it } from "vitest";

import { LayerContextStrip } from "@/components/LayerContextStrip";

expect.extend(toHaveNoViolations);

describe("LayerContextStrip — axe (Vitest)", () => {
  it.each(
    [
      { id: "pilot" as const },
      { id: "operate-analysis" as const },
      { id: "operate-governance" as const }
    ] as const
  )("has no accessibility violations for $id (landmark, contrast heuristics)", async ({ id }) => {
    const { container, unmount } = render(<LayerContextStrip layerId={id} />);
    try {
      expect(await axe(container)).toHaveNoViolations();
    } finally {
      unmount();
    }
  });
});
