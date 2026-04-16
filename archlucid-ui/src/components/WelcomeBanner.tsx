"use client";

import { X } from "lucide-react";
import Link from "next/link";
import { useEffect, useState } from "react";

import { Button } from "@/components/ui/button";
import { cn } from "@/lib/utils";

const STORAGE_KEY = "archlucid_welcome_dismissed";

/**
 * First-visit welcome card with primary workflow CTAs; dismissal persists in localStorage.
 */
export function WelcomeBanner() {
  const [dismissed, setDismissed] = useState(true);
  const [hydrated, setHydrated] = useState(false);

  useEffect(() => {
    try {
      const raw = typeof window !== "undefined" ? window.localStorage.getItem(STORAGE_KEY) : null;
      setDismissed(raw === "1");
    } catch {
      setDismissed(false);
    }

    setHydrated(true);
  }, []);

  if (!hydrated || dismissed) {
    return null;
  }

  return (
    <div
      role="banner"
      aria-label="Welcome"
      className={cn(
        "relative mb-4 max-w-3xl rounded-lg border border-neutral-200 bg-white p-4 pl-5 shadow-sm dark:border-neutral-700 dark:bg-neutral-900",
        "border-l-4 border-l-teal-700 dark:border-l-teal-500",
      )}
    >
      <Button
        type="button"
        variant="ghost"
        size="icon"
        className="absolute right-2 top-2 h-8 w-8 text-neutral-500 hover:text-neutral-900 dark:text-neutral-400 dark:hover:text-neutral-100"
        aria-label="Dismiss welcome banner"
        onClick={() => {
          try {
            window.localStorage.setItem(STORAGE_KEY, "1");
          } catch {
            /* private mode */
          }

          setDismissed(true);
        }}
      >
        <X className="h-4 w-4" aria-hidden />
      </Button>
      <h2 className="pr-10 text-lg font-semibold text-neutral-900 dark:text-neutral-100">Welcome to ArchLucid</h2>
      <p className="mt-2 max-w-2xl text-sm leading-relaxed text-neutral-700 dark:text-neutral-300">
        Start by creating a run with the guided wizard. The pipeline will produce manifests and artifacts you can review
        on the run detail page. When you are ready, compare runs or explore governance from the sidebar.
      </p>
      <div className="mt-4 flex flex-wrap gap-3">
        <Button asChild className="bg-teal-700 text-white hover:bg-teal-800 dark:bg-teal-600 dark:hover:bg-teal-500">
          <Link href="/runs/new">Create your first run</Link>
        </Button>
        <Button asChild variant="outline">
          <Link href="/runs?projectId=default">Explore demo data</Link>
        </Button>
      </div>
    </div>
  );
}
