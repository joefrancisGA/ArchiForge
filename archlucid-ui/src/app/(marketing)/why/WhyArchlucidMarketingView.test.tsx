import { render } from "@testing-library/react";
import { describe, expect, it } from "vitest";

import { WhyArchlucidMarketingView } from "./WhyArchlucidMarketingView";
import { WHY_ARCHLUCID_COMPARISON_ROWS } from "@/marketing/why-archlucid-comparison";

describe("WhyArchlucidMarketingView", () => {
  it("matches snapshot (marketing /why layout + proof pack download)", () => {
    const { container } = render(
      <WhyArchlucidMarketingView rows={WHY_ARCHLUCID_COMPARISON_ROWS} showDemoEmbed={false} />,
    );

    expect(container.firstChild).toMatchSnapshot();
  });

  it("renders proof pack download targeting the proxied PDF endpoint", () => {
    const { getByTestId } = render(
      <WhyArchlucidMarketingView rows={WHY_ARCHLUCID_COMPARISON_ROWS} showDemoEmbed={false} />,
    );

    const link = getByTestId("why-proof-pack-download");
    expect(link.getAttribute("href")).toBe("/api/proxy/v1/marketing/why-archlucid-pack.pdf");
  });
});
