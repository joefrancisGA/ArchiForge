import type { OperatorScopeRecord } from "@/lib/operator-scope-storage";
import { defaultLabelsForScopeIds } from "@/lib/operator-scope-storage";

export type SessionScopeHeaderTriplet = {
  tenantId: string;
  workspaceId: string;
  projectId: string;
};

function shortenId(id: string): string {
  const t = id.trim();

  if (t.length === 0) {
    return "(unset)";
  }

  if (t.length <= 12) {
    return t;
  }

  return `${t.slice(0, 8)}…`;
}

/**
 * Honest, bookmark-friendly scope copy for the Executive Workspace Health banner — no cross-workspace rollup claims.
 */
export function formatExecutiveWorkspaceScopeDescription(
  record: OperatorScopeRecord | null,
  headers: SessionScopeHeaderTriplet,
): string {
  if (record !== null) {
    const fallback = defaultLabelsForScopeIds(record.workspaceId, record.projectId);
    const workspace =
      record.workspaceLabel.trim().length > 0 ? record.workspaceLabel.trim() : fallback.workspace;
    const project =
      record.projectLabel.trim().length > 0 ? record.projectLabel.trim() : fallback.project;

    return `Active scope: tenant ${shortenId(record.tenantId)}, workspace “${workspace}”, project “${project}”. Matches SESSION_CONTEXT on governance and audit requests — not a cross-workspace executive rollup.`;
  }

  return `Active scope: tenant ${shortenId(headers.tenantId)}, workspace ${shortenId(headers.workspaceId)}, project ${shortenId(headers.projectId)}. Matches SESSION_CONTEXT on governance and audit requests — not a cross-workspace executive rollup.`;
}
