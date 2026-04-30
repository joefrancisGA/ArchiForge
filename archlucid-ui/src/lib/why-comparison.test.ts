import { describe, expect, it } from "vitest";

import { WHY_COMPARISON_VERIFY_LINK_ROWS } from "./why-comparison-verify-points";
import {
  type WhyHardComparisonRow,
  WHY_COMPARISON_ROWS,
  WHY_COMPARISON_ROWS_SERIALIZED,
  WHY_COMPARISON_TABLE_ROW_LABELS_IN_ORDER,
} from "./why-comparison";

describe("why-comparison (front-door table drift guards)", () => {
  it("WHY_COMPARISON_ROWS_SERIALIZED round-trips to WHY_COMPARISON_ROWS", () => {
    const parsed: WhyHardComparisonRow[] = JSON.parse(WHY_COMPARISON_ROWS_SERIALIZED) as WhyHardComparisonRow[];
    expect(parsed).toEqual([...WHY_COMPARISON_ROWS]);
  });

  it("every row has exactly four product columns and ArchLucid is yes on every row", () => {
    const productKeys = [
      "archlucid",
      "drawioConfluence",
      "githubCopilotIac",
      "genericAiArchitect",
    ] as const satisfies readonly (keyof WhyHardComparisonRow)[];

    expect(productKeys).toHaveLength(4);

    for (const row of WHY_COMPARISON_ROWS) {
      expect(row.archlucid).toBe("yes");

      for (const key of productKeys) {
        expect(["yes", "partial", "no"] as const).toContain(row[key]);
      }
    }
  });

  it("row labels match the ordered label export", () => {
    expect(WHY_COMPARISON_ROWS.map((row) => row.label)).toEqual([...WHY_COMPARISON_TABLE_ROW_LABELS_IN_ORDER]);
  });

  it("marketing verify link rows stay aligned with comparison row count", () => {
    expect(WHY_COMPARISON_VERIFY_LINK_ROWS).toHaveLength(WHY_COMPARISON_ROWS.length);

    for (const links of WHY_COMPARISON_VERIFY_LINK_ROWS) {
      expect(links.length).toBeGreaterThan(0);

      for (const link of links) {
        expect(link.label.trim().length).toBeGreaterThan(0);
        expect(link.href.trim().length).toBeGreaterThan(0);
      }
    }
  });
});
