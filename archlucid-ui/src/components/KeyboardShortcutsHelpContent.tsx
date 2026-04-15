"use client";

import { Fragment, useMemo } from "react";
import type React from "react";

import { ALERTS_PAGE_SHORTCUTS, SHORTCUTS, type ShortcutEntry } from "@/lib/shortcut-registry";

function formatKeyPart(part: string): string {
  const trimmed = part.trim().toLowerCase();

  if (trimmed === "?") {
    return "?";
  }

  if (trimmed.length === 1) {
    return trimmed.toUpperCase();
  }

  return trimmed.charAt(0).toUpperCase() + trimmed.slice(1);
}

function ShortcutComboKbd({ combo }: { combo: string }) {
  const parts = combo.split("+").map((segment) => segment.trim());

  return (
    <span className="inline-flex flex-wrap items-center gap-1">
      {parts.map((part, index) => (
        <Fragment key={`${part}-${index}`}>
          {index > 0 ? <span className="text-neutral-400 dark:text-neutral-500">+</span> : null}
          <kbd>{formatKeyPart(part)}</kbd>
        </Fragment>
      ))}
    </span>
  );
}

function ShortcutTable({
  entries,
  caption,
}: {
  entries: ReadonlyArray<{ key: string; description: string }>;
  caption: string;
}) {
  if (entries.length === 0) {
    return null;
  }

  return (
    <div className="space-y-2">
      <h3 className="text-xs font-semibold uppercase tracking-wide text-neutral-500 dark:text-neutral-400">
        {caption}
      </h3>
      <div
        className="grid gap-2 rounded-md border border-neutral-200 bg-neutral-50/80 p-3 text-sm dark:border-neutral-700 dark:bg-neutral-900/50"
        role="table"
        aria-label={caption}
      >
        {entries.map((entry) => (
          <div
            key={entry.key}
            className="grid grid-cols-[minmax(0,1fr)_minmax(0,2fr)] items-start gap-3 border-b border-neutral-200/80 pb-2 last:border-b-0 last:pb-0 dark:border-neutral-700/80"
            role="row"
          >
            <div className="font-medium text-neutral-800 dark:text-neutral-100" role="cell">
              <ShortcutComboKbd combo={entry.key} />
            </div>
            <div className="text-neutral-600 dark:text-neutral-300" role="cell">
              {entry.description}
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}

/** Shared shortcut tables for Help panel and tests. */
export function KeyboardShortcutsHelpContent(): React.ReactElement {
  const { navigationEntries, helpEntries } = useMemo(() => {
    const navigation: ShortcutEntry[] = [];
    const help: ShortcutEntry[] = [];

    for (const entry of SHORTCUTS) {
      if (entry.route !== undefined && entry.route !== "") {
        navigation.push(entry);
      } else {
        help.push(entry);
      }
    }

    return { navigationEntries: navigation, helpEntries: help };
  }, []);

  return (
    <div className="space-y-6 pt-2">
      <ShortcutTable entries={navigationEntries} caption="Navigation" />
      <ShortcutTable entries={ALERTS_PAGE_SHORTCUTS} caption="Alerts page" />
      <ShortcutTable entries={helpEntries} caption="Help" />
    </div>
  );
}
