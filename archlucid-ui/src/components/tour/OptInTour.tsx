"use client";

import { useCallback, useEffect, useState } from "react";

import { Button } from "@/components/ui/button";

/**
 * In-product opt-in tour (PENDING_QUESTIONS.md item 38 — owner Q8 + Q9; copy approved 2026-04-24).
 *
 * Hard-wired contract:
 * - Five steps. Approved copy (Improvement 5, option B — single batch PR).
 * - Tour NEVER auto-launches (owner Q9). The component is purely controlled — render
 *   it conditionally on `isOpen` and trigger via the parent's button.
 * - Closing the tour persists a dismissal flag in `localStorage`. The flag is a
 *   defensive marker for any future auto-launch path — the "Show me around" button
 *   itself ignores it, so re-opening the tour by hand always works.
 */
export const TOUR_DISMISSED_LOCAL_STORAGE_KEY = "archlucid.optInTour.dismissed.v1";

export interface OptInTourStep {
  readonly title: string;
  readonly body: string;
}

/**
 * Five-step opt-in tour script (owner-approved 2026-04-24).
 */
export const DRAFT_TOUR_STEPS: readonly OptInTourStep[] = [
  {
    title: "1. Operator home",
    body:
      "Your starting point. The Core Pilot checklist at the top walks you through your first run — follow it in order. " +
      "The analysis and governance sections below are optional until your first run is committed.",
  },
  {
    title: "2. Start a run",
    body:
      "Click New run (or press Alt+N) to open the wizard. It guides you through system identity, requirements, and " +
      "constraints, then kicks off the analysis pipeline. You will see live progress on step 7.",
  },
  {
    title: "3. Review and commit",
    body:
      "When the pipeline finishes, open your run from the Runs list. Review the findings and evidence, then click " +
      "Commit to produce the versioned manifest — the architecture package you can export and share.",
  },
  {
    title: "4. Governance and alerts",
    body:
      "After your first commit, dashboards and alerts can highlight policy gaps and approval queues. These are " +
      "available when you are ready — they are not required for a successful first pilot.",
  },
  {
    title: "5. Get help",
    body:
      "If something is not working, go to Admin → Support to download a redacted diagnostics bundle for support " +
      "tickets. Most pages also include a link to the relevant documentation.",
  },
];

export interface OptInTourProps {
  readonly isOpen: boolean;
  readonly onClose: () => void;
}

export function OptInTour({ isOpen, onClose }: OptInTourProps) {
  const [stepIndex, setStepIndex] = useState(0);

  useEffect(() => {
    if (!isOpen) return;
    setStepIndex(0);
  }, [isOpen]);

  const handleClose = useCallback(() => {
    persistDismissal();
    onClose();
  }, [onClose]);

  const handleNext = useCallback(() => {
    setStepIndex((current) => Math.min(current + 1, DRAFT_TOUR_STEPS.length - 1));
  }, []);

  const handlePrev = useCallback(() => {
    setStepIndex((current) => Math.max(current - 1, 0));
  }, []);

  if (!isOpen) return null;

  const isLast = stepIndex === DRAFT_TOUR_STEPS.length - 1;
  const isFirst = stepIndex === 0;

  return (
    <div
      className="fixed inset-0 z-50 flex items-center justify-center bg-black/60 p-4"
      role="dialog"
      aria-modal="true"
      aria-label="ArchLucid opt-in tour"
      data-testid="opt-in-tour-dialog"
    >
      <div className="w-full max-w-md space-y-4 rounded-lg border border-neutral-200 bg-white p-6 shadow-2xl dark:border-neutral-800 dark:bg-neutral-950">
        <div className="flex items-start justify-between gap-3">
          <h2 className="text-lg font-semibold text-neutral-900 dark:text-neutral-100">Show me around</h2>
          <button
            type="button"
            className="rounded p-1 text-sm text-neutral-500 hover:bg-neutral-100 dark:hover:bg-neutral-800"
            onClick={handleClose}
            data-testid="opt-in-tour-close"
            aria-label="Close tour"
          >
            ✕
          </button>
        </div>

        {DRAFT_TOUR_STEPS.map((step, idx) =>
          idx === stepIndex ? (
            <div key={step.title} data-testid={`opt-in-tour-step-${idx}`} className="space-y-3">
              <h3 className="text-sm font-semibold text-neutral-900 dark:text-neutral-100">{step.title}</h3>
              <p className="text-sm leading-relaxed text-neutral-800 dark:text-neutral-100">{step.body}</p>
            </div>
          ) : null,
        )}

        <div className="flex items-center justify-between pt-2">
          <span className="text-xs text-neutral-500 dark:text-neutral-400">
            Step {stepIndex + 1} of {DRAFT_TOUR_STEPS.length}
          </span>
          <div className="flex gap-2">
            <Button
              type="button"
              variant="outline"
              disabled={isFirst}
              onClick={handlePrev}
              data-testid="opt-in-tour-prev"
            >
              Back
            </Button>
            {isLast ? (
              <Button type="button" onClick={handleClose} data-testid="opt-in-tour-finish">
                Finish
              </Button>
            ) : (
              <Button type="button" onClick={handleNext} data-testid="opt-in-tour-next">
                Next
              </Button>
            )}
          </div>
        </div>
      </div>
    </div>
  );
}

function persistDismissal(): void {
  if (typeof window === "undefined") return;

  try {
    window.localStorage.setItem(TOUR_DISMISSED_LOCAL_STORAGE_KEY, new Date().toISOString());
  } catch {
    // localStorage may be disabled (private mode, embedded contexts) — silently ignore;
    // the worst case is we'd ask "Show me around" again on next visit, which is harmless.
  }
}
