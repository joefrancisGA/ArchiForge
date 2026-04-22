import { describe, expect, it } from "vitest";

import {
  WHY_ARCHLUCID_COMPARISON_ROWS,
  WHY_ARCHLUCID_COMPETITOR_LANDSCAPE_CITATION,
} from "./why-archlucid-comparison";

/** Every ArchLucid proof line must point at a docs/ or adr/ path so claims stay traceable to the repo. */
const CITATION_EVIDENCE = /docs\/|docs\\|adr\/|adr\\/i;

const EVIDENCE_API = /GET \/v1\/|POST \/v1\/|GET \/openapi\//i;

const EVIDENCE_MARKETING = /\/marketing\/why\//i;

describe("WHY_ARCHLUCID_COMPARISON_ROWS", () => {
  it("has at least twelve anchored rows", () => {
    expect(WHY_ARCHLUCID_COMPARISON_ROWS.length).toBeGreaterThanOrEqual(12);
  });

  it("requires a docs/ or adr/ citation on every ArchLucid cell", () => {
    for (const row of WHY_ARCHLUCID_COMPARISON_ROWS) {
      expect(row.archlucidCitation.trim().length, `dimension=${row.dimension}`).toBeGreaterThan(10);
      expect(row.archlucidCitation, `dimension=${row.dimension}`).toMatch(CITATION_EVIDENCE);
    }
  });

  it("requires an evidence anchor with API path and marketing asset on every row", () => {
    for (const row of WHY_ARCHLUCID_COMPARISON_ROWS) {
      expect(row.evidenceAnchor.trim().length, `dimension=${row.dimension}`).toBeGreaterThan(8);
      expect(row.evidenceAnchor, `dimension=${row.dimension}`).toMatch(EVIDENCE_API);
      expect(row.evidenceAnchor, `dimension=${row.dimension}`).toMatch(EVIDENCE_MARKETING);
    }
  });

  it("requires the competitive-landscape §2.1 footnote on every row (incumbent + PDF alignment)", () => {
    for (const row of WHY_ARCHLUCID_COMPARISON_ROWS) {
      expect(row.competitorLandscapeCitation, `dimension=${row.dimension}`).toBe(WHY_ARCHLUCID_COMPETITOR_LANDSCAPE_CITATION);
      expect(row.competitorLandscapeCitation, `dimension=${row.dimension}`).toMatch(/COMPETITIVE_LANDSCAPE\.md/i);
      expect(row.competitorLandscapeCitation, `dimension=${row.dimension}`).toContain("§2.1");
    }
  });

  it("keeps competitor columns non-empty", () => {
    for (const row of WHY_ARCHLUCID_COMPARISON_ROWS) {
      expect(row.leanix.trim(), `dimension=${row.dimension}`).not.toHaveLength(0);
      expect(row.ardoq.trim(), `dimension=${row.dimension}`).not.toHaveLength(0);
      expect(row.megaHopex.trim(), `dimension=${row.dimension}`).not.toHaveLength(0);
      expect(row.archlucid.trim(), `dimension=${row.dimension}`).not.toHaveLength(0);
    }
  });
});
