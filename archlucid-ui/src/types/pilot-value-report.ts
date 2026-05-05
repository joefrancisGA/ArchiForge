/** JSON from `GET /v1/tenant/pilot-value-report` (camelCase). */

export type PilotValueReportSeverityJson = {
  critical: number;
  high: number;
  medium: number;
  low: number;
  info: number;
};

export type PilotValueReportTimelineRow = {
  runId: string;
  createdUtc: string;
  committedUtc: string | null;
  systemName: string;
};

export type PilotValueReportJson = {
  tenantId: string;
  fromUtc: string;
  toUtc: string;
  totalRunsCommitted: number;
  runDetailsTruncated: boolean;
  runDetailCap: number;
  totalFindings: number;
  findingsBySeverity: PilotValueReportSeverityJson;
  totalRecommendationsProduced: number;
  averagePipelineCompletionSeconds: number | null;
  governanceApprovals: number;
  governanceRejections: number;
  policyPackAssignments: number;
  comparisonOrDriftDetections: number;
  uniqueAgentTypes: string[];
  committedRunsTimeline: PilotValueReportTimelineRow[];
  governancePendingApprovalsNow: number;
  auditExportTruncated: boolean;
};
