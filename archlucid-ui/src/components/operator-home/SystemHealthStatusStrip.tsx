"use client";

import Link from "next/link";
import { useEffect, useState } from "react";

import type { HealthReadyResponse } from "@/lib/health-dashboard-types";
import { isNextPublicDemoMode } from "@/lib/demo-ui-env";
import { mergeRegistrationScopeForProxy } from "@/lib/proxy-fetch-registration-scope";
import { cn } from "@/lib/utils";

function healthReadinessDotClass(status: string): string {
  const normalized = status.trim().toLowerCase();

  if (normalized.includes("unhealthy") || normalized.includes("down") || normalized.includes("fail")) {
    return "bg-red-500";
  }

  if (normalized.includes("degraded") || normalized.includes("warn")) {
    return "bg-amber-500";
  }

  if (normalized.includes("healthy") || normalized.includes("ok")) {
    return "bg-emerald-500";
  }

  return "bg-neutral-400";
}

type SystemHealthStatusStripProps = {
  className?: string;
};

/** Readiness as inline metadata (no card chrome) — only shown when a real status is available. */
export function SystemHealthStatusStrip({ className }: SystemHealthStatusStripProps) {
  const [phase, setPhase] = useState<"loading" | "ready" | "unavailable">("loading");
  const [ready, setReady] = useState<HealthReadyResponse | null>(null);

  useEffect(() => {
    if (isNextPublicDemoMode()) {
      return;
    }

    let cancelled = false;

    async function load() {
      setPhase("loading");

      try {
        const res = await fetch(
          "/api/proxy/health/ready",
          mergeRegistrationScopeForProxy({ headers: { Accept: "application/json" }, cache: "no-store" }),
        );

        if (cancelled) {
          return;
        }

        if (!res.ok) {
          setReady(null);
          setPhase("unavailable");

          return;
        }

        const body = (await res.json()) as HealthReadyResponse;
        setReady(body);
        setPhase("ready");
      } catch {
        if (cancelled) {
          return;
        }

        setReady(null);
        setPhase("unavailable");
      }
    }

    void load();

    return () => {
      cancelled = true;
    };
  }, []);

  if (isNextPublicDemoMode()) {
    return null;
  }

  const overall = ready?.status?.trim() ?? "";

  if (phase !== "ready" || overall.length === 0) {
    return null;
  }

  return (
    <div
      data-testid="command-center-health-card"
      className={cn("mb-2 flex flex-wrap items-center gap-2 text-xs", className)}
      aria-label="System health"
    >
      <span
        className={cn("h-2 w-2 shrink-0 rounded-full", healthReadinessDotClass(overall))}
        aria-hidden
      />
      <span className="text-neutral-800 dark:text-neutral-200">
        Platform services: <span className="font-medium">{overall}</span>
      </span>
      <Link
        href="/admin/health"
        className="ml-auto inline-block text-xs font-semibold text-teal-800 underline dark:text-teal-300"
      >
        Details
      </Link>
    </div>
  );
}
