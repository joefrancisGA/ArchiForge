import { render } from "@testing-library/react";
import { describe, expect, it } from "vitest";

import { MarketingPricingPublicCutoverNotice } from "./MarketingPricingPublicCutoverNotice";

describe("MarketingPricingPublicCutoverNotice", () => {
  it("matches snapshot (Marketplace go-live / public price list notice)", () => {
    const { container } = render(<MarketingPricingPublicCutoverNotice />);

    expect(container.firstChild).toMatchSnapshot();
  });
});
