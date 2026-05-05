"use client";

import { useEffect, useState } from "react";

import { Input } from "@/components/ui/input";
import {
  DEFAULT_LOADED_HOURLY_USD,
  ROI_HOURLY_USD_STORAGE_KEY,
  formatHours,
  formatUsd,
  hoursSurfaced,
} from "@/lib/roi-assumptions";
import type { PilotValueReportSeverityJson } from "@/types/pilot-value-report";

export type RoiTelemetryCardProps = {
  title: string;
  /** Rolling vs pilot-to-date — copy only */
  windowLabel: string;
  /** Stable id suffix when multiple cards on one page (e.g. rolling30, pilot). */
  domSuffix: string;
  severity: Pick<PilotValueReportSeverityJson, "critical" | "high" | "medium">;
  precommitBlocks: number;
  precommitBlocksExact: boolean;
  isAdmin: boolean;
};

function readStoredHourlyUsd(): number {
  if (typeof window === "undefined") {
    return DEFAULT_LOADED_HOURLY_USD;
  }

  try {
    const raw = window.localStorage.getItem(ROI_HOURLY_USD_STORAGE_KEY);

    if (raw === null || raw.trim().length === 0) {
      return DEFAULT_LOADED_HOURLY_USD;
    }

    const n = Number(raw);

    return Number.isFinite(n) && n > 0 ? n : DEFAULT_LOADED_HOURLY_USD;
  } catch {
    return DEFAULT_LOADED_HOURLY_USD;
  }
}

/**
 * Hours-first ROI tile; Admin sees loaded $/hour (localStorage) and implied USD total.
 */
export function RoiTelemetryCard(props: RoiTelemetryCardProps) {
  const hours = hoursSurfaced(props.severity, props.precommitBlocks);
  const [hourlyUsd, setHourlyUsd] = useState<number>(DEFAULT_LOADED_HOURLY_USD);
  const [mounted, setMounted] = useState(false);

  useEffect(() => {
    setMounted(true);
    setHourlyUsd(readStoredHourlyUsd());
  }, []);

  function persistHourlyUsd(next: number): void {
    setHourlyUsd(next);

    try {
      window.localStorage.setItem(ROI_HOURLY_USD_STORAGE_KEY, String(next));
    } catch {
      /* private mode */
    }
  }

  const blockLabel = props.precommitBlocksExact
    ? String(props.precommitBlocks)
    : `${props.precommitBlocks} (sampled)`;

  const usdTotal = hours * hourlyUsd;
  const hourlyIsDefault = Math.abs(hourlyUsd - DEFAULT_LOADED_HOURLY_USD) < 1e-6;

  return (
    <section
      className="rounded-lg border border-neutral-200 bg-white p-4 dark:border-neutral-800 dark:bg-neutral-900"
      aria-labelledby={`roi-card-${props.domSuffix}`}
    >
      <h2
        id={`roi-card-${props.domSuffix}`}
        className="m-0 text-base font-semibold text-neutral-900 dark:text-neutral-100"
      >
        {props.title}
      </h2>
      <p className="m-0 mt-1 text-xs text-neutral-500 dark:text-neutral-400">{props.windowLabel}</p>
      <p className="m-0 mt-3 font-mono text-2xl font-semibold tabular-nums text-neutral-900 dark:text-neutral-100">
        {formatHours(hours)}
      </p>
      <p className="m-0 mt-1 text-xs text-neutral-600 dark:text-neutral-400">
        Model: 8×Critical + 3×High + 1×Medium + 2×pre-commit blocks. Blocks in window:{" "}
        <span title={props.precommitBlocksExact ? undefined : "Audit search may be capped; count is a lower bound."}>
          {blockLabel}
        </span>
        .
      </p>
      {props.isAdmin ? (
        <div className="mt-4 space-y-2 rounded-md border border-neutral-100 bg-neutral-50 p-3 text-sm dark:border-neutral-800 dark:bg-neutral-950/50">
          <div className="flex flex-wrap items-center gap-2">
            <label className="text-xs font-medium text-neutral-600 dark:text-neutral-400" htmlFor={`hourly-usd-${props.domSuffix}`}>
              Loaded cost / hour (USD)
            </label>
            {!hourlyIsDefault ? (
              <span className="rounded bg-amber-100 px-1.5 py-0.5 text-[10px] font-medium text-amber-950 dark:bg-amber-950/40 dark:text-amber-100">
                local override
              </span>
            ) : null}
          </div>
          <Input
            id={`hourly-usd-${props.domSuffix}`}
            type="number"
            inputMode="decimal"
            min={1}
            step={1}
            className="max-w-[12rem] font-mono text-sm"
            value={mounted ? hourlyUsd : DEFAULT_LOADED_HOURLY_USD}
            disabled={!mounted}
            aria-label="Loaded engineering cost per hour in US dollars"
            onChange={(e) => {
              const n = Number(e.target.value);

              if (Number.isFinite(n) && n > 0) {
                persistHourlyUsd(n);
              }
            }}
          />
          <p className="m-0 text-xs text-neutral-600 dark:text-neutral-400">
            Implied total: <span className="font-mono font-medium">{formatUsd(usdTotal)}</span> (estimate only; not an
            invoice).
          </p>
        </div>
      ) : null}
    </section>
  );
}
