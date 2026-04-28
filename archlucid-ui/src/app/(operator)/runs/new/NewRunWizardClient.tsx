"use client";

import { zodResolver } from "@hookform/resolvers/zod";
import { useSearchParams } from "next/navigation";
import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import type { FieldPath } from "react-hook-form";
import { FormProvider, useForm, useWatch } from "react-hook-form";

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
import { createArchitectureRun, listRunsByProjectPaged } from "@/lib/api";
import { recordFirstTenantFunnelEvent } from "@/lib/first-tenant-funnel-telemetry";
import { showError, showSuccess } from "@/lib/toast";
import { wizardValuesToCreateRunPayload } from "@/lib/wizard-payload";
import { WIZARD_STEP_FIELD_GROUPS } from "@/lib/wizard-step-fields";
import { validateWizardStep } from "@/lib/wizard-step-validate";
import {
  OPERATOR_HOME_EXAMPLE_DESCRIPTION,
  OPERATOR_HOME_EXAMPLE_QUERY_VALUE,
  OPERATOR_HOME_EXAMPLE_SYSTEM_NAME,
} from "@/lib/operator-home-example-request";
import { buildDefaultWizardValues, wizardFormSchema, type WizardFormValues } from "@/lib/wizard-schema";

import { QuickStartWizard } from "./QuickStartWizard";

const WIZARD_MODE_STORAGE_KEY = "archlucid_new_run_wizard_mode_v1";
const WIZARD_STEP_DEFINITIONS = [
  { label: "Choose starting point", description: "Template, import, or blank" },
  { label: "Identity", description: "System & environment" },
  { label: "Description", description: "Goals & requirements" },
  { label: "Constraints", description: "Limits & capabilities" },
  { label: "Advanced", description: "Optional context" },
  { label: "Review", description: "Confirm & create" },
  { label: "Pipeline", description: "Track progress" },
] as const;

/** High-level phases shown in the stepper (maps the seven internal steps to three sponsor-friendly phases). */
const MACRO_WIZARD_STEP_DEFINITIONS = [
  { label: "Request brief", description: "Identity, goals, starting point" },
  { label: "Constraints", description: "Requirements, policies, context" },
  { label: "Review and run", description: "Confirm, create, track" },
] as const;

const WIZARD_DRAFT_STORAGE_KEY = "archlucid_new_run_wizard_draft_v1";

const STEP_INDEX_MAX = WIZARD_STEP_DEFINITIONS.length - 1;

function macroWizardStepIndex(stepIndex: number): number {
  if (stepIndex <= 2) {
    return 0;
  }

  if (stepIndex <= 4) {
    return 1;
  }

  return 2;
}

function macroCompletedSteps(stepIndex: number): number[] {
  const macro = macroWizardStepIndex(stepIndex);

  return Array.from({ length: macro }, (_, index) => index);
}

const SAMPLE_RUN_GUID_RE =
  /^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$|^[0-9a-fA-F]{32}$/;

function tryParseSampleRunQuery(raw: string | null): string | null {
  if (raw === null) {
    return null;
  }

  const trimmed = raw.trim();

  if (trimmed.length === 0 || !SAMPLE_RUN_GUID_RE.test(trimmed)) {
    return null;
  }

  if (trimmed.includes("-")) {
    return trimmed;
  }

  const n = trimmed.toLowerCase();

  return `${n.slice(0, 8)}-${n.slice(8, 12)}-${n.slice(12, 16)}-${n.slice(16, 20)}-${n.slice(20, 32)}`;
}

/** Seven-step client wizard: react-hook-form + zod, create run, poll summary with live region + toast. */
export function NewRunWizardClient() {
  const searchParams = useSearchParams();
  const featuredSampleRunId = useMemo(() => {
    const raw = searchParams?.get("sampleRunId") ?? null;

    return tryParseSampleRunQuery(raw);
  }, [searchParams]);
  const [stepIndex, setStepIndex] = useState(0);
  const [submitting, setSubmitting] = useState(false);
  const [runId, setRunId] = useState<string | null>(null);
  const [wizardMode, setWizardMode] = useState<"quick" | "full">(() => {
    if (typeof window === "undefined") {
      return "quick";
    }

    try {
      const stored = window.localStorage.getItem(WIZARD_MODE_STORAGE_KEY);
      if (stored === "quick" || stored === "full") {
        return stored;
      }
    } catch {
      /* ignore */
    }

    return "quick";
  });
  const [wizardModeReady] = useState(true);
  const liveRef = useRef<HTMLDivElement>(null);
  const wizardReadyRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    wizardReadyRef.current?.setAttribute("data-wizard-ready", "true");
  }, []);

  const { summary: pollSummary } = useRunSummaryStream(runId, {
    enabled:
      runId !== null && (wizardMode === "quick" ? true : stepIndex === 6),
  });

  const form = useForm<WizardFormValues>({
    resolver: zodResolver(wizardFormSchema),
    defaultValues: buildDefaultWizardValues(),
    mode: "onBlur",
  });

  const { trigger, getValues, setError, clearErrors, control, setValue } = form;

  const operatorHomeExampleKey = useMemo(() => {
    const raw = searchParams?.get("example")?.trim().toLowerCase() ?? "";

    if (raw === OPERATOR_HOME_EXAMPLE_QUERY_VALUE) {
      return OPERATOR_HOME_EXAMPLE_QUERY_VALUE;
    }

    return null;
  }, [searchParams]);

  useEffect(() => {
    let cancelled = false;

    void (async () => {
      try {
        const stored =
          typeof window !== "undefined" ? window.localStorage.getItem(WIZARD_MODE_STORAGE_KEY) : null;

        if (stored === "quick" || stored === "full") {
          return;
        }

        const page = await listRunsByProjectPaged("default", 1, 50);
        const anyCommitted = page.items.some((r) => r.hasGoldenManifest === true);

        if (!cancelled) {
          setWizardMode(anyCommitted ? "full" : "quick");
        }
      } catch {
        if (!cancelled) {
          setWizardMode("quick");
        }
      }
    })();

    return () => {
      cancelled = true;
    };
  }, []);

  useEffect(() => {
    if (operatorHomeExampleKey !== OPERATOR_HOME_EXAMPLE_QUERY_VALUE) {
      return;
    }

    if (stepIndex >= 1) {
      setValue("systemName", OPERATOR_HOME_EXAMPLE_SYSTEM_NAME, { shouldValidate: true, shouldDirty: true });
    }

    if (stepIndex >= 2) {
      setValue("description", OPERATOR_HOME_EXAMPLE_DESCRIPTION, { shouldValidate: true, shouldDirty: true });
    }
  }, [operatorHomeExampleKey, setValue, stepIndex]);

  const watchedValues = useWatch({ control });

  const stepHasValidationErrors = useMemo(() => {
    if (stepIndex < 1 || stepIndex > 4) {
      return false;
    }

    return validateWizardStep(stepIndex, watchedValues as WizardFormValues).length > 0;
  }, [stepIndex, watchedValues]);

  const canProceed = !submitting && (stepIndex === 0 || stepIndex === 5 || !stepHasValidationErrors);

  const showToast = useCallback((kind: "ok" | "err", message: string) => {
    if (kind === "ok") {
      showSuccess(message);
    } else {
      showError("Wizard", message);
    }
  }, []);

  const saveWizardDraft = useCallback(() => {
    try {
      const payload = JSON.stringify({ v: 1, stepIndex, values: getValues() });
      window.localStorage.setItem(WIZARD_DRAFT_STORAGE_KEY, payload);
      showSuccess("Draft saved in this browser.");
    } catch {
      showError("Wizard", "Could not save draft.");
    }
  }, [getValues, stepIndex]);

  const completedMacroSteps: number[] = macroCompletedSteps(stepIndex);
  const macroStep = macroWizardStepIndex(stepIndex);

  const liveMessage =
    runId === null
      ? "No run yet."
      : pollSummary
        ? `Run ${runId} polled: context ${pollSummary.hasContextSnapshot ? "ready" : "pending"}, graph ${pollSummary.hasGraphSnapshot ? "ready" : "pending"}, findings ${pollSummary.hasFindingsSnapshot ? "ready" : "pending"}, reviewed manifest ${pollSummary.hasGoldenManifest ? "ready" : "pending"}.`
        : `Run ${runId} created; loading summary.`;

  const persistWizardMode = useCallback((mode: "quick" | "full") => {
    setWizardMode(mode);

    try {
      window.localStorage.setItem(WIZARD_MODE_STORAGE_KEY, mode);
    } catch {
      /* ignore */
    }
  }, []);

  const goBack = () => {
    setStepIndex((current) => Math.max(0, current - 1));
  };

  const goNext = () => {
    if (stepIndex === 0) {
      setStepIndex(1);
      return;
    }

    const fieldGroup = WIZARD_STEP_FIELD_GROUPS[stepIndex];
    if (fieldGroup != null) {
      for (const f of fieldGroup) {
        clearErrors(f);
      }
    }

    const list = validateWizardStep(stepIndex, getValues());
    if (list.length > 0) {
      for (const e of list) {
        setError(e.field as FieldPath<WizardFormValues>, { type: "validate", message: e.message });
      }

      showToast("err", "Fix the highlighted fields before continuing.");
      return;
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
      recordFirstTenantFunnelEvent("first_run_started");
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
  const showQuickTrack = wizardMode === "quick" && runId !== null;
  const showFullWizardShell = wizardMode === "full" && !showQuickTrack;

  return (
    <FormProvider {...form}>
      <div ref={wizardReadyRef} className="mx-auto w-full max-w-4xl space-y-4 pb-24">
          {!wizardModeReady ? (
            <p className="text-sm text-neutral-600 dark:text-neutral-400">Loading wizard…</p>
          ) : null}
          {wizardModeReady ? (
            <div
              className="flex flex-wrap items-center gap-2 rounded-lg border border-neutral-200/80 bg-neutral-50/80 p-3 dark:border-neutral-800 dark:bg-neutral-900/40"
              role="group"
              aria-label="Wizard mode"
              data-testid="new-run-wizard-mode-toggle"
            >
              <span className="text-sm font-medium text-neutral-800 dark:text-neutral-200">Mode</span>
              <button
                type="button"
                className={
                  wizardMode === "quick"
                    ? "rounded-md bg-teal-600 px-3 py-1.5 text-sm font-medium text-white"
                    : "rounded-md px-3 py-1.5 text-sm text-neutral-700 ring-1 ring-neutral-300 hover:bg-neutral-100 dark:text-neutral-200 dark:ring-neutral-700 dark:hover:bg-neutral-800"
                }
                aria-pressed={wizardMode === "quick"}
                onClick={() => persistWizardMode("quick")}
              >
                Quick Start (3 steps)
              </button>
              <button
                type="button"
                className={
                  wizardMode === "full"
                    ? "rounded-md bg-teal-600 px-3 py-1.5 text-sm font-medium text-white"
                    : "rounded-md px-3 py-1.5 text-sm text-neutral-700 ring-1 ring-neutral-300 hover:bg-neutral-100 dark:text-neutral-200 dark:ring-neutral-700 dark:hover:bg-neutral-800"
                }
                aria-pressed={wizardMode === "full"}
                onClick={() => persistWizardMode("full")}
              >
                Full Wizard (7 steps)
              </button>
            </div>
          ) : null}

          {wizardModeReady && wizardMode === "quick" && showQuickTrack && runId ? (
            <WizardStepTrack runId={runId} pollSummary={pollSummary} />
          ) : null}

          {wizardModeReady && wizardMode === "quick" && !showQuickTrack ? (
            <QuickStartWizard
              key={wizardMode}
              onRunCreated={(id) => {
                setRunId(id);
              }}
            />
          ) : null}

          {wizardModeReady && showFullWizardShell ? (
            <>
          <div className="space-y-1" data-testid="new-run-wizard-progress">
            <p
              className="m-0 font-medium text-neutral-900 dark:text-neutral-100"
              data-testid="new-run-wizard-stage-line"
            >
              Stage {macroStep + 1} of {MACRO_WIZARD_STEP_DEFINITIONS.length} —{" "}
              {MACRO_WIZARD_STEP_DEFINITIONS[macroStep].label}
            </p>
            <p
              className="m-0 text-sm text-neutral-500 dark:text-neutral-400"
              data-testid="new-run-wizard-step-line"
            >
              Step {stepIndex + 1}: {WIZARD_STEP_DEFINITIONS[stepIndex].label}
            </p>
          </div>

          <WizardStepper
            steps={[...MACRO_WIZARD_STEP_DEFINITIONS]}
            currentStep={macroStep}
            completedSteps={completedMacroSteps}
          />

          {stepIndex === 0 ? (
            <WizardStepPreset
              featuredSampleRunId={featuredSampleRunId}
              onStartingPointCommitted={() => setStepIndex(1)}
              onWizardNotice={(kind, message) => showToast(kind === "ok" ? "ok" : "err", message)}
            />
          ) : null}
          {stepIndex === 1 ? <WizardStepIdentity /> : null}
          {stepIndex === 2 ? <WizardStepDescription /> : null}
          {stepIndex === 3 ? <WizardStepConstraints /> : null}
          {stepIndex === 4 ? <WizardStepAdvanced /> : null}
          {stepIndex === 5 ? <WizardStepReview /> : null}
          {stepIndex === 6 && runId ? <WizardStepTrack runId={runId} pollSummary={pollSummary} /> : null}

          {showNav ? (
            <div
              className="sticky bottom-0 z-10 -mx-4 mt-8 border-t border-neutral-200/50 bg-neutral-50/95 px-4 py-3 shadow-[0_-4px_16px_-4px_rgba(0,0,0,0.05)] backdrop-blur supports-[backdrop-filter]:bg-neutral-50/80 dark:border-neutral-800/50 dark:bg-neutral-950/95 dark:shadow-[0_-4px_16px_-4px_rgba(0,0,0,0.2)] dark:supports-[backdrop-filter]:bg-neutral-950/80 lg:-mx-6 lg:px-6"
              data-testid="new-run-wizard-footer"
            >
              <WizardNavButtons
                onBack={goBack}
                onNext={isReviewStep ? undefined : goNext}
                onSubmit={isReviewStep ? submitRun : undefined}
                onSaveDraft={saveWizardDraft}
                submitting={submitting}
                canProceed={canProceed}
                isFirstStep={isFirstStep}
                isLastInputStep={isReviewStep}
                nextLabel={stepIndex === 0 ? "Continue" : "Next"}
                submitLabel="Create request"
                submittingLabel="Creating…"
              />
            </div>
          ) : null}

          {stepIndex === 6 && !runId ? (
            <p className="text-sm text-red-600">Run id missing; cannot track pipeline.</p>
          ) : null}

            </>
          ) : null}

          {wizardModeReady ? (
            <div ref={liveRef} aria-live="polite" aria-atomic="true" className="sr-only">
              {liveMessage}
            </div>
          ) : null}
        </div>
      </FormProvider>
  );
}
