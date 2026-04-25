/**
 * Mirrors `ArchLucid.Contracts.Governance.PolicyPackDryRunResponse` and friends. See
 * `docs/library/AUDIT_COVERAGE_MATRIX.md` for the audit-trail companion.
 */

export type PolicyPackDryRunSeverityCount = {
  severity: string;
  count: number;
};

export type PolicyPackDryRunThresholdOutcome = {
  key: string;
  proposedValue: number;
  actualValue: number;
  wouldBreach: boolean;
};

export type PolicyPackDryRunRunItem = {
  runId: string;
  runMissing: boolean;
  findingsBySeverity: PolicyPackDryRunSeverityCount[];
  thresholdOutcomes: PolicyPackDryRunThresholdOutcome[];
  wouldBlock: boolean;
};

export type PolicyPackDryRunDeltaCounts = {
  evaluated: number;
  wouldBlock: number;
  wouldAllow: number;
  runMissing: number;
};

export type PolicyPackDryRunResponse = {
  policyPackId: string;
  evaluatedUtc: string;
  page: number;
  pageSize: number;
  totalRequestedRuns: number;
  returnedRuns: number;
  proposedThresholdsRedactedJson: string;
  deltaCounts: PolicyPackDryRunDeltaCounts;
  items: PolicyPackDryRunRunItem[];
};

export type PolicyPackDryRunRequest = {
  proposedThresholds: Record<string, string>;
  evaluateAgainstRunIds: string[];
};

/**
 * Default page size for the governance dry-run modal. Owner Q38 (2026-04-23) fixed
 * the default at 20 with a server-side cap of 100. Vitest asserts this constant so a
 * silent regression to a different default is caught at lint/test time.
 */
export const POLICY_PACK_DRY_RUN_DEFAULT_PAGE_SIZE = 20;

/** Server-side cap on page size; the API will clamp anything larger. */
export const POLICY_PACK_DRY_RUN_MAX_PAGE_SIZE = 100;
