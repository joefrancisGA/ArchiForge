"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { useMemo, useState } from "react";

import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { KeyboardShortcutsHelpContent } from "@/components/KeyboardShortcutsHelpContent";
import { filterHelpTopics, getDocHref, type HelpTopic } from "@/lib/help-topics";

export type HelpPanelProps = {
  open: boolean;
  onOpenChange: (open: boolean) => void;
};

/**
 * Contextual help: doc topics (search + relevance) and embedded keyboard shortcut reference.
 */
export function HelpPanel({ open, onOpenChange }: HelpPanelProps) {
  const pathname = usePathname() ?? "/";
  const [query, setQuery] = useState("");

  const topics: HelpTopic[] = useMemo(() => filterHelpTopics(query, pathname), [query, pathname]);

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-h-[90vh] max-w-lg overflow-y-auto sm:max-w-2xl">
        <DialogHeader>
          <DialogTitle>Help</DialogTitle>
          <DialogDescription>
            Search topics and open documentation from your repository. Alt+letter shortcuts work outside text fields.
          </DialogDescription>
        </DialogHeader>

        <div className="space-y-4">
          <div className="space-y-1.5">
            <Label htmlFor="help-search">Search topics</Label>
            <Input
              id="help-search"
              type="search"
              placeholder="e.g. artifacts, alerts, compare…"
              value={query}
              onChange={(e) => {
                setQuery(e.target.value);
              }}
            />
          </div>

          <div>
            <h3 className="mb-2 text-xs font-semibold uppercase tracking-wide text-neutral-500 dark:text-neutral-400">
              Topics
            </h3>
            <ul className="m-0 list-none space-y-3 p-0">
              {topics.map((topic) => {
                const href = getDocHref(topic.docPath);

                return (
                  <li
                    key={topic.id}
                    className="rounded-md border border-neutral-200 bg-neutral-50/80 p-3 dark:border-neutral-700 dark:bg-neutral-900/40"
                  >
                    <div className="font-medium text-neutral-900 dark:text-neutral-100">{topic.title}</div>
                    <p className="mt-1 text-sm text-neutral-600 dark:text-neutral-300">{topic.summary}</p>
                    <p className="mt-2 font-mono text-xs text-neutral-500 dark:text-neutral-400">{topic.docPath}</p>
                    {href ? (
                      <a
                        href={href}
                        target="_blank"
                        rel="noreferrer"
                        className="mt-2 inline-block text-sm text-teal-800 underline dark:text-teal-300"
                      >
                        Open documentation
                      </a>
                    ) : (
                      <p className="mt-2 text-xs text-neutral-500 dark:text-neutral-400">
                        External documentation link is unavailable for this topic (missing path).
                      </p>
                    )}
                  </li>
                );
              })}
            </ul>
          </div>

          <div className="border-t border-neutral-200 pt-4 dark:border-neutral-700">
            <h3 className="mb-2 text-xs font-semibold uppercase tracking-wide text-neutral-500 dark:text-neutral-400">
              Keyboard shortcuts
            </h3>
            <KeyboardShortcutsHelpContent />
          </div>

          <p className="text-xs text-neutral-500 dark:text-neutral-400">
            In-app: <Link href="/getting-started" className="text-teal-800 underline dark:text-teal-300">Getting started</Link>{" "}
            (same first-manifest checklist as Home).
          </p>
        </div>
      </DialogContent>
    </Dialog>
  );
}
