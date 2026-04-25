import { render } from "@testing-library/react";
import { describe, expect, it } from "vitest";

import { BRAND_CATEGORY, BRAND_CATEGORY_LEGACY } from "@/lib/brand-category";
import { WHY_COMPARISON_ROWS } from "@/lib/why-comparison";

import { WhyArchlucidMarketingView } from "./WhyArchlucidMarketingView";

describe("WhyArchlucidMarketingView", () => {
  it("matches snapshot (marketing /why layout + proof pack download)", () => {
    const { container } = render(
      <WhyArchlucidMarketingView frontDoorRows={WHY_COMPARISON_ROWS} showDemoEmbed={false} />,
    );

    expect(container.firstChild).toMatchSnapshot();
  });

  it("renders proof pack download targeting the proxied PDF endpoint", () => {
    const { getByTestId } = render(
      <WhyArchlucidMarketingView frontDoorRows={WHY_COMPARISON_ROWS} showDemoEmbed={false} />,
    );

    const link = getByTestId("why-proof-pack-download");
    expect(link.getAttribute("href")).toBe("/api/proxy/v1/marketing/why-archlucid-pack.pdf");
  });

  it("renders the brand-category paragraph using BRAND_CATEGORY (not the legacy string)", () => {
    const { getByTestId } = render(
      <WhyArchlucidMarketingView frontDoorRows={WHY_COMPARISON_ROWS} showDemoEmbed={false} />,
    );

    const paragraph = getByTestId("why-brand-category-paragraph");
    const text = paragraph.textContent ?? "";

    expect(text).toContain(BRAND_CATEGORY);
    expect(text).not.toContain(BRAND_CATEGORY_LEGACY);
  });
});
