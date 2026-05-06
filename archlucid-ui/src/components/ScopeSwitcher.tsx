"use client";

import { ChevronsUpDown } from "lucide-react";
import { useCallback, useEffect, useMemo, useState } from "react";

import { ContextualHelp } from "@/components/ContextualHelp";
import { useOperatorNavAuthority } from "@/components/OperatorNavAuthorityProvider";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { Label } from "@/components/ui/label";
import { isBuyerPolishedOperatorShellEnv } from "@/lib/demo-ui-env";
import { AUTHORITY_RANK } from "@/lib/nav-authority";
import { formatOperatorProjectIdDisplay } from "@/lib/operator-project-display";
import {
  clearOperatorScopeStorage,
  defaultLabelsForScopeIds,
  getEffectiveBrowserProxyScopeHeaders,
  isDevDefaultScopeRecord,
  readOperatorScopeFromStorage,
  type OperatorScopeRecord,
  writeOperatorScopeToStorage,
} from "@/lib/operator-scope-storage";
import { mergeRegistrationScopeForProxy } from "@/lib/proxy-fetch-registration-scope";
import { DEV_SCOPE_PROJECT_ID, DEV_SCOPE_WORKSPACE_ID } from "@/lib/scope";

const WORKSPACES_PATH = "/api/proxy/v1/tenant/workspaces";

type ProjectOption = { projectId: string; name: string };
type WorkspaceOption = { workspaceId: string; name: string; projects: ProjectOption[] };

type WorkspacesListPayload = {
  workspaces?: ReadonlyArray<{
    workspaceId?: string;
    id?: string;
    name?: string;
    displayName?: string;
    projects?: ReadonlyArray<{
      projectId?: string;
      id?: string;
      name?: string;
      displayName?: string;
    }>;
  }>;
};

function parseWorkspacesList(json: unknown): WorkspaceOption[] {
  if (json === null || typeof json !== "object") {
    return [];
  }
  const root = json as WorkspacesListPayload;
  const raw = root.workspaces;
  if (!Array.isArray(raw)) {
    return [];
  }
  const out: WorkspaceOption[] = [];
  for (const w of raw) {
    if (w === null || typeof w !== "object") {
      continue;
    }
    const wid = (w as { workspaceId?: string; id?: string }).workspaceId ?? (w as { id?: string }).id;
    if (typeof wid !== "string" || wid.trim().length === 0) {
      continue;
    }
    const wname =
      typeof w.displayName === "string" && w.displayName.trim().length > 0
        ? w.displayName.trim()
        : typeof w.name === "string" && w.name.trim().length > 0
          ? w.name.trim()
          : "Workspace";
    const projects: ProjectOption[] = [];
    const prows = w.projects;
    if (Array.isArray(prows)) {
      for (const p of prows) {
        if (p === null || typeof p !== "object") {
          continue;
        }
        const pid = (p as { projectId?: string; id?: string }).projectId ?? (p as { id?: string }).id;
        if (typeof pid !== "string" || pid.trim().length === 0) {
          continue;
        }
        const pname =
          typeof p.displayName === "string" && p.displayName.trim().length > 0
            ? p.displayName.trim()
            : typeof p.name === "string" && p.name.trim().length > 0
              ? p.name.trim()
              : "Project";
        projects.push({ projectId: pid.trim(), name: pname });
      }
    }
    out.push({ workspaceId: wid.trim(), name: wname, projects });
  }
  return out;
}

/**
 * Header control: show current workspace/project, persist scope to `localStorage`, and send scope on `/api/proxy` requests
 * (see `getEffectiveBrowserProxyScopeHeaders`).
 */
export function ScopeSwitcher() {
  const { callerAuthorityRank, isAuthorityLoading } = useOperatorNavAuthority();
  const [open, setOpen] = useState(false);
  const [tick, setTick] = useState(0);
  const [listLoading, setListLoading] = useState(false);
  const [listError, setListError] = useState<string | null>(null);
  const [workspaces, setWorkspaces] = useState<WorkspaceOption[] | null>(null);

  const effective = useMemo(() => {
    void tick;
    return getEffectiveBrowserProxyScopeHeaders();
  }, [tick]);
  const stored = useMemo(() => {
    void tick;
    return readOperatorScopeFromStorage();
  }, [tick]);
  const tenantId = effective["x-tenant-id"] ?? "";
  const workspaceId = effective["x-workspace-id"] ?? "";
  const projectId = effective["x-project-id"] ?? "";

  const { workspaceLabel, projectLabel } = useMemo(() => {
    const d = defaultLabelsForScopeIds(workspaceId, projectId);
    if (stored === null) {
      return { workspaceLabel: d.workspace, projectLabel: d.project };
    }
    const w = stored.workspaceLabel.length > 0 ? stored.workspaceLabel : d.workspace;
    const p = stored.projectLabel.length > 0 ? stored.projectLabel : d.project;
    return { workspaceLabel: w, projectLabel: p };
  }, [stored, workspaceId, projectId]);

  const polishedShell = isBuyerPolishedOperatorShellEnv();
  const scopeButtonWorkspace =
    polishedShell &&
    workspaceId.trim() === DEV_SCOPE_WORKSPACE_ID &&
    projectId.trim() === DEV_SCOPE_PROJECT_ID
      ? "Illustrative workspace"
      : workspaceLabel;
  const scopeButtonProject =
    polishedShell &&
    workspaceId.trim() === DEV_SCOPE_WORKSPACE_ID &&
    projectId.trim() === DEV_SCOPE_PROJECT_ID
      ? formatOperatorProjectIdDisplay(DEV_SCOPE_PROJECT_ID)
      : projectLabel;

  const canShow =
    !isAuthorityLoading && callerAuthorityRank >= AUTHORITY_RANK.ReadAuthority;

  const refreshList = useCallback(async () => {
    setListLoading(true);
    setListError(null);
    try {
      const res = await fetch(WORKSPACES_PATH, mergeRegistrationScopeForProxy({ headers: { Accept: "application/json" } }));
      if (!res.ok) {
        setWorkspaces(null);
        setListError("Workspace list API is not available yet (expected until GET /v1/tenant/workspaces is implemented).");
        return;
      }
      const json: unknown = await res.json();
      const parsed = parseWorkspacesList(json);
      if (parsed.length === 0) {
        setWorkspaces(null);
        setListError("The workspace list response was empty; scope selection stays read-only.");
        return;
      }
      setWorkspaces(parsed);
      setListError(null);
    } catch (e) {
      setWorkspaces(null);
      setListError(e instanceof Error ? e.message : "Request failed");
    } finally {
      setListLoading(false);
    }
  }, []);

  useEffect(() => {
    if (open) {
      void refreshList();
    }
  }, [open, refreshList]);

  const applyScope = useCallback(
    (row: OperatorScopeRecord) => {
      writeOperatorScopeToStorage(row);
      setTick((n) => n + 1);
      setOpen(false);
    },
    [],
  );

  if (!canShow) {
    return null;
  }

  return (
    <div className="relative z-50 flex min-w-0 max-w-full items-center gap-1">
      <Button
        type="button"
        variant="outline"
        size="sm"
        className="max-w-[min(20rem,42vw)] shrink gap-1 truncate"
        aria-expanded={open}
        aria-haspopup="dialog"
        data-testid="operator-scope-switcher-trigger"
        onClick={() => {
          setOpen((o) => !o);
        }}
      >
        <span className="min-w-0 shrink truncate text-left text-xs font-medium">
          {polishedShell ? (
            <span className="text-neutral-800 dark:text-neutral-200">
              {scopeButtonWorkspace}
              <span className="text-neutral-400 dark:text-neutral-500"> · </span>
              {scopeButtonProject}
            </span>
          ) : (
            <>
              <span className="text-neutral-500 dark:text-neutral-400">W:</span> {workspaceLabel}{" "}
              <span className="text-neutral-400 dark:text-neutral-500">/</span>{" "}
              <span className="text-neutral-500 dark:text-neutral-400">P:</span> {projectLabel}
            </>
          )}
        </span>
        <ChevronsUpDown className="size-3.5 shrink-0 opacity-50" aria-hidden />
      </Button>
      <ContextualHelp helpKey="operator-scope-switcher" className="shrink-0" />
      {open ? (
        <Card
          className="absolute right-0 top-full z-[60] mt-1 w-[min(22rem,calc(100vw-2rem))] space-y-3 p-3 shadow-lg"
          data-testid="operator-scope-switcher-panel"
        >
          <p className="m-0 text-sm text-neutral-600 dark:text-neutral-300">Choose the workspace and project for API scope headers (tenant / workspace / project RLS slice).</p>
          <div className="space-y-1 text-xs text-neutral-500 dark:text-neutral-400">
            <p className="m-0 break-all">
              <span className="text-neutral-600 dark:text-neutral-500">x-tenant-id:</span> {tenantId}
            </p>
            <p className="m-0 break-all">
              <span className="text-neutral-600 dark:text-neutral-500">x-workspace-id:</span> {workspaceId}
            </p>
            <p className="m-0 break-all">
              <span className="text-neutral-600 dark:text-neutral-500">x-project-id:</span> {projectId}
            </p>
          </div>
          {listError !== null ? (
            <p className="m-0 text-xs text-amber-800 dark:text-amber-200" data-testid="operator-scope-list-note">
              {listError}
            </p>
          ) : null}
          {workspaces === null && listLoading ? (
            <p className="m-0 text-sm text-neutral-500">Loading workspace list…</p>
          ) : null}
          {workspaces !== null && workspaces.length > 0 ? (
            <div className="max-h-64 space-y-2 overflow-y-auto" role="list" aria-label="Workspaces and projects">
              {workspaces.map((ws) => {
                if (ws.projects.length === 0) {
                  return null;
                }
                return (
                  <div key={ws.workspaceId} className="space-y-1">
                    <p className="m-0 text-xs font-semibold text-neutral-700 dark:text-neutral-200">{ws.name}</p>
                    {ws.projects.map((pr) => {
                      return (
                        <Button
                          key={pr.projectId}
                          type="button"
                          variant="secondary"
                          size="sm"
                          className="h-8 w-full justify-start"
                          onClick={() => {
                            if (!isNonEmptyId(tenantId)) {
                              return;
                            }
                            applyScope({
                              tenantId: tenantId.trim(),
                              workspaceId: ws.workspaceId,
                              projectId: pr.projectId,
                              workspaceLabel: ws.name,
                              projectLabel: pr.name,
                            });
                          }}
                        >
                          {pr.name}
                        </Button>
                      );
                    })}
                  </div>
                );
              })}
            </div>
          ) : null}
          {stored !== null && !isDevDefaultScopeRecord(stored) ? (
            <div className="space-y-2 border-t border-neutral-200 pt-2 dark:border-neutral-700">
              <Label className="text-xs text-neutral-500">Override</Label>
              <Button
                type="button"
                variant="ghost"
                size="sm"
                className="h-8 w-full"
                onClick={() => {
                  clearOperatorScopeStorage();
                  setTick((n) => n + 1);
                  setOpen(false);
                }}
              >
                Clear custom scope
              </Button>
            </div>
          ) : null}
        </Card>
      ) : null}
      {open ? (
        <button
          type="button"
          className="fixed inset-0 z-[45] cursor-default bg-transparent"
          aria-label="Close scope popover"
          onClick={() => {
            setOpen(false);
          }}
        />
      ) : null}
    </div>
  );
}

function isNonEmptyId(value: string | undefined | null): boolean {
  return value !== null && value !== undefined && value.trim().length > 0;
}
