"use client";

import { useEffect, useState } from "react";

import { mergeRegistrationScopeForProxy } from "@/lib/proxy-fetch-registration-scope";

import type { RecentPilotRunDeltasPayload } from "./types";

/**
 * Shared loader for the `BeforeAfterDeltaPanel` top / sidebar / inline variants.
 *
 * Centralises three things so each variant component is presentation-only:
 *  1. The `/api/proxy/v1/pilots/runs/recent-deltas` URL shape and `count` query param.
 *  2. The JWT-vs-registration-scope header dance via `mergeRegistrationScopeForProxy`.
 *  3. The "loading | ready | error" state machine + cancellation on unmount so a
 *     route navigation mid-flight does not log a setState-on-unmounted warning.
 *
 * Returns `data === null` when the panel should render nothing (loading, error,
 * or zero committed runs) — the variants treat the three terminal states the same
 * way so a partial outage degrades gracefully to "panel hidden", never broken UI.
 */
export type DeltaQueryState = {
  status: "loading" | "ready" | "error";
  data: RecentPilotRunDeltasPayload | null;
};

export type UseDeltaQueryOptions = {
  /** Number of most recent committed runs to aggregate over. Server clamps to [1, 25]. */
  count: number;
};

const RECENT_DELTAS_PROXY_PATH = "/api/proxy/v1/pilots/runs/recent-deltas";

export function useDeltaQuery({ count }: UseDeltaQueryOptions): DeltaQueryState {
  const [state, setState] = useState<DeltaQueryState>({ status: "loading", data: null });

  useEffect(() => {
    let cancelled = false;

    async function load(): Promise<void> {
      try {
        const url = `${RECENT_DELTAS_PROXY_PATH}?count=${encodeURIComponent(String(count))}`;
        const res = await fetch(
          url,
          mergeRegistrationScopeForProxy({ headers: { Accept: "application/json" } }),
        );

        if (!res.ok) {
          if (!cancelled) setState({ status: "error", data: null });

          return;
        }

        const payload = (await res.json()) as RecentPilotRunDeltasPayload;

        if (cancelled) return;

        setState({ status: "ready", data: payload });
      } catch {
        if (!cancelled) setState({ status: "error", data: null });
      }
    }

    void load();

    return () => {
      cancelled = true;
    };
  }, [count]);

  return state;
}
