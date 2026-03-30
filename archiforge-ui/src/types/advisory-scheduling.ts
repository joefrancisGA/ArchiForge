/** A cron-based schedule for periodic advisory scans. */
export type AdvisoryScanSchedule = {
  scheduleId: string;
  tenantId: string;
  workspaceId: string;
  projectId: string;
  runProjectSlug: string;
  name: string;
  cronExpression: string;
  isEnabled: boolean;
  createdUtc: string;
  lastRunUtc?: string | null;
  nextRunUtc?: string | null;
};

/** A single execution of an advisory scan schedule (started, status, result). */
export type AdvisoryScanExecution = {
  executionId: string;
  scheduleId: string;
  startedUtc: string;
  completedUtc?: string | null;
  status: string;
  resultJson: string;
  errorMessage?: string | null;
};

/** A periodic architecture digest (summary report with markdown content). */
export type ArchitectureDigest = {
  digestId: string;
  tenantId: string;
  workspaceId: string;
  projectId: string;
  runId?: string | null;
  comparedToRunId?: string | null;
  generatedUtc: string;
  title: string;
  summary: string;
  contentMarkdown: string;
  metadataJson: string;
};
