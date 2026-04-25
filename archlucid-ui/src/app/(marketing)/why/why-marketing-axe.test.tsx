import { render } from "@testing-library/react";
import { axe, toHaveNoViolations } from "jest-axe";
import { describe, expect, it } from "vitest";

import { WHY_COMPARISON_ROWS } from "@/lib/why-comparison";

import { WhyArchlucidMarketingView } from "./WhyArchlucidMarketingView";

expect.extend(toHaveNoViolations);

describe("Why ArchLucid marketing page (Vitest + axe)", () => {
  it("has no serious axe violations", async () => {
    const { container } = render(
      <WhyArchlucidMarketingView frontDoorRows={WHY_COMPARISON_ROWS} showDemoEmbed={false} />,
    );

    expect(await axe(container)).toHaveNoViolations();
  });
});
