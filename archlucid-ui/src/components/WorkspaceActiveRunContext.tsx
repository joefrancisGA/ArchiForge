"use client";

import {
  createContext,
  useCallback,
  useContext,
  useMemo,
  useState,
  type ReactNode,
} from "react";

const STORAGE_KEY = "archlucid_workspace_active_run_v1";

export type WorkspaceActiveRunContextValue = {
  readonly activeRunId: string | null;
  readonly setActiveRunId: (runId: string | null) => void;
};

const WorkspaceActiveRunContext = createContext<WorkspaceActiveRunContextValue | null>(null);

/**
 * Remembers the last run the operator opened (`/reviews/[runId]`) per browser session so Ask / Graph can stay aligned without pasting IDs.
 */
export function WorkspaceActiveRunProvider({ children }: { readonly children: ReactNode }) {
  const [activeRunId, setActiveRunIdState] = useState<string | null>(() => {
    if (typeof window === "undefined") {
      return null;
    }

    try {
      const raw = sessionStorage.getItem(STORAGE_KEY)?.trim() ?? "";

      return raw.length > 0 ? raw : null;
    } catch {
      return null;
    }
  });

  const setActiveRunId = useCallback((runId: string | null) => {
    const next = runId?.trim() ?? "";

    setActiveRunIdState(next.length > 0 ? next : null);

    try {
      if (next.length > 0) {
        sessionStorage.setItem(STORAGE_KEY, next);

        return;
      }

      sessionStorage.removeItem(STORAGE_KEY);
    } catch {
      /* private mode */
    }
  }, []);

  const value = useMemo<WorkspaceActiveRunContextValue>(
    () => ({
      activeRunId,
      setActiveRunId,
    }),
    [activeRunId, setActiveRunId],
  );

  return <WorkspaceActiveRunContext.Provider value={value}>{children}</WorkspaceActiveRunContext.Provider>;
}

export function useWorkspaceActiveRun(): WorkspaceActiveRunContextValue | null {
  return useContext(WorkspaceActiveRunContext);
}
