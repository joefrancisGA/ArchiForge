/** A simple alert rule with a metric type, severity, and threshold. */
export type AlertRule = {
  ruleId: string;
  tenantId: string;
  workspaceId: string;
  projectId: string;
  name: string;
  ruleType: string;
  severity: string;
  thresholdValue: number;
  isEnabled: boolean;
  targetChannelType: string;
  metadataJson: string;
  createdUtc: string;
};

/** A fired alert record with lifecycle state (Active → Acknowledged → Resolved/Suppressed). */
export type AlertRecord = {
  alertId: string;
  ruleId: string;
  title: string;
  category: string;
  severity: string;
  status: string;
  triggerValue: string;
  description: string;
  createdUtc: string;
  lastUpdatedUtc?: string | null;
  runId?: string | null;
  comparedToRunId?: string | null;
  recommendationId?: string | null;
};
