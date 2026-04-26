"use client";

import Link from "next/link";
import { useEffect, useState } from "react";

import type { HealthReadyResponse } from "@/lib/health-dashboard-types";
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

/** Readiness as inline metadata (no card chrome) above the runs dashboard. */
export function SystemHealthStatusStrip() {
  const [phase, setPhase] = useState<"loading" | "ready" | "unavailable">("loading");
  const [ready, setReady] = useState<HealthReadyResponse | null>(null);

  useEffect(() => {
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

  const overall = ready?.status?.trim() ?? "";

  return (
    <div
      data-testid="command-center-health-card"
      className="mb-2 flex flex-wrap items-center gap-2 text-xs"
      aria-label="System health"
    >
      {phase === "loading" ? (
        <span className="text-neutral-500 dark:text-neutral-400">Checking readiness…</span>
      ) : null}

      {phase === "unavailable" ? (
        <>
          <span className="h-2 w-2 shrink-0 rounded-full bg-amber-500" aria-hidden />
          <span className="text-neutral-600 dark:text-neutral-400">Health dashboard not configured yet.</span>
        </>
      ) : null}

      {phase === "ready" && overall.length > 0 ? (
        <>
          <span
            className={cn("h-2 w-2 shrink-0 rounded-full", healthReadinessDotClass(overall))}
            aria-hidden
          />
          <span className="text-neutral-800 dark:text-neutral-200">
            Platform services: <span className="font-medium">{overall}</span>
          </span>
        </>
      ) : null}

      {phase === "ready" && overall.length === 0 ? (
        <span className="text-neutral-600 dark:text-neutral-400">Readiness payload had no overall status.</span>
      ) : null}

      <Link
        href="/admin/health"
        className="ml-auto inline-block text-xs font-semibold text-teal-800 underline dark:text-teal-300"
      >
        Details
      </Link>
    </div>
  );
}
