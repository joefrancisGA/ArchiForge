"use client";

import Link from "next/link";
import { useEffect, useState } from "react";

import { Label } from "@/components/ui/label";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { SHOWCASE_STATIC_DEMO_RUN_ID } from "@/lib/showcase-static-demo";
import { loadProjectRunsMergedWithDemoFallback } from "@/lib/operator-run-picker-client";
import type { RunSummary } from "@/types/authority";

/** Preferred demo run id when multiple rows exist and demo mode is enabled (`NEXT_PUBLIC_DEMO_MODE`). */
const DEMO_RUN_PREF_ID = "claims-intake-modernization";

export type AskRunIdPickerProps = {
  readonly value: string;
  readonly onChange: (runId: string) => void;
  readonly selectedThreadId: string;
  /**
   * When false, do not auto-select the demo / first listed run while `value` is empty — use for paired compare/base pickers.
   * Defaults to true.
   */
  readonly preferAutoPick?: boolean;
  readonly label?: string;
  /** When true, the control cannot be changed (e.g. read-only governance submit at Reader rank). */
  readonly disabled?: boolean;
  /** Stable DOM id suffix so multiple pickers avoid duplicate ids (defaults to primary run field). */
  readonly fieldId?: string;
};

/**
 * Loads recent runs for the default project and prefers a combobox over raw IDs.
 * When the list is empty or unavailable, renders a disabled selector plus guidance — not a paste-ID field.
 */
export function AskRunIdPicker(props: AskRunIdPickerProps) {
  const {
    value,
    onChange,
    selectedThreadId,
    preferAutoPick = true,
    label,
    fieldId,
    disabled = false,
  } = props;
  const [items, setItems] = useState<RunSummary[]>([]);
  const [loading, setLoading] = useState(true);
  const [loadError, setLoadError] = useState(false);

  const labelText = label ?? "Review";
  const controlIdPrefix = fieldId ?? "ask-run-primary";
  const selectControlId = `${controlIdPrefix}-select`;

  useEffect(() => {
    let cancelled = false;

    void (async () => {
      setLoading(true);

      try {
        const merged = await loadProjectRunsMergedWithDemoFallback("default");

        if (!cancelled) {
          setItems(merged.items);
          setLoadError(merged.loadError);
        }
      } catch {
        if (!cancelled) {
          setLoadError(true);
        }
      } finally {
        if (!cancelled) {
          setLoading(false);
        }
      }
    })();

    return () => {
      cancelled = true;
    };
  }, []);

  useEffect(() => {
    if (!preferAutoPick) {
      return;
    }

    if (loading) {
      return;
    }

    if (value.trim().length > 0) {
      return;
    }

    if (items.length === 0) {
      return;
    }

    const demoMode =
      process.env.NEXT_PUBLIC_DEMO_MODE === "true" || process.env.NEXT_PUBLIC_DEMO_MODE === "1";
    const demoPreferred = items.find((r) => r.runId === DEMO_RUN_PREF_ID);

    const firstItem = items[0];

    if (items.length === 1 && firstItem !== undefined) {
      onChange(firstItem.runId);

      return;
    }

    if (demoMode && demoPreferred !== undefined) {
      onChange(demoPreferred.runId);
    }
  }, [loading, items, value, onChange, preferAutoPick]);

  /**
   * Demo fallback lists zero runs without API error — keep parent state in sync so Graph / Governance receive a run id.
   * Without this, the Select displays the synthetic row while `value` stays empty upstream.
   */
  useEffect(() => {
    if (!preferAutoPick) {
      return;
    }

    if (loading || loadError) {
      return;
    }

    const demoMode =
      process.env.NEXT_PUBLIC_DEMO_MODE === "true" || process.env.NEXT_PUBLIC_DEMO_MODE === "1";

    if (!demoMode || items.length > 0) {
      return;
    }

    if (value.trim().length > 0) {
      return;
    }

    onChange(SHOWCASE_STATIC_DEMO_RUN_ID);
  }, [loading, loadError, items, preferAutoPick, value, onChange]);

  useEffect(() => {
    if (!loadError) {
      return;
    }

    if (!preferAutoPick) {
      return;
    }

    const demoMode =
      process.env.NEXT_PUBLIC_DEMO_MODE === "true" || process.env.NEXT_PUBLIC_DEMO_MODE === "1";

    if (!demoMode) {
      return;
    }

    if (value.trim().length > 0) {
      return;
    }

    onChange(SHOWCASE_STATIC_DEMO_RUN_ID);
  }, [loadError, preferAutoPick, value, onChange]);

  const optionalCopy =
    selectedThreadId.trim().length > 0 ? "(optional when a context is already selected)" : "(select an architecture review)";

  if (loadError) {
    const demoMode =
      process.env.NEXT_PUBLIC_DEMO_MODE === "true" || process.env.NEXT_PUBLIC_DEMO_MODE === "1";

    if (demoMode) {
      const selectedInSynthetic = value === SHOWCASE_STATIC_DEMO_RUN_ID;

      return (
        <div className="space-y-2">
          <Label htmlFor={selectControlId}>
            {labelText} {optionalCopy}
          </Label>
          <Select
            disabled={disabled}
            value={
              selectedInSynthetic ? SHOWCASE_STATIC_DEMO_RUN_ID : value.trim().length > 0 ? value : undefined
            }
            onValueChange={onChange}
          >
            <SelectTrigger id={selectControlId} className="font-mono text-sm">
              <SelectValue placeholder="Choose demo run" />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value={SHOWCASE_STATIC_DEMO_RUN_ID}>Claims Intake Modernization Run</SelectItem>
            </SelectContent>
          </Select>
          <p className="m-0 text-xs text-neutral-600 dark:text-neutral-400">
            Runs list unavailable — demo mode uses the Claims Intake sample run for Ask.
          </p>
        </div>
      );
    }

    return (
      <div className="space-y-2">
        <Label htmlFor={selectControlId}>
          {labelText} {optionalCopy}
        </Label>
        <Select disabled>
          <SelectTrigger id={selectControlId} className="font-mono text-sm">
            <SelectValue placeholder="Runs list unavailable" />
          </SelectTrigger>
        </Select>
        <p className="m-0 text-xs text-neutral-600 dark:text-neutral-400">
          The review list could not be loaded — open an existing conversation from the left, or try again shortly.
        </p>
      </div>
    );
  }

  if (loading) {
    return (
      <div className="space-y-2">
        <Label htmlFor={selectControlId}>{labelText}</Label>
        <Select disabled>
          <SelectTrigger id={selectControlId} className="font-mono text-sm">
            <SelectValue placeholder="Loading runs…" />
          </SelectTrigger>
        </Select>
      </div>
    );
  }

  if (items.length === 0) {
    const demoMode =
      process.env.NEXT_PUBLIC_DEMO_MODE === "true" || process.env.NEXT_PUBLIC_DEMO_MODE === "1";

    if (demoMode && preferAutoPick) {
      return (
        <div className="space-y-2">
          <Label htmlFor={selectControlId}>
            {labelText} {optionalCopy}
          </Label>
          <Select
            disabled={disabled}
            value={value.trim().length > 0 ? value : SHOWCASE_STATIC_DEMO_RUN_ID}
            onValueChange={onChange}
          >
            <SelectTrigger id={selectControlId} className="font-mono text-sm">
              <SelectValue placeholder="Choose demo run" />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value={SHOWCASE_STATIC_DEMO_RUN_ID}>Claims Intake Modernization Run</SelectItem>
            </SelectContent>
          </Select>
          <p className="m-0 text-xs text-neutral-600 dark:text-neutral-400">
            Runs list unavailable — demo mode uses the Claims Intake sample run.
          </p>
        </div>
      );
    }

    return (
      <div className="space-y-2">
        <Label htmlFor={selectControlId}>
          {labelText} {optionalCopy}
        </Label>
        <Select disabled>
          <SelectTrigger id={selectControlId} className="font-mono text-sm">
            <SelectValue placeholder="No reviews in this workspace yet" />
          </SelectTrigger>
        </Select>
        <p className="m-0 text-xs text-neutral-600 dark:text-neutral-400">
          <Link className="font-medium text-teal-800 underline dark:text-teal-300" href="/reviews/new">
            Create a request
          </Link>{" "}
          to add a review, open an existing conversation from the left, or{" "}
          <Link className="font-medium text-teal-800 underline dark:text-teal-300" href="/showcase/claims-intake-modernization">
            browse the Claims Intake sample scenario
          </Link>{" "}
          (read-only).
        </p>
      </div>
    );
  }

  const selectedInList = items.some((r) => r.runId === value);
  const selectValue = selectedInList ? value : undefined;

  return (
    <div className="space-y-2">
      <Label htmlFor={selectControlId}>{labelText}</Label>
      <Select disabled={disabled} value={selectValue} onValueChange={onChange}>
        <SelectTrigger id={selectControlId} className="font-mono text-sm">
          <SelectValue placeholder="Choose a run" />
        </SelectTrigger>
        <SelectContent>
          {items.map((row) => (
            <SelectItem key={row.runId} value={row.runId}>
              {(row.description ?? "").trim().length > 0 ? row.description : row.runId}
            </SelectItem>
          ))}
        </SelectContent>
      </Select>
    </div>
  );
}
