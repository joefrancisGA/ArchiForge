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
