"use client";

import { zodResolver } from "@hookform/resolvers/zod";
import Link from "next/link";
import { useCallback, useRef, useState } from "react";
import type { FieldErrors } from "react-hook-form";
import { FormProvider, useForm } from "react-hook-form";

import { TooltipProvider } from "@/components/ui/tooltip";
import { WizardNavButtons } from "@/components/wizard/WizardNavButtons";
import { WizardStepper } from "@/components/wizard/WizardStepper";
import { WizardStepAdvanced } from "@/components/wizard/steps/WizardStepAdvanced";
import { WizardStepConstraints } from "@/components/wizard/steps/WizardStepConstraints";
import { WizardStepDescription } from "@/components/wizard/steps/WizardStepDescription";
import { WizardStepIdentity } from "@/components/wizard/steps/WizardStepIdentity";
import { WizardStepPreset } from "@/components/wizard/steps/WizardStepPreset";
import { WizardStepReview } from "@/components/wizard/steps/WizardStepReview";
import { WizardStepTrack } from "@/components/wizard/steps/WizardStepTrack";
import { useRunSummaryStream } from "@/hooks/useRunSummaryStream";
import { createArchitectureRun } from "@/lib/api";
import { showError, showSuccess } from "@/lib/toast";
import { wizardValuesToCreateRunPayload } from "@/lib/wizard-payload";
import { buildDefaultWizardValues, wizardFormSchema, type WizardFormValues } from "@/lib/wizard-schema";
const WIZARD_STEP_DEFINITIONS = [
  { label: "Starting point", description: "Preset or scratch" },
  { label: "Identity", description: "System & environment" },
  { label: "Description", description: "Goals & requirements" },
  { label: "Constraints", description: "Limits & capabilities" },
  { label: "Advanced", description: "Optional context" },
  { label: "Review", description: "Confirm & create" },
  { label: "Pipeline", description: "Track progress" },
] as const;

const STEP_INDEX_MAX = WIZARD_STEP_DEFINITIONS.length - 1;

/** Fields validated before leaving each step (0 = preset, no validation). */
const STEP_TRIGGER_FIELDS: Record<number, (keyof WizardFormValues)[] | null> = {
  0: null,
  1: ["systemName", "environment", "cloudProvider", "priorManifestVersion"],
  2: ["description", "inlineRequirements"],
  3: ["constraints", "requiredCapabilities", "assumptions"],
  4: [
    "policyReferences",
    "topologyHints",
    "securityBaselineHints",
    "documents",
    "infrastructureDeclarations",
  ],
};

function stepHasBlockingErrors(stepIndex: number, errors: FieldErrors<WizardFormValues>): boolean {
  if (stepIndex === 5) {
    return Object.keys(errors).length > 0;
  }

  const fields = STEP_TRIGGER_FIELDS[stepIndex];

  if (!fields || fields.length === 0) {
    return false;
  }

  return fields.some((field) => errors[field] != null);
}

/** Seven-step client wizard: react-hook-form + zod, create run, poll summary with live region + toast. */
export function NewRunWizardClient() {
  const [stepIndex, setStepIndex] = useState(0);
  const [submitting, setSubmitting] = useState(false);
  const [runId, setRunId] = useState<string | null>(null);
  const liveRef = useRef<HTMLDivElement>(null);
  const { summary: pollSummary } = useRunSummaryStream(runId, {
    enabled: runId !== null && stepIndex === 6,
  });

  const form = useForm<WizardFormValues>({
    resolver: zodResolver(wizardFormSchema),
    defaultValues: buildDefaultWizardValues(),
    mode: "onBlur",
  });

  const { trigger, getValues, formState } = form;

  const canProceed = stepIndex === 0 || !stepHasBlockingErrors(stepIndex, formState.errors);

  const showToast = useCallback((kind: "ok" | "err", message: string) => {
    if (kind === "ok") {
      showSuccess(message);
    } else {
      showError("Wizard", message);
    }
  }, []);

  const completedSteps: number[] =
    stepIndex === 0 ? [] : Array.from({ length: stepIndex }, (_, index) => index);

  const liveMessage =
    runId === null
      ? "No run yet."
      : pollSummary
        ? `Run ${runId} polled: context ${pollSummary.hasContextSnapshot ? "ready" : "pending"}, graph ${pollSummary.hasGraphSnapshot ? "ready" : "pending"}, findings ${pollSummary.hasFindingsSnapshot ? "ready" : "pending"}, golden manifest ${pollSummary.hasGoldenManifest ? "ready" : "pending"}.`
        : `Run ${runId} created; loading summary.`;

  const goBack = () => {
    setStepIndex((current) => Math.max(0, current - 1));
  };

  const goNext = async () => {
    if (stepIndex === 0) {
      setStepIndex(1);

      return;
    }

    const fields = STEP_TRIGGER_FIELDS[stepIndex];

    if (fields !== null && fields.length > 0) {
      const ok = await trigger(fields, { shouldFocus: true });

      if (!ok) {
        showToast("err", "Fix the highlighted fields before continuing.");

        return;
      }
    }

    setStepIndex((current) => Math.min(STEP_INDEX_MAX, current + 1));
  };

  const submitRun = async () => {
    const ok = await trigger(undefined, { shouldFocus: true });

    if (!ok) {
      showToast("err", "Fix validation errors before creating the run.");

      return;
    }

    setSubmitting(true);

    try {
      const body = wizardValuesToCreateRunPayload(getValues());
      const res = await createArchitectureRun(body);
      const id = res.run?.runId ?? null;

      if (!id) {
        showToast("err", "API returned no run id.");

        return;
      }

      setRunId(id);
      setStepIndex(6);
      showToast("ok", `Run ${id} created — tracking pipeline below.`);
    } catch (error: unknown) {
      const message =
        error && typeof error === "object" && "message" in error
          ? String((error as { message?: string }).message)
          : "Request failed.";
      showToast("err", message);
    } finally {
      setSubmitting(false);
    }
  };

  const showNav = stepIndex < 6;
  const isFirstStep = stepIndex === 0;
  const isReviewStep = stepIndex === 5;

  return (
    <TooltipProvider delayDuration={200}>
      <FormProvider {...form}>
        <div className="mx-auto w-full max-w-4xl space-y-6">
          <p
            className="m-0 text-sm text-neutral-600 dark:text-neutral-400"
            data-testid="new-run-wizard-step-line"
          >
            Step {stepIndex + 1} of {WIZARD_STEP_DEFINITIONS.length} — guided create for{" "}
            <code className="rounded bg-neutral-100 px-1 text-xs dark:bg-neutral-800">/v1/architecture/request</code>
            .{" "}
            <Link href="/runs" className="text-teal-700 underline">
              Runs list
            </Link>
          </p>

          <WizardStepper
            steps={[...WIZARD_STEP_DEFINITIONS]}
            currentStep={stepIndex}
            completedSteps={completedSteps}
          />

          {stepIndex === 0 ? <WizardStepPreset /> : null}
          {stepIndex === 1 ? <WizardStepIdentity /> : null}
          {stepIndex === 2 ? <WizardStepDescription /> : null}
          {stepIndex === 3 ? <WizardStepConstraints /> : null}
          {stepIndex === 4 ? <WizardStepAdvanced /> : null}
          {stepIndex === 5 ? <WizardStepReview /> : null}
          {stepIndex === 6 && runId ? <WizardStepTrack runId={runId} pollSummary={pollSummary} /> : null}

          {showNav ? (
            <WizardNavButtons
              onBack={goBack}
              onNext={isReviewStep ? undefined : goNext}
              onSubmit={isReviewStep ? submitRun : undefined}
              submitting={submitting}
              canProceed={canProceed}
              isFirstStep={isFirstStep}
              isLastInputStep={isReviewStep}
              submitLabel="Create run"
              submittingLabel="Creating…"
            />
          ) : null}

          {stepIndex === 6 && !runId ? (
            <p className="text-sm text-red-600">Run id missing; cannot track pipeline.</p>
          ) : null}

          <div ref={liveRef} aria-live="polite" aria-atomic="true" className="sr-only">
            {liveMessage}
          </div>
        </div>
      </FormProvider>
    </TooltipProvider>
  );
}
