"use client";

import { useCallback, useEffect, useState } from "react";

import { Button } from "@/components/ui/button";

import { TourStepPendingApproval } from "./TourStepPendingApproval";

/**
 * In-product opt-in tour (PENDING_QUESTIONS.md item 38, owner Q8 + Q9 — 2026-04-23).
 *
 * Hard-wired contract:
 * - Five steps. Copy is the assistant's first cut, wrapped in `TourStepPendingApproval`
 *   so end-tenants see the "pending owner approval" marker until owner sign-off.
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
 * Five-step opt-in tour script. Wording is intentionally generic and DRAFT — every
 * step is wrapped in `TourStepPendingApproval` until the owner approves real copy
 * (owner Q8). When the owner approves, replace the wrapper for that step with a
 * plain fragment.
 */
export const DRAFT_TOUR_STEPS: readonly OptInTourStep[] = [
  {
    title: "1. Operator home",
    body:
      "This is your launchpad — Core Pilot quick links sit at the top, Advanced Analysis and Enterprise Controls below. " +
      "Skip the lower sections until your first pilot run is done.",
  },
  {
    title: "2. Start a run",
    body:
      "Use New run (Alt+N) to walk through the seven-step wizard. The wizard collects context, kicks off the pipeline, " +
      "and tracks live progress on the run-detail page.",
  },
  {
    title: "3. Inspect a run",
    body:
      "Open Runs → run detail to view the manifest, evidence chain, and findings. Use Commit run once the pipeline " +
      "completes to produce the golden manifest and downloadable artifacts.",
  },
  {
    title: "4. Governance + alerts",
    body:
      "Once you have a committed run, governance dashboards and alerts highlight policy drift and approval needs. " +
      "Read-only roles can view; operators can act on findings (API-enforced access levels).",
  },
  {
    title: "5. Get help",
    body:
      "Stuck? /admin/support assembles a redacted bundle you can attach to a support ticket. Most pages also link the " +
      "relevant doc — look for the small reference pointers under each section.",
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
              <TourStepPendingApproval>{step.body}</TourStepPendingApproval>
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

/**
 * For tests: render every step's body inline (used by the "all five steps render the
 * pending-approval marker" Vitest assertion). Production code should use `OptInTour`
 * instead — this helper exists only so the spec doesn't need to drive the Next button
 * five times to verify every step's marker.
 */
export function TourStepListForTesting() {
  return (
    <div data-testid="opt-in-tour-all-steps">
      {DRAFT_TOUR_STEPS.map((step) => (
        <div key={step.title} data-testid={`opt-in-tour-step-${step.title}`}>
          <h3>{step.title}</h3>
          <TourStepPendingApproval>{step.body}</TourStepPendingApproval>
        </div>
      ))}
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
