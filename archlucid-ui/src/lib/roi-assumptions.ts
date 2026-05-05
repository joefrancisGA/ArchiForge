/** Default coefficients for “hours surfaced pre-commit” — editable only in source for v1.1; $/hour is Admin UI + localStorage. */

import type { PilotValueReportSeverityJson } from "@/types/pilot-value-report";

export const HOURS_PER_CRITICAL = 8;
export const HOURS_PER_HIGH = 3;
export const HOURS_PER_MEDIUM = 1;
export const HOURS_PER_PRECOMMIT_BLOCK = 2;
export const DEFAULT_LOADED_HOURLY_USD = 150;
export const ROI_HOURLY_USD_STORAGE_KEY = "archlucid.roi.hourlyUsd";

/**
 * Loaded $/hour for ROI tiles (browser localStorage). Server/components must pass a safe default when `window` is absent.
 */
export function readStoredHourlyUsd(): number {
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

export type RoiHoursCoefficients = {
  hoursPerCritical: number;
  hoursPerHigh: number;
  hoursPerMedium: number;
  hoursPerPrecommitBlock: number;
};

export const DEFAULT_ROI_HOURS_COEFFICIENTS: RoiHoursCoefficients = {
  hoursPerCritical: HOURS_PER_CRITICAL,
  hoursPerHigh: HOURS_PER_HIGH,
  hoursPerMedium: HOURS_PER_MEDIUM,
  hoursPerPrecommitBlock: HOURS_PER_PRECOMMIT_BLOCK,
};

/**
 * Linear estimate: weighted severity findings + premium per pre-commit block (same window as inputs).
 */
export function hoursSurfaced(
  severity: Pick<PilotValueReportSeverityJson, "critical" | "high" | "medium">,
  precommitBlocks: number,
  coefficients: RoiHoursCoefficients = DEFAULT_ROI_HOURS_COEFFICIENTS,
): number {
  const s =
    severity.critical * coefficients.hoursPerCritical +
    severity.high * coefficients.hoursPerHigh +
    severity.medium * coefficients.hoursPerMedium +
    precommitBlocks * coefficients.hoursPerPrecommitBlock;

  return Number.isFinite(s) ? s : 0;
}

export function formatHours(hours: number): string {
  if (!Number.isFinite(hours) || hours <= 0) {
    return "0 h";
  }

  if (hours >= 100) {
    return `${Math.round(hours)} h`;
  }

  return `${hours.toFixed(1)} h`;
}

export function formatUsd(amount: number): string {
  if (!Number.isFinite(amount)) {
    return "—";
  }

  return new Intl.NumberFormat(undefined, { style: "currency", currency: "USD", maximumFractionDigits: 0 }).format(amount);
}
