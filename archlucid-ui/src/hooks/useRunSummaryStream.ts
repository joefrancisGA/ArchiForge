"use client";

import { useEffect, useRef, useState } from "react";

import { getRunSummary } from "@/lib/api";
import type { RunSummary } from "@/types/authority";

export type RunSummaryStreamPhase = "streaming" | "poll-fallback" | "complete";

export type UseRunSummaryStreamResult = {
  summary: RunSummary | null;
  streamPhase: RunSummaryStreamPhase;
  sseConnected: boolean;
};

const FALLBACK_POLL_MS = 3000;

/**
 * Live run summary updates via SSE (`GET /v1/authority/runs/{id}/events` through `/api/proxy`), with HTTP polling fallback.
 */
export function useRunSummaryStream(
  runId: string | null,
  options: { enabled: boolean; initialSummary?: RunSummary | null; retryToken?: number },
): UseRunSummaryStreamResult {
  const initial = options.initialSummary ?? null;
  const retryToken = options.retryToken ?? 0;
  const [summary, setSummary] = useState<RunSummary | null>(initial);
  const [streamPhase, setStreamPhase] = useState<RunSummaryStreamPhase>("streaming");
  const [sseConnected, setSseConnected] = useState(false);
  const fallbackStartedRef = useRef(false);
  const fallbackIntervalRef = useRef<number | undefined>(undefined);

  useEffect(() => {
    setSummary(initial);
  }, [initial]);

  useEffect(() => {
    if (!options.enabled || runId === null || runId.length === 0) {
      return;
    }

    fallbackStartedRef.current = false;
    let cancelled = false;
    const url = `${typeof window !== "undefined" ? window.location.origin : ""}/api/proxy/v1/authority/runs/${encodeURIComponent(runId)}/events`;

    const clearFallback = () => {
      if (fallbackIntervalRef.current !== undefined) {
        window.clearInterval(fallbackIntervalRef.current);
        fallbackIntervalRef.current = undefined;
      }
    };

    const startPollingFallback = () => {
      if (cancelled || fallbackStartedRef.current) {
        return;
      }

      fallbackStartedRef.current = true;
      setSseConnected(false);
      setStreamPhase("poll-fallback");

      const tick = async () => {
        try {
          const next = await getRunSummary(runId);

          if (cancelled) {
            return;
          }

          setSummary(next);

          if (next.hasGoldenManifest) {
            clearFallback();
            setStreamPhase("complete");
          }
        } catch {
          /* keep polling */
        }
      };

      void tick();
      fallbackIntervalRef.current = window.setInterval(() => void tick(), FALLBACK_POLL_MS);
    };

    let es: EventSource | null = null;

    try {
      es = new EventSource(url);
    } catch {
      startPollingFallback();

      return () => {
        cancelled = true;
        clearFallback();
      };
    }

    es.onopen = () => {
      if (!cancelled) {
        setSseConnected(true);
        setStreamPhase("streaming");
      }
    };

    es.addEventListener("status", (ev: MessageEvent) => {
      if (cancelled || typeof ev.data !== "string") {
        return;
      }

      try {
        const parsed = JSON.parse(ev.data) as RunSummary;
        setSummary(parsed);
      } catch {
        /* ignore malformed chunk */
      }
    });

    es.addEventListener("complete", () => {
      if (cancelled) {
        return;
      }

      cancelled = true;
      clearFallback();
      setStreamPhase("complete");
      es?.close();
    });

    es.addEventListener("error", () => {
      if (cancelled) {
        return;
      }

      es?.close();
      startPollingFallback();
    });

    return () => {
      cancelled = true;
      clearFallback();
      es?.close();
    };
  }, [runId, options.enabled, retryToken]);

  return { summary, streamPhase, sseConnected };
}
