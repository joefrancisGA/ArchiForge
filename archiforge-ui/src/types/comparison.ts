export type DecisionDelta = {
  decisionKey: string;
  baseValue?: string | null;
  targetValue?: string | null;
  changeType: string;
};

export type RequirementDelta = {
  requirementName: string;
  changeType: string;
};

export type SecurityDelta = {
  controlName: string;
  baseStatus?: string | null;
  targetStatus?: string | null;
};

export type TopologyDelta = {
  resource: string;
  changeType: string;
};

export type CostDelta = {
  baseCost?: number | null;
  targetCost?: number | null;
};

export type GoldenManifestComparison = {
  baseRunId: string;
  targetRunId: string;
  decisionChanges: DecisionDelta[];
  requirementChanges: RequirementDelta[];
  securityChanges: SecurityDelta[];
  topologyChanges: TopologyDelta[];
  costChanges: CostDelta[];
  summaryHighlights: string[];
};
