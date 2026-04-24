import { render } from "@testing-library/react";
import { describe, expect, it } from "vitest";

import { BRAND_CATEGORY, BRAND_CATEGORY_LEGACY } from "@/lib/brand-category";

import GetStartedPage from "./page";

describe("GetStartedPage brand category", () => {
  it("renders the brand-category paragraph using BRAND_CATEGORY (not the legacy string)", () => {
    const { getByTestId } = render(<GetStartedPage />);

    const paragraph = getByTestId("get-started-brand-category-paragraph");
    const text = paragraph.textContent ?? "";

    expect(text).toContain(BRAND_CATEGORY);
    expect(text).not.toContain(BRAND_CATEGORY_LEGACY);
  });
});
