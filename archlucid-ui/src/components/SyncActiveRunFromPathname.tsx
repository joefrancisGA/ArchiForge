"use client";

import { useEffect } from "react";
import { usePathname } from "next/navigation";

import { useWorkspaceActiveRun } from "@/components/WorkspaceActiveRunContext";

/**
 * Tracks `/reviews/[runId]` navigation (excluding `/reviews/new`) and stores the active run id for downstream pickers (Ask / Graph).
 */
export function SyncActiveRunFromPathname(): null {
  const pathname = usePathname();
  const ctx = useWorkspaceActiveRun();

  useEffect(() => {
    if (ctx === null) {
      return;
    }

    const executiveMatch = /^\/executive\/reviews\/([^/]+)/.exec(pathname);
    const reviewMatch = /^\/reviews\/([^/]+)/.exec(pathname);
    const legacyRunsMatch = /^\/runs\/([^/]+)/.exec(pathname);
    const segment = (executiveMatch?.[1] ?? reviewMatch?.[1] ?? legacyRunsMatch?.[1])?.trim();

    if (!segment || segment.length === 0 || segment === "new") {
      return;
    }

    ctx.setActiveRunId(segment);
  }, [ctx, pathname]);

  return null;
}
