export type ConversationThread = {
  threadId: string;
  tenantId: string;
  workspaceId: string;
  projectId: string;
  runId?: string | null;
  baseRunId?: string | null;
  targetRunId?: string | null;
  title: string;
  createdUtc: string;
  lastUpdatedUtc: string;
};

export type ConversationMessage = {
  messageId: string;
  threadId: string;
  role: string;
  content: string;
  createdUtc: string;
  metadataJson: string;
};

export type AskResponse = {
  threadId: string;
  answer: string;
  referencedDecisions: string[];
  referencedFindings: string[];
  referencedArtifacts: string[];
};
