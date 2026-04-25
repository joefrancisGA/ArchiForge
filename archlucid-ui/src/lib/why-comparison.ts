/**
 * Front-door /why comparison: same row labels and order as
 * docs/go-to-market/COMPETITIVE_LANDSCAPE.md § "Hard comparison table (front-door)".
 * CI: scripts/ci/check_why_table_alignment.py
 */

export type WhyHardComparisonCell = "yes" | "partial" | "no";

export type WhyHardComparisonRow = {
  label: string;
  archlucid: WhyHardComparisonCell;
  drawioConfluence: WhyHardComparisonCell;
  githubCopilotIac: WhyHardComparisonCell;
  genericAiArchitect: WhyHardComparisonCell;
};

/**
 * Ordered row labels — keep in sync with the markdown table first column (CI enforces).
 */
export const WHY_COMPARISON_TABLE_ROW_LABELS_IN_ORDER = [
  "Every commit produces a versioned, immutable manifest",
  "Every mutation produces a typed audit row in an append-only store",
  "Tenant isolation is enforced at SQL via Row-Level Security with SESSION_CONTEXT",
  "Authentication fails closed by default (API keys disabled until enabled)",
  "Comparison replay can re-derive the same artifact and detect drift",
  "Findings carry typed payloads per category, not free-text",
  "Pre-commit governance gate can block commit on configured severity thresholds",
] as const;

const WHY_HARD_ROW_CELLS: readonly Omit<WhyHardComparisonRow, "label">[] = [
  {
    archlucid: "yes",
    drawioConfluence: "partial",
    githubCopilotIac: "no",
    genericAiArchitect: "no",
  },
  {
    archlucid: "yes",
    drawioConfluence: "partial",
    githubCopilotIac: "no",
    genericAiArchitect: "no",
  },
  {
    archlucid: "yes",
    drawioConfluence: "no",
    githubCopilotIac: "no",
    genericAiArchitect: "no",
  },
  {
    archlucid: "yes",
    drawioConfluence: "partial",
    githubCopilotIac: "no",
    genericAiArchitect: "no",
  },
  {
    archlucid: "yes",
    drawioConfluence: "no",
    githubCopilotIac: "no",
    genericAiArchitect: "no",
  },
  {
    archlucid: "yes",
    drawioConfluence: "no",
    githubCopilotIac: "partial",
    genericAiArchitect: "partial",
  },
  {
    archlucid: "yes",
    drawioConfluence: "no",
    githubCopilotIac: "no",
    genericAiArchitect: "no",
  },
];

export const WHY_COMPARISON_ROWS: readonly WhyHardComparisonRow[] =
  WHY_COMPARISON_TABLE_ROW_LABELS_IN_ORDER.map((label, index) => ({
    label,
    ...WHY_HARD_ROW_CELLS[index]!,
  }));

/** JSON form for the marketing page import path (round-trip checked in Vitest). */
export const WHY_COMPARISON_ROWS_SERIALIZED: string = JSON.stringify(WHY_COMPARISON_ROWS);

export function whyHardCellDisplay(cell: WhyHardComparisonCell): string {
  if (cell === "yes") return "✓";

  if (cell === "partial") return "partial";

  return "—";
}
