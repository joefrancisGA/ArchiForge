"use client";

import { useCallback, useEffect, useId, useMemo, useState } from "react";

import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { listRunsByProjectPaged } from "@/lib/api";
import type { RunSummary } from "@/types/authority";
import { cn } from "@/lib/utils";

type RunIdPickerProps = {
  value: string;
  onChange: (runId: string) => void;
  placeholder: string;
  label: string;
  projectId?: string;
  inputId?: string;
};

function truncate(text: string, max: number): string {
  const t = text.trim();

  if (t.length <= max)
  {
    return t;
  }

  return `${t.slice(0, max - 1)}…`;
}

/**
 * Run ID text field with debounced typeahead over recent runs (server list + local filter).
 */
export function RunIdPicker({
  value,
  onChange,
  placeholder,
  label,
  projectId = "default",
  inputId,
}: RunIdPickerProps) {
  const generatedId = useId();
  const controlId = inputId ?? `run-id-picker-${generatedId}`;
  const [query, setQuery] = useState(value);
  const [runs, setRuns] = useState<RunSummary[]>([]);
  const [open, setOpen] = useState(false);
  const [loading, setLoading] = useState(false);
  const [loadError, setLoadError] = useState<string | null>(null);

  useEffect(() => {
    setQuery(value);
  }, [value]);

  const loadRuns = useCallback(async () => {
    setLoading(true);
    setLoadError(null);

    try {
      const page = await listRunsByProjectPaged(projectId, 1, 50);
      setRuns(page.items ?? []);
    }
    catch {
      setRuns([]);
      setLoadError("Could not load runs list.");
    }
    finally {
      setLoading(false);
    }
  }, [projectId]);

  const filtered = useMemo(() => {
    const q = query.trim().toLowerCase();

    if (q.length === 0)
    {
      return runs.slice(0, 12);
    }

    return runs
      .filter(
        (r) =>
          r.runId.toLowerCase().includes(q) ||
          (r.description ?? "").toLowerCase().includes(q) ||
          (r.projectId ?? "").toLowerCase().includes(q),
      )
      .slice(0, 12);
  }, [runs, query]);

  return (
    <div className="relative max-w-xl">
      <Label htmlFor={controlId} className="mb-1 block text-sm font-medium text-neutral-800 dark:text-neutral-200">
        {label}
      </Label>
      <Input
        id={controlId}
        value={query}
        placeholder={placeholder}
        autoComplete="off"
        aria-autocomplete="list"
        aria-expanded={open}
        aria-controls={`${controlId}-listbox`}
        onFocus={() => {
          setOpen(true);
          void loadRuns();
        }}
        onBlur={() => {
          window.setTimeout(() => {
            setOpen(false);
          }, 150);
        }}
        onChange={(e) => {
          const next = e.target.value;
          setQuery(next);
          onChange(next);
          setOpen(true);
        }}
      />
      {open && (filtered.length > 0 || loadError !== null || loading) ? (
        <ul
          id={`${controlId}-listbox`}
          role="listbox"
          className="absolute z-30 mt-1 max-h-60 w-full overflow-auto rounded-md border border-neutral-200 bg-white py-1 text-sm shadow-md dark:border-neutral-700 dark:bg-neutral-900"
        >
          {loading ? (
            <li className="px-3 py-2 text-neutral-500 dark:text-neutral-400" role="presentation">
              Loading runs…
            </li>
          ) : null}
          {loadError ? (
            <li className="px-3 py-2 text-red-700 dark:text-red-400" role="presentation">
              {loadError}
            </li>
          ) : null}
          {!loading &&
            filtered.map((r) => (
              <li key={r.runId} role="presentation">
                <button
                  type="button"
                  role="option"
                  className={cn(
                    "flex w-full flex-col items-start gap-0.5 px-3 py-2 text-left hover:bg-neutral-100 dark:hover:bg-neutral-800",
                    r.runId === value.trim() && "bg-teal-50 dark:bg-teal-900/20",
                  )}
                  onMouseDown={(e) => {
                    e.preventDefault();
                  }}
                  onClick={() => {
                    setQuery(r.runId);
                    onChange(r.runId);
                    setOpen(false);
                  }}
                >
                  <span className="font-mono text-xs text-neutral-900 dark:text-neutral-100">{truncate(r.runId, 40)}</span>
                  <span className="text-xs text-neutral-600 dark:text-neutral-400">
                    {truncate(r.description ?? r.projectId ?? "—", 60)}
                  </span>
                </button>
              </li>
            ))}
        </ul>
      ) : null}
    </div>
  );
}
