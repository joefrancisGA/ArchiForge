import { isLikelySignedIn } from "@/lib/oidc/session";
import { registrationScopeHeaders } from "@/lib/registration-session";
import { DEV_SCOPE_PROJECT_ID, DEV_SCOPE_TENANT_ID, DEV_SCOPE_WORKSPACE_ID, getScopeHeaders } from "@/lib/scope";

const STORAGE_KEY = "archlucid_operator_scope_v1";

/**
 * Browser-persisted scope selection (IDs + optional display labels for the header switcher).
 * The proxy forwards `x-tenant-id` / `x-workspace-id` / `x-project-id` on every `/api/proxy` request;
 * this module is the client-side source of truth when all three IDs are set.
 */
export type OperatorScopeRecord = {
  tenantId: string;
  workspaceId: string;
  projectId: string;
  /** For header copy; may be empty when only IDs are known. */
  workspaceLabel: string;
  projectLabel: string;
};

function isNonEmptyId(value: string | undefined | null): boolean {
  return value !== null && value !== undefined && value.trim().length > 0;
}

export function readOperatorScopeFromStorage(): OperatorScopeRecord | null {
  if (typeof window === "undefined") {
    return null;
  }

  try {
    const raw = window.localStorage.getItem(STORAGE_KEY);
    if (raw === null || raw.length === 0) {
      return null;
    }
    const parsed = JSON.parse(raw) as unknown;
    if (parsed === null || typeof parsed !== "object" || !("tenantId" in parsed)) {
      return null;
    }
    const row = parsed as Record<string, unknown>;
    const tenantId = String(row.tenantId ?? "");
    const workspaceId = String(row.workspaceId ?? "");
    const projectId = String(row.projectId ?? "");
    if (!isNonEmptyId(tenantId) || !isNonEmptyId(workspaceId) || !isNonEmptyId(projectId)) {
      return null;
    }
    return {
      tenantId: tenantId.trim(),
      workspaceId: workspaceId.trim(),
      projectId: projectId.trim(),
      workspaceLabel: String(row.workspaceLabel ?? "").trim(),
      projectLabel: String(row.projectLabel ?? "").trim(),
    };
  } catch {
    return null;
  }
}

export function writeOperatorScopeToStorage(record: OperatorScopeRecord): void {
  if (typeof window === "undefined") {
    return;
  }
  try {
    window.localStorage.setItem(
      STORAGE_KEY,
      JSON.stringify({
        tenantId: record.tenantId,
        workspaceId: record.workspaceId,
        projectId: record.projectId,
        workspaceLabel: record.workspaceLabel,
        projectLabel: record.projectLabel,
      }),
    );
  } catch {
    /* quota / private mode */
  }
}

export function clearOperatorScopeStorage(): void {
  if (typeof window === "undefined") {
    return;
  }
  try {
    window.localStorage.removeItem(STORAGE_KEY);
  } catch {
    /* */
  }
}

/**
 * Resolves the scope headers the browser should send to `/api/proxy`, matching
 * `buildUpstreamHeaders` in `app/api/proxy/[...path]/route.ts` (server fallback when a header is absent).
 * Priority: explicit operator selection (localStorage) → post-registration session (unsigned only) → dev defaults.
 */
export function getEffectiveBrowserProxyScopeHeaders(): Record<string, string> {
  if (typeof window === "undefined") {
    return getScopeHeaders();
  }

  const fromOperator = readOperatorScopeFromStorage();
  if (fromOperator !== null) {
    return {
      "x-tenant-id": fromOperator.tenantId,
      "x-workspace-id": fromOperator.workspaceId,
      "x-project-id": fromOperator.projectId,
    };
  }

  if (!isLikelySignedIn()) {
    const reg = registrationScopeHeaders();
    if (reg !== null) {
      return reg;
    }
  }

  return getScopeHeaders();
}

/** Display strings for the header when labels are missing. In production builds, dev-default UUIDs use neutral copy for screenshots and demos. */
export function defaultLabelsForScopeIds(
  workspaceId: string,
  projectId: string,
): { workspace: string; project: string } {
  const production = process.env.NODE_ENV === "production";

  const productionLike =
    production || process.env.NEXT_PUBLIC_DEMO_MODE === "true";

  const ws =
    workspaceId.trim() === DEV_SCOPE_WORKSPACE_ID
      ? productionLike
        ? "Workspace"
        : "Development workspace"
      : workspaceId.slice(0, 8) + "…";
  const pr =
    projectId.trim() === DEV_SCOPE_PROJECT_ID ? "Default project" : projectId.slice(0, 8) + "…";
  return { workspace: ws, project: pr };
}

export function isDevDefaultScopeRecord(record: Pick<OperatorScopeRecord, "tenantId" | "workspaceId" | "projectId">): boolean {
  return (
    record.tenantId === DEV_SCOPE_TENANT_ID &&
    record.workspaceId === DEV_SCOPE_WORKSPACE_ID &&
    record.projectId === DEV_SCOPE_PROJECT_ID
  );
}
