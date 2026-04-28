"use client";

import { CircleHelp } from "lucide-react";

import { toDocsBlobUrl } from "@/lib/contextual-help-content";
import { cn } from "@/lib/utils";

export type HelpLinkProps = {
  /** Repo-relative path such as `/docs/CORE_PILOT.md`; resolved via `toDocsBlobUrl` (same as ContextualHelp "Learn more"). */
  docPath: string;
  /** Accessible name and tooltip; keep specific (not just "Help"). */
  label: string;
  className?: string;
};

/**
 * Subtle docs icon that opens canonical GitHub `main`-branch markdown on a new tab.
 * Use sparingly beside titles; complements {@link ContextualHelp} in-app summaries.
 */
export function HelpLink({ docPath, label, className }: HelpLinkProps) {
  const href = toDocsBlobUrl(docPath);

  return (
    <a
      href={href}
      target="_blank"
      rel="noopener noreferrer"
      className={cn(
        "inline-flex h-5 w-5 shrink-0 items-center justify-center rounded-full border border-neutral-400 bg-white text-neutral-700 shadow-sm hover:border-teal-600 hover:text-teal-800 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-1 focus-visible:outline-teal-600 dark:border-neutral-500 dark:bg-neutral-900 dark:text-neutral-200 dark:hover:border-teal-500 dark:hover:text-teal-200",
        className,
      )}
      aria-label={label}
      title={label}
    >
      <CircleHelp className="h-3.5 w-3.5" aria-hidden />
    </a>
  );
}
