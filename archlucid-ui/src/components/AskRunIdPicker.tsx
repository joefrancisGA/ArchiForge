"use client";

import { useEffect, useState } from "react";

import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { listRunsByProjectPaged } from "@/lib/api";
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
  /** Stable DOM id suffix so multiple pickers avoid duplicate ids (defaults to primary run field). */
  readonly fieldId?: string;
};

/**
 * Loads recent runs for the default project and prefers a combobox over raw IDs.
 * Falls back to manual entry when the list endpoint fails or returns no rows.
 */
export function AskRunIdPicker(props: AskRunIdPickerProps) {
  const {
    value,
    onChange,
    selectedThreadId,
    preferAutoPick = true,
    label,
    fieldId,
  } = props;
  const [items, setItems] = useState<RunSummary[]>([]);
  const [loading, setLoading] = useState(true);
  const [loadError, setLoadError] = useState(false);

  const labelText = label ?? "Run";
  const controlIdPrefix = fieldId ?? "ask-run-primary";
  const selectControlId = `${controlIdPrefix}-select`;
  const inputControlId = `${controlIdPrefix}-input`;

  useEffect(() => {
    let cancelled = false;

    void (async () => {
      setLoading(true);

      try {
        const page = await listRunsByProjectPaged("default", 1, 50);

        if (!cancelled) {
          setItems(page.items);
          setLoadError(false);
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

    if (items.length === 1) {
      onChange(items[0].runId);

      return;
    }

    if (demoMode && demoPreferred !== undefined) {
      onChange(demoPreferred.runId);
    }
  }, [loading, items, value, onChange, preferAutoPick]);

  const optionalCopy =
    selectedThreadId.trim().length > 0 ? "(optional if thread already anchored)" : "(required for new thread)";

  if (loadError) {
    return (
      <div className="space-y-2">
        <Label htmlFor={inputControlId}>
          {labelText} ID {optionalCopy}
        </Label>
        <p className="m-0 text-xs text-neutral-500 dark:text-neutral-400">
          Could not load runs — enter a run ID manually.
        </p>
        <Input
          id={inputControlId}
          className="font-mono text-sm"
          value={value}
          onChange={(e) => onChange(e.target.value)}
          placeholder="Paste a run ID from Run detail"
          autoComplete="off"
        />
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
    return (
      <div className="space-y-2">
        <Label htmlFor={inputControlId}>
          {labelText} ID {optionalCopy}
        </Label>
        <Input
          id={inputControlId}
          className="font-mono text-sm"
          value={value}
          onChange={(e) => onChange(e.target.value)}
          placeholder="No runs in this project yet — paste a run ID"
          autoComplete="off"
        />
      </div>
    );
  }

  const selectedInList = items.some((r) => r.runId === value);
  const selectValue = selectedInList ? value : undefined;

  return (
    <div className="space-y-2">
      <Label htmlFor={selectControlId}>{labelText}</Label>
      <Select value={selectValue} onValueChange={onChange}>
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
