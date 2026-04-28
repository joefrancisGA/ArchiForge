"use client";

import { Button } from "@/components/ui/button";

export type WizardNavButtonsProps = {
  onBack?: () => void;
  onNext?: () => void;
  onSubmit?: () => void;
  submitting?: boolean;
  /** When false, primary (Next / Submit) is disabled. Defaults to true. */
  canProceed?: boolean;
  isFirstStep?: boolean;
  /** When true, primary action is Submit instead of Next. */
  isLastInputStep?: boolean;
  /** Label for forward navigation when not submitting (default: Next). */
  nextLabel?: string;
  /** Optional local persistence (e.g. localStorage) — shown between Back and Next. */
  onSaveDraft?: () => void;
  saveDraftLabel?: string;
  /** Label for the primary submit action (default: Submit). */
  submitLabel?: string;
  /** Loading label while `submitting` (default: Submitting…). */
  submittingLabel?: string;
};

/**
 * Bottom bar: Back (outline) and Next or Submit (default).
 */
export function WizardNavButtons({
  onBack,
  onNext,
  onSubmit,
  submitting = false,
  canProceed = true,
  isFirstStep = false,
  isLastInputStep = false,
  nextLabel = "Next",
  onSaveDraft,
  saveDraftLabel = "Save draft",
  submitLabel = "Submit",
  submittingLabel = "Submitting…",
}: WizardNavButtonsProps) {
  const showBack = Boolean(onBack) && !isFirstStep;
  const primaryDisabled = !canProceed || submitting;
  const showSubmit = Boolean(onSubmit) && isLastInputStep;
  const showNext = Boolean(onNext) && !isLastInputStep;
  const showSaveDraft = Boolean(onSaveDraft);

  return (
    <div className="flex flex-wrap items-center justify-between gap-3 pt-2">
      <div className="min-h-9">
        {showBack ? (
          <Button type="button" variant="outline" onClick={onBack}>
            Back
          </Button>
        ) : null}
      </div>
      <div className="flex flex-wrap justify-end gap-2">
        {showSaveDraft ? (
          <Button type="button" variant="outline" disabled={submitting} onClick={onSaveDraft}>
            {saveDraftLabel}
          </Button>
        ) : null}
        {showSubmit ? (
          <Button type="button" variant="primary" disabled={primaryDisabled} onClick={onSubmit}>
            {submitting ? submittingLabel : submitLabel}
          </Button>
        ) : null}
        {showNext ? (
          <Button type="button" variant="primary" disabled={primaryDisabled} onClick={onNext}>
            {nextLabel}
          </Button>
        ) : null}
      </div>
    </div>
  );
}
