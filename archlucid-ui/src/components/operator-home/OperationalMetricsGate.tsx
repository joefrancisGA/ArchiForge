"use client";

import { useEffect, useState, type ReactNode } from "react";

import { fetchCorePilotCommitContext } from "@/lib/core-pilot-commit-context";

type Phase = "loading" | "ready";

/**
 * Hides tertiary operator-home surfaces until at least one committed manifest exists for the tenant (trial-status
 * anchor or golden manifest on a run row). Fails open on resolution errors so transient API issues do not strip the
 * dashboard for returning operators.
 */
export function OperationalMetricsGate({ children }: { children: ReactNode }) {
  const [phase, setPhase] = useState<Phase>("loading");
  const [showOperateDiscovery, setShowOperateDiscovery] = useState(false);

  useEffect(() => {
    let cancelled = false;

    async function load() {
      setPhase("loading");

      try {
        const ctx = await fetchCorePilotCommitContext();

        if (cancelled) {
          return;
        }

        setShowOperateDiscovery(ctx.hasCommittedManifest);
        setPhase("ready");
      } catch {
        if (cancelled) {
          return;
        }

        setShowOperateDiscovery(true);
        setPhase("ready");
      }
    }

    void load();

    return () => {
      cancelled = true;
    };
  }, []);

  if (phase === "loading") {
    return null;
  }

  if (phase === "ready" && !showOperateDiscovery) {
    return null;
  }

  return <>{children}</>;
}
