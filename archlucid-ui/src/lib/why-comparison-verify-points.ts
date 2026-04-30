import { DEFAULT_GITHUB_BLOB_BASE } from "@/lib/docs-public-base";

/**
 * One row per front-door hard-comparison claim (same order as `WHY_COMPARISON_ROWS` / COMPETITIVE_LANDSCAPE.md).
 * CI: row count locked in `why-comparison.test.ts`.
 */
export type WhyVerifyLink = {
  readonly label: string;
  readonly href: string;
};

export const WHY_COMPARISON_VERIFY_LINK_ROWS: readonly WhyVerifyLink[][] = [
  [{ label: "/demo/preview", href: "/demo/preview" }],
  [
    { label: "Evidence pack (ZIP)", href: "/v1/marketing/trust-center/evidence-pack.zip" },
    {
      label: "AUDIT_COVERAGE_MATRIX.md",
      href: `${DEFAULT_GITHUB_BLOB_BASE}/docs/library/AUDIT_COVERAGE_MATRIX.md`,
    },
  ],
  [
    {
      label: "MULTI_TENANT_RLS.md",
      href: `${DEFAULT_GITHUB_BLOB_BASE}/docs/security/MULTI_TENANT_RLS.md`,
    },
  ],
  [{ label: "V1_SCOPE.md (auth)", href: `${DEFAULT_GITHUB_BLOB_BASE}/docs/library/V1_SCOPE.md` }],
  [
    {
      label: "COMPARISON_REPLAY.md",
      href: `${DEFAULT_GITHUB_BLOB_BASE}/docs/library/COMPARISON_REPLAY.md`,
    },
  ],
  [{ label: "/demo/explain", href: "/demo/explain" }],
  [
    {
      label: "PRE_COMMIT_GOVERNANCE_GATE.md",
      href: `${DEFAULT_GITHUB_BLOB_BASE}/docs/library/PRE_COMMIT_GOVERNANCE_GATE.md`,
    },
  ],
];
