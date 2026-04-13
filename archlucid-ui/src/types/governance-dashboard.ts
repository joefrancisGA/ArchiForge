import type {
  GovernanceApprovalRequest,
  GovernancePromotionRecord,
} from "@/types/governance-workflow";

export interface PolicyPackChangeLogEntry {
  changeLogId: string;
  policyPackId: string;
  tenantId: string;
  workspaceId: string;
  projectId: string;
  changeType: string;
  changedBy: string;
  changedUtc: string;
  previousValue?: string | null;
  newValue?: string | null;
  summaryText?: string | null;
}

export interface GovernanceDashboardSummary {
  pendingApprovals: GovernanceApprovalRequest[];
  recentDecisions: GovernanceApprovalRequest[];
  recentChanges: PolicyPackChangeLogEntry[];
  pendingCount: number;
}

/** One time bucket from GET /v1/governance/compliance-drift-trend. */
export interface ComplianceDriftTrendPoint {
  bucketUtc: string;
  changeCount: number;
  changesByType: Record<string, number>;
}

/** GET /v1/governance/approval-requests/{id}/lineage */
export interface GovernanceLineageRunSummary {
  runId: string;
  status: string;
  createdUtc: string;
  completedUtc: string | null;
  currentManifestVersion: string | null;
}

export interface GovernanceLineageManifestSummary {
  manifestVersion: string | null;
  decisionCount: number;
  unresolvedIssueCount: number;
  complianceGapCount: number;
}

export interface GovernanceLineageFindingSummary {
  findingId: string;
  title: string;
  engineType: string;
  severity: string;
  traceCompletenessRatio: number;
  /** Optional link to AgentExecutionTrace.traceId when the finding records it. */
  sourceAgentExecutionTraceId?: string | null;
}

export interface GovernanceLineageResult {
  approvalRequest: GovernanceApprovalRequest;
  run: GovernanceLineageRunSummary | null;
  manifest: GovernanceLineageManifestSummary | null;
  topFindings: GovernanceLineageFindingSummary[];
  riskPosture: string | null;
  promotions: GovernancePromotionRecord[];
}

/** GET /v1/governance/approval-requests/{id}/rationale */
export interface GovernanceRationaleResult {
  schemaVersion: number;
  approvalRequestId: string;
  summary: string;
  bullets: string[];
}
