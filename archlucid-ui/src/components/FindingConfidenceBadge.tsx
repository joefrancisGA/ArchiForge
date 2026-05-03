import { Shield } from "lucide-react";

import type { FindingConfidenceLevel } from "@/types/explanation";

export type FindingConfidenceBadgeProps = {
  level: FindingConfidenceLevel | null | undefined;
};

/**
 * Compact pill for evaluation-derived coarse confidence (harness + reference-case + trace completeness).
 */
export function FindingConfidenceBadge({ level }: FindingConfidenceBadgeProps) {
  if (level !== "High" && level !== "Medium" && level !== "Low") {
    return null;
  }

  const cfg =
    level === "High"
      ? {
          label: "High confidence",
          pillClass:
            "border-emerald-200 bg-emerald-50 text-emerald-900 dark:border-emerald-900 dark:bg-emerald-950/50 dark:text-emerald-50",
          iconClass: "text-emerald-700 dark:text-emerald-400",
        }
      : level === "Medium"
        ? {
            label: "Medium confidence",
            pillClass:
              "border-amber-200 bg-amber-50 text-amber-950 dark:border-amber-900 dark:bg-amber-950/45 dark:text-amber-50",
            iconClass: "text-amber-700 dark:text-amber-400",
          }
        : {
            label: "Low confidence",
            pillClass:
              "border-orange-200 bg-orange-50 text-orange-950 dark:border-orange-900 dark:bg-orange-950/40 dark:text-orange-50",
            iconClass: "text-orange-700 dark:text-orange-400",
          };

  return (
    <span
      role="status"
      aria-label={cfg.label}
      data-archlucid-confidence={level}
      className={`finding-confidence-badge inline-flex items-center gap-1 rounded-full border px-2 py-0.5 text-xs font-medium ${cfg.pillClass}`}
    >
      <Shield className={`h-3.5 w-3.5 shrink-0 ${cfg.iconClass}`} aria-hidden />
      {cfg.label}
    </span>
  );
}
