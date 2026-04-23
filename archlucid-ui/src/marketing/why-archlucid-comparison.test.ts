import { describe, expect, it } from "vitest";

import { WHY_ARCHLUCID_COMPARISON_ROWS } from "./why-archlucid-comparison";

const FIRST_PARTY_CITATION = "first-party assertion (no external citation yet)";

const HTTPS = /^https:\/\//i;

describe("WHY_ARCHLUCID_COMPARISON_ROWS", () => {
  it("has exactly five differentiation rows (PDF + page + CI sync)", () => {
    expect(WHY_ARCHLUCID_COMPARISON_ROWS).toHaveLength(5);
  });

  it("requires non-empty claim, evidence, baseline, citation, and narrative on every row", () => {
    for (const [index, row] of WHY_ARCHLUCID_COMPARISON_ROWS.entries()) {
      expect(row.claim.trim(), `row=${index}`).not.toHaveLength(0);
      expect(row.archlucidEvidence.trim(), `row=${index}`).not.toHaveLength(0);
      expect(row.competitorBaseline.trim(), `row=${index}`).not.toHaveLength(0);
      expect(row.citation.trim(), `row=${index}`).not.toHaveLength(0);
      expect(row.narrativeParagraph.trim(), `row=${index}`).not.toHaveLength(0);
    }
  });

  it("requires citation to be HTTPS or the explicit first-party disclaimer phrase", () => {
    for (const [index, row] of WHY_ARCHLUCID_COMPARISON_ROWS.entries()) {
      const ok = HTTPS.test(row.citation) || row.citation === FIRST_PARTY_CITATION;
      expect(ok, `row=${index} citation=${row.citation}`).toBe(true);
    }
  });

  it("keeps evidence anchored to repo paths, routes, tests, workflows, or docs", () => {
    const evidencePattern =
      /(`[^`]+`)|(\bGET \/v1\/|\bPOST \/v1\/)|(\.(cs|md|json|yml|py)\b)|(\btests\/|\bscripts\/|\.github\/workflows\/)/i;

    for (const [index, row] of WHY_ARCHLUCID_COMPARISON_ROWS.entries()) {
      expect(row.archlucidEvidence, `row=${index}`).toMatch(evidencePattern);
    }
  });

  it("limits narrative to four sentences (buyer-facing PDF constraint)", () => {
    for (const [index, row] of WHY_ARCHLUCID_COMPARISON_ROWS.entries()) {
      const sentenceEnds = row.narrativeParagraph.match(/[.!?](?:\s|$)/g);
      const count = sentenceEnds?.length ?? 0;
      expect(count, `row=${index} narrative=${row.narrativeParagraph}`).toBeLessThanOrEqual(4);
    }
  });
});
