"use client";

import { Search } from "lucide-react";
import Link from "next/link";
import { usePathname } from "next/navigation";
import { useCallback, useMemo, useState } from "react";

import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import { KeyboardShortcutsTabContent, matchesShortcutQuery } from "@/components/KeyboardShortcutsHelpContent";
import { ALERTS_PAGE_SHORTCUTS, SHORTCUTS } from "@/lib/shortcut-registry";
import { getDocHref, helpTopicsForGuidesTab, helpTopicsForTroubleshootingTab, type HelpTopic } from "@/lib/help-topics";
import { cn } from "@/lib/utils";
import { Button } from "@/components/ui/button";

export type HelpPanelProps = {
  open: boolean;
  onOpenChange: (open: boolean) => void;
};

type HelpTabId = "guides" | "shortcuts" | "troubleshooting";

const KEY_CONCEPTS: { label: string; text: string }[] = [
  { label: "Request", text: "The architecture intent you submit." },
  { label: "Run", text: "The pipeline execution created from a request." },
  { label: "Manifest", text: "The governed architecture output produced by a run." },
  { label: "Artifacts", text: "Supporting files, findings, and review materials." },
];

function allShortcutRowsForSearch(): { key: string; description: string }[] {
  const rows: { key: string; description: string }[] = [];

  for (const s of SHORTCUTS) {
    rows.push({ key: s.key, description: s.description });
  }

  for (const s of ALERTS_PAGE_SHORTCUTS) {
    rows.push({ key: s.key, description: s.description });
  }

  return rows;
}

/**
 * Contextual help: guides first, doc topics, keyboard shortcuts in a separate tab. Light, app-aligned styling.
 */
export function HelpPanel({ open, onOpenChange }: HelpPanelProps) {
  const pathname = usePathname() ?? "/";
  const [query, setQuery] = useState("");
  const [tab, setTab] = useState<HelpTabId>("guides");

  const allShortcutRows = useMemo(() => allShortcutRowsForSearch(), []);

  const topicMatchesQuery = useCallback(
    (t: HelpTopic) => {
      const q = query.trim().toLowerCase();

      if (q.length === 0) {
        return true;
      }

      return (
        t.title.toLowerCase().includes(q) ||
        t.summary.toLowerCase().includes(q) ||
        t.keywords.some((k) => k.includes(q))
      );
    },
    [query],
  );

  const guidesBase = useMemo(() => helpTopicsForGuidesTab(), []);
  const troubleshootingBase = useMemo(() => helpTopicsForTroubleshootingTab(), []);

  const guidesFiltered = useMemo(() => {
    const q = query.trim();

    if (q.length === 0) {
      const byRoute = guidesBase.filter((topic) =>
        topic.routes.some((route) => pathname === route || pathname.startsWith(`${route}/`)),
      );

      return byRoute.length > 0 ? byRoute : guidesBase;
    }

    return guidesBase.filter(topicMatchesQuery);
  }, [guidesBase, pathname, query, topicMatchesQuery]);

  const troubleshootingFiltered = useMemo(() => {
    if (query.trim().length === 0) {
      return troubleshootingBase;
    }

    return troubleshootingBase.filter(topicMatchesQuery);
  }, [query, topicMatchesQuery, troubleshootingBase]);

  const shortcutsSearchHits = useMemo(() => {
    const q = query.trim();

    if (q.length === 0) {
      return allShortcutRows;
    }

    return allShortcutRows.filter((row) => matchesShortcutQuery(q, row.description, row.key));
  }, [allShortcutRows, query]);

  function handleOpenChange(next: boolean): void {
    if (!next) {
      setQuery("");
      setTab("guides");
    }

    onOpenChange(next);
  }

  return (
    <Dialog open={open} onOpenChange={handleOpenChange}>
      <DialogContent
        className="flex max-h-[80vh] max-w-lg flex-col gap-0 overflow-hidden border border-neutral-200 bg-white p-0 sm:max-w-[520px] dark:border-neutral-700 dark:bg-neutral-900"
      >
        <DialogHeader className="shrink-0 space-y-1 border-b border-neutral-100 px-5 pb-3 pt-5 dark:border-neutral-800">
          <DialogTitle className="text-left text-lg text-neutral-900 dark:text-neutral-100">Help</DialogTitle>
          <DialogDescription className="text-left text-sm text-neutral-600 dark:text-neutral-400">
            Search ArchLucid guidance, docs, and shortcuts.
          </DialogDescription>
        </DialogHeader>

        <div className="shrink-0 space-y-3 border-b border-neutral-100 px-5 py-3 dark:border-neutral-800">
          <div className="relative">
            <Search
              className="pointer-events-none absolute left-2.5 top-1/2 h-3.5 w-3.5 -translate-y-1/2 text-neutral-400"
              aria-hidden
            />
            <Input
              id="help-search"
              type="search"
              className="h-9 border-neutral-200 bg-white pl-8 text-sm font-normal text-neutral-900 shadow-none placeholder:text-neutral-400 focus-visible:ring-1 focus-visible:ring-teal-500 dark:border-neutral-600 dark:bg-neutral-900 dark:text-neutral-100"
              placeholder="Search help, docs, or shortcuts"
              value={query}
              onChange={(e) => {
                setQuery(e.target.value);
              }}
              aria-label="Search help, docs, or shortcuts"
            />
          </div>
          <div className="flex flex-wrap gap-1.5" role="tablist" aria-label="Help sections">
            {(
              [
                { id: "guides" as const, label: "Guides" },
                { id: "shortcuts" as const, label: "Shortcuts" },
                { id: "troubleshooting" as const, label: "Troubleshooting" },
              ] as const
            ).map(({ id, label }) => (
              <Button
                key={id}
                type="button"
                role="tab"
                size="sm"
                variant="ghost"
                className={cn(
                  "h-8 rounded-full px-3 text-xs font-medium",
                  tab === id
                    ? "bg-teal-100 text-teal-900 dark:bg-teal-900/50 dark:text-teal-100"
                    : "text-neutral-600 hover:text-neutral-900 dark:text-neutral-400",
                )}
                aria-selected={tab === id}
                onClick={() => {
                  setTab(id);
                }}
              >
                {label}
              </Button>
            ))}
          </div>
        </div>

        <div className="min-h-0 flex-1 overflow-y-auto px-5 py-4">
          {tab === "guides" ? (
            <div className="space-y-4">
              <div className="rounded-lg border border-neutral-200/90 bg-teal-50/40 p-3 dark:border-neutral-700 dark:bg-teal-950/20">
                <h3 className="m-0 text-xs font-semibold uppercase tracking-wide text-teal-900 dark:text-teal-200">
                  Key concepts
                </h3>
                <ul className="m-0 mt-2 list-none space-y-1.5 p-0 text-sm text-neutral-700 dark:text-neutral-300">
                  {KEY_CONCEPTS.map((row) => (
                    <li key={row.label}>
                      <span className="font-semibold text-neutral-800 dark:text-neutral-200">{row.label}:</span> {row.text}
                    </li>
                  ))}
                </ul>
              </div>
              <h3 className="m-0 text-xs font-semibold uppercase tracking-wide text-neutral-500 dark:text-neutral-400">
                Topics
              </h3>
              {guidesFiltered.length === 0 ? (
                <p className="m-0 text-sm text-neutral-500 dark:text-neutral-400">No topics match your search.</p>
              ) : (
                <ul className="m-0 list-none space-y-2 p-0">
                  {guidesFiltered.map((topic) => {
                    const href = getDocHref(topic.docPath);

                    return (
                      <li
                        key={topic.id}
                        className="rounded-md border border-neutral-200/90 bg-white p-3 shadow-sm dark:border-neutral-600 dark:bg-neutral-900/50"
                      >
                        <div className="font-medium text-neutral-900 dark:text-neutral-100">{topic.title}</div>
                        <p className="mt-1 text-sm text-neutral-600 dark:text-neutral-300">{topic.summary}</p>
                        {href ? (
                          <a
                            href={href}
                            target="_blank"
                            rel="noreferrer"
                            title={topic.docPath}
                            className="mt-2 inline-block text-sm font-medium text-teal-800 underline dark:text-teal-300"
                          >
                            Open documentation
                          </a>
                        ) : null}
                      </li>
                    );
                  })}
                </ul>
              )}
            </div>
          ) : null}

          {tab === "troubleshooting" ? (
            <div className="space-y-3">
              {troubleshootingFiltered.length === 0 ? (
                <p className="m-0 text-sm text-neutral-500 dark:text-neutral-400">No topics match your search.</p>
              ) : (
                <ul className="m-0 list-none space-y-2 p-0">
                  {troubleshootingFiltered.map((topic) => {
                    const href = getDocHref(topic.docPath);

                    return (
                      <li
                        key={topic.id}
                        className="rounded-md border border-neutral-200/90 bg-white p-3 shadow-sm dark:border-neutral-600 dark:bg-neutral-900/50"
                      >
                        <div className="font-medium text-neutral-900 dark:text-neutral-100">{topic.title}</div>
                        <p className="mt-1 text-sm text-neutral-600 dark:text-neutral-300">{topic.summary}</p>
                        {href ? (
                          <a
                            href={href}
                            target="_blank"
                            rel="noreferrer"
                            title={topic.docPath}
                            className="mt-2 inline-block text-sm font-medium text-teal-800 underline dark:text-teal-300"
                          >
                            Open documentation
                          </a>
                        ) : null}
                      </li>
                    );
                  })}
                </ul>
              )}
            </div>
          ) : null}

          {tab === "shortcuts" ? (
            <div className="space-y-3">
              {query.trim().length > 0 && shortcutsSearchHits.length === 0 ? (
                <p className="m-0 text-sm text-neutral-500 dark:text-neutral-400">No shortcuts match your search.</p>
              ) : query.trim().length > 0 ? (
                <div>
                  <h3 className="mb-2 text-xs font-semibold uppercase tracking-wide text-neutral-500 dark:text-neutral-400">
                    Search results
                  </h3>
                  <div className="space-y-2 rounded-md border border-neutral-200/80 p-2 dark:border-neutral-600">
                    {shortcutsSearchHits.map((row) => (
                      <div key={row.key} className="text-sm text-neutral-700 dark:text-neutral-300">
                        <kbd className="mr-2 rounded border border-neutral-200 bg-neutral-50 px-1.5 py-0.5 font-mono text-xs dark:border-neutral-600 dark:bg-neutral-800">
                          {row.key}
                        </kbd>
                        {row.description}
                      </div>
                    ))}
                  </div>
                </div>
              ) : (
                <KeyboardShortcutsTabContent />
              )}
            </div>
          ) : null}
        </div>

        <div className="shrink-0 border-t border-neutral-100 px-5 py-3 dark:border-neutral-800">
          <p className="m-0 text-xs text-neutral-500 dark:text-neutral-400">
            In-app:{" "}
            <Link href="/getting-started" className="font-medium text-teal-800 underline dark:text-teal-300">
              Getting started
            </Link>{" "}
            (first-manifest checklist on Home)
          </p>
        </div>
      </DialogContent>
    </Dialog>
  );
}
