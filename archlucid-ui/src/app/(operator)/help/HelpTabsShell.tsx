"use client";

import type { ReactNode } from "react";
import { useState } from "react";

import { Button } from "@/components/ui/button";
import { cn } from "@/lib/utils";

type HelpTabsShellProps = {
  readonly guide: ReactNode;
  readonly docs: ReactNode;
};

/** Default Help tab shows product guidance; documentation index stays secondary per UX review. */
export function HelpTabsShell({ guide, docs }: HelpTabsShellProps) {
  const [tab, setTab] = useState<"guide" | "docs">("guide");

  return (
    <div className="space-y-4">
      <div
        className="flex flex-wrap gap-2 rounded-lg border border-neutral-200 bg-white/80 p-1 dark:border-neutral-800 dark:bg-neutral-950/80"
        role="tablist"
        aria-label="Help sections"
      >
        <Button
          type="button"
          id="help-tab-guide"
          variant={tab === "guide" ? "primary" : "ghost"}
          size="sm"
          className={cn(tab !== "guide" && "text-neutral-700 dark:text-neutral-300")}
          aria-selected={tab === "guide"}
          role="tab"
          onClick={() => setTab("guide")}
        >
          Product guide
        </Button>
        <Button
          type="button"
          id="help-tab-docs"
          variant={tab === "docs" ? "primary" : "ghost"}
          size="sm"
          className={cn(tab !== "docs" && "text-neutral-700 dark:text-neutral-300")}
          aria-selected={tab === "docs"}
          role="tab"
          onClick={() => setTab("docs")}
        >
          Documentation
        </Button>
      </div>
      <div role="tabpanel" aria-labelledby="help-tab-guide" hidden={tab !== "guide"}>
        {guide}
      </div>
      <div role="tabpanel" aria-labelledby="help-tab-docs" hidden={tab !== "docs"}>
        <p className="mb-4 text-sm text-neutral-600 dark:text-neutral-400">
          Repository and reference topics. Use the Product guide tab for day-one tasks first.
        </p>
        {docs}
      </div>
    </div>
  );
}
