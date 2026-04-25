"use client";

import { useEffect, useState, type ReactNode } from "react";

import { Tooltip, TooltipContent, TooltipTrigger } from "@/components/ui/tooltip";
import { type GlossaryTermKey, GLOSSARY_TERMS } from "@/lib/glossary-terms";
import { cn } from "@/lib/utils";

export type GlossaryTooltipProps = {
  termKey: GlossaryTermKey;
  children: ReactNode;
  /** If false, first-visit pulse animation to the underline is skipped. */
  pulseOnFirstSession?: boolean;
};

const SEEN_KEY_PREFIX = "glossary-seen-";

/**
 * Dotted inline term with a short definition, optional “Learn more” to `docs/library/GLOSSARY.md`, and optional first-visit pulse.
 * Use within an app region wrapped by `TooltipProvider` (see `AppShellClient`).
 */
export function GlossaryTooltip({ termKey, children, pulseOnFirstSession = true }: GlossaryTooltipProps) {
  const entry = GLOSSARY_TERMS[termKey];
  const [firstPulse, setFirstPulse] = useState(false);

  useEffect(() => {
    if (!pulseOnFirstSession || typeof window === "undefined") {
      return;
    }

    const storageKey = SEEN_KEY_PREFIX + termKey;

    if (sessionStorage.getItem(storageKey) === "1") {
      return;
    }

    sessionStorage.setItem(storageKey, "1");
    setFirstPulse(true);
    const timer = window.setTimeout(() => setFirstPulse(false), 1600);

    return () => {
      window.clearTimeout(timer);
    };
  }, [termKey, pulseOnFirstSession]);

  return (
    <Tooltip>
      <TooltipTrigger asChild>
        <span
          className={cn(
            "cursor-help border-b border-dotted border-neutral-500 text-inherit underline-offset-2 dark:border-neutral-400",
            firstPulse && "motion-safe:animate-pulse",
          )}
        >
          {children}
        </span>
      </TooltipTrigger>
      <TooltipContent side="top" className="max-w-sm text-sm">
        <p className="m-0 text-sm font-semibold">{entry.term}</p>
        <p className="mb-0 mt-1.5 text-xs leading-snug">{entry.definition}</p>
        {entry.docLink !== undefined ? (
          <p className="mb-0 mt-2 text-xs">
            <a
              className="font-medium underline decoration-neutral-300 underline-offset-2 dark:decoration-neutral-600"
              href={entry.docLink}
              target="_blank"
              rel="noreferrer"
            >
              Learn more in glossary →
            </a>
          </p>
        ) : null}
      </TooltipContent>
    </Tooltip>
  );
}
