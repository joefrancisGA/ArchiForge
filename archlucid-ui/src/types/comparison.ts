/** A single decision-level change between two golden manifests. */
export type DecisionDelta = {
  decisionKey: string;
  /** API may supply an operator-facing caption alongside the dotted key. */
  displayLabel?: string | null;
  baseValue?: string | null;
  targetValue?: string | null;
  changeType: string;
};

/** A requirement-level change (added, removed, or modified) between two manifests. */
export type RequirementDelta = {
  requirementName: string;
  changeType: string;
};

/** A security control change between two manifests (status transition). */
export type SecurityDelta = {
  controlName: string;
  baseStatus?: string | null;
  targetStatus?: string | null;
};

/** A topology resource change (added, removed, or modified) between two manifests. */
export type TopologyDelta = {
  resource: string;
  changeType: string;
};

/** Cost difference between two manifests (base vs target estimated costs). */
export type CostDelta = {
  baseCost?: number | null;
  targetCost?: number | null;
};

/** Structured comparison result between two golden manifests (all delta sections). */
export type GoldenManifestComparison = {
  baseRunId: string;
  targetRunId: string;
  decisionChanges: DecisionDelta[];
  requirementChanges: RequirementDelta[];
  securityChanges: SecurityDelta[];
  topologyChanges: TopologyDelta[];
  costChanges: CostDelta[];
  summaryHighlights: string[];
  /** API 55R+: sum of section delta counts from GET api/compare. */
  totalDeltaCount?: number;
};
