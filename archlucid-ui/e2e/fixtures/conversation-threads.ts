import type { ConversationThread } from "@/types/conversation";

const TENANT_ID = "e2e-tenant-demonstration";
const WORKSPACE_ID = "ws-archlucid-pilot";

/** Mock Ask threads list for screenshots / mock API. */
export function fixtureConversationThreads(): ConversationThread[] {
  return [
    {
      threadId: "thread-claims-intake-001",
      tenantId: TENANT_ID,
      workspaceId: WORKSPACE_ID,
      projectId: "default",
      runId: "claims-intake-modernization",
      title: "Healthcare claims intake — data-flow review",
      createdUtc: "2026-01-12T10:05:00.000Z",
      lastUpdatedUtc: "2026-01-14T16:42:31.000Z",
    },
    {
      threadId: "thread-hipaa-boundary-002",
      tenantId: TENANT_ID,
      workspaceId: WORKSPACE_ID,
      projectId: "default",
      runId: "claims-intake-modernization",
      title: "HIPAA boundary analysis — PHI handling posture",
      createdUtc: "2026-01-10T14:22:00.000Z",
      lastUpdatedUtc: "2026-01-11T09:18:07.000Z",
    },
  ];
}
