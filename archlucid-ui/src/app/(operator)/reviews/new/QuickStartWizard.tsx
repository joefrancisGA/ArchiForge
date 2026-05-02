"use client";

import { useCallback, useEffect, useMemo, useState } from "react";
import { useFormContext } from "react-hook-form";

import { WizardNavButtons } from "@/components/wizard/WizardNavButtons";
import { WizardStepDescription } from "@/components/wizard/steps/WizardStepDescription";
import { WizardStepIdentity } from "@/components/wizard/steps/WizardStepIdentity";
import { WizardStepReview } from "@/components/wizard/steps/WizardStepReview";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { architectureReviewTemplates, suggestedSystemNameFromTemplateId } from "@/data/review-templates";
import { createArchitectureRun } from "@/lib/api";
import { recordFirstTenantFunnelEvent } from "@/lib/first-tenant-funnel-telemetry";
import { showError, showSuccess } from "@/lib/toast";
import { wizardValuesToCreateRunPayload } from "@/lib/wizard-payload";
import { applyWizardPreset, wizardPresets, type WizardPreset } from "@/lib/wizard-presets";
import { buildDefaultWizardValues, type WizardFormValues } from "@/lib/wizard-schema";
import { WIZARD_STEP_FIELD_GROUPS } from "@/lib/wizard-step-fields";

const QUICK_STEPS = [
  { label: "System & preset", description: "Name your system and pick a starter profile" },
  { label: "Architecture brief", description: "Goals and scope (min. 10 characters)" },
  { label: "Review & submit", description: "Confirm defaults and create the request" },
] as const;

export type QuickStartWizardProps = {
  /** Invoked after a run id is returned so the parent can show pipeline tracking. */
  onRunCreated: (runId: string) => void;
};

/**
 * Three-step wizard that maps to the same `ArchitectureRequest` payload as the full wizard, using preset defaults
 * for constraints and advanced fields.
 */
export function QuickStartWizard(props: QuickStartWizardProps) {
  const { onRunCreated } = props;
  const [quickStep, setQuickStep] = useState(0);
  const [submitting, setSubmitting] = useState(false);
  const [presetId, setPresetId] = useState<string>("greenfield-web-app");

  const { trigger, getValues, reset, setValue, clearErrors } = useFormContext<WizardFormValues>();

  const selectedPreset: WizardPreset | undefined = useMemo(
    () => wizardPresets.find((p) => p.id === presetId),
    [presetId],
  );

  useEffect(() => {
    const preset = wizardPresets.find((p) => p.id === presetId);

    if (!preset) {
      return;
    }

    const merged = applyWizardPreset(buildDefaultWizardValues(), preset.values);
    reset(merged);
  }, [presetId, reset]);

  const canProceed = !submitting;
  const showToast = useCallback((kind: "ok" | "err", message: string) => {
    if (kind === "ok") {
      showSuccess(message);
    } else {
      showError("Quick start", message);
    }
  }, []);

  const applyReviewTemplate = (templateId: string) => {
    const template = architectureReviewTemplates.find((t) => t.id === templateId);

    if (!template) {
      return;
    }

    clearErrors(["description", "systemName"]);
    setValue("description", template.briefText, { shouldValidate: true, shouldDirty: true });
    setValue("systemName", suggestedSystemNameFromTemplateId(template.id), {
      shouldValidate: true,
      shouldDirty: true,
    });
  };

  const goBack = () => {
    setQuickStep((s) => Math.max(0, s - 1));
  };

  const goNext = async () => {
    const validationStepIndex = quickStep === 0 ? 1 : 2;
    const fieldGroup = WIZARD_STEP_FIELD_GROUPS[validationStepIndex];

    if (fieldGroup != null) {
      // Trigger validation for only the fields on this step
      const ok = await trigger(fieldGroup, { shouldFocus: true });
      if (!ok) {
        showToast("err", "Fix the highlighted fields before continuing.");
        return;
      }
    }

    setQuickStep((s) => Math.min(QUICK_STEPS.length - 1, s + 1));
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

      recordFirstTenantFunnelEvent("first_run_started");
      showToast("ok", `Run ${id} created — tracking pipeline below.`);
      onRunCreated(id);
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

  const isReviewStep = quickStep === 2;
  const isFirstStep = quickStep === 0;

  return (
    <div className="space-y-4 pb-36">
      <div className="space-y-1" data-testid="quick-start-progress">
        <p className="m-0 font-medium text-neutral-900 dark:text-neutral-100">
          Quick start — step {quickStep + 1} of {QUICK_STEPS.length}: {QUICK_STEPS[quickStep].label}
        </p>
        <p className="m-0 text-sm text-neutral-500 dark:text-neutral-400">{QUICK_STEPS[quickStep].description}</p>
      </div>

      {quickStep === 0 ? (
        <div className="space-y-4">
          <Card>
            <CardHeader>
              <CardTitle>Environment preset</CardTitle>
              <CardDescription>
                Applies default constraints and capability hints. You can fine-tune later from the full wizard.
              </CardDescription>
            </CardHeader>
            <CardContent>
              <Select value={presetId} onValueChange={setPresetId}>
                <SelectTrigger aria-label="Environment preset" data-testid="quick-start-preset-select">
                  <SelectValue placeholder="Choose a preset" />
                </SelectTrigger>
                <SelectContent>
                  {wizardPresets.map((p) => (
                    <SelectItem key={p.id} value={p.id} data-testid={`quick-start-preset-${p.id}`}>
                      {p.label}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </CardContent>
          </Card>
          {selectedPreset ? (
            <p className="text-sm text-neutral-600 dark:text-neutral-400" data-testid="quick-start-preset-caption">
              Using preset: <strong>{selectedPreset.label}</strong>
            </p>
          ) : null}
          <WizardStepIdentity />
        </div>
      ) : null}
      {quickStep === 1 ? (
        <div className="space-y-4">
          <Card>
            <CardHeader>
              <CardTitle>Start from a template</CardTitle>
              <CardDescription>
                One click fills the architecture brief (and a matching system name). Edit the textarea afterward if
                needed.
              </CardDescription>
            </CardHeader>
            <CardContent>
              <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
                {architectureReviewTemplates.map((t) => (
                  <button
                    key={t.id}
                    type="button"
                    data-testid={`quick-start-template-${t.id}`}
                    aria-label={`Use template: ${t.name}`}
                    onClick={() => {
                      applyReviewTemplate(t.id);
                    }}
                    className="flex flex-col items-start gap-1 rounded-lg border border-neutral-200/80 bg-white p-3 text-left text-sm shadow-sm transition hover:border-teal-600/45 hover:bg-neutral-50 dark:border-neutral-700 dark:bg-neutral-950/40 dark:hover:border-teal-500/40 dark:hover:bg-neutral-900/60"
                  >
                    <span className="font-medium text-neutral-900 dark:text-neutral-100">{t.name}</span>
                    <span className="text-xs text-neutral-600 dark:text-neutral-400">{t.description}</span>
                  </button>
                ))}
              </div>
            </CardContent>
          </Card>
          <WizardStepDescription />
        </div>
      ) : null}
      {quickStep === 2 ? <WizardStepReview /> : null}

      <div
        className="sticky bottom-0 z-10 -mx-4 mt-8 border-t border-neutral-200/60 bg-neutral-50/98 px-4 py-3 shadow-[0_-2px_8px_-2px_rgba(0,0,0,0.06)] backdrop-blur supports-[backdrop-filter]:bg-neutral-50/85 dark:border-neutral-800/60 dark:bg-neutral-950/98 dark:shadow-[0_-2px_8px_-2px_rgba(0,0,0,0.25)] dark:supports-[backdrop-filter]:bg-neutral-950/85 lg:-mx-6 lg:px-6"
        data-testid="quick-start-footer"
      >
        <WizardNavButtons
          onBack={goBack}
          onNext={isReviewStep ? undefined : goNext}
          onSubmit={isReviewStep ? submitRun : undefined}
          onSaveDraft={undefined}
          submitting={submitting}
          canProceed={canProceed}
          isFirstStep={isFirstStep}
          isLastInputStep={isReviewStep}
          nextLabel="Next"
          submitLabel="Create request"
          submittingLabel="Creating…"
        />
      </div>
    </div>
  );
}
