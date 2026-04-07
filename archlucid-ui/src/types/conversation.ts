/** A conversation thread in the ArchiForge Ask system (scoped to tenant/workspace/project). */
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

/** A single message in a conversation thread (user question or AI response). */
export type ConversationMessage = {
  messageId: string;
  threadId: string;
  role: string;
  content: string;
  createdUtc: string;
  metadataJson: string;
};

/** Response from the /api/ask endpoint: answer text plus referenced decisions/findings/artifacts. */
export type AskResponse = {
  threadId: string;
  answer: string;
  referencedDecisions: string[];
  referencedFindings: string[];
  referencedArtifacts: string[];
};
