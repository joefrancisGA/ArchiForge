"use client";

import { Controller, useFormContext } from "react-hook-form";

import { Button } from "@/components/ui/button";
import { Textarea } from "@/components/ui/textarea";
import { WizardFieldError } from "@/components/wizard/WizardFieldError";
import { WizardFieldHint } from "@/components/wizard/WizardFieldHint";
import { WizardStepPanel } from "@/components/wizard/WizardStepPanel";
import type { WizardFormValues } from "@/lib/wizard-schema";

/**
 * Step 3: primary description + dynamic inline requirements.
 */
export function WizardStepDescription() {
  const { control, watch, setValue, formState, clearErrors } = useFormContext<WizardFormValues>();
  const { errors } = formState;
  const descErr = errors.description?.message;
  const inlineReqErr = errors.inlineRequirements?.message;
  const description = watch("description") ?? "";
  const inlineRequirements = watch("inlineRequirements") ?? [];

  const updateInlineRequirement = (index: number, value: string) => {
    const next = [...inlineRequirements];
    next[index] = value;
    clearErrors("inlineRequirements");
    setValue("inlineRequirements", next, { shouldValidate: true, shouldDirty: true });
  };

  const appendInlineRequirement = () => {
    clearErrors("inlineRequirements");
    setValue("inlineRequirements", [...inlineRequirements, ""], { shouldValidate: true, shouldDirty: true });
  };

  const removeInlineRequirement = (index: number) => {
    clearErrors("inlineRequirements");
    setValue(
      "inlineRequirements",
      inlineRequirements.filter((_, idx) => idx !== index),
      { shouldValidate: true, shouldDirty: true },
    );
  };

  return (
    <WizardStepPanel
      title="Description & requirements"
      description="The main narrative agents use, plus optional structured requirement lines."
    >
      <div className="space-y-6">
        <div>
          <WizardFieldHint
            htmlFor="wizard-description"
            label="Description"
            hint="Describe what the architecture must achieve. The AI agents use this as the primary input signal."
          />
          <Controller
            name="description"
            control={control}
            render={({ field }) => (
              <Textarea
                id="wizard-description"
                rows={8}
                className="min-h-[140px]"
                {...field}
                onChange={(e) => {
                  clearErrors("description");
                  field.onChange(e);
                }}
                aria-invalid={descErr != null && String(descErr).length > 0}
                aria-describedby={
                  descErr != null && String(descErr).length > 0
                    ? "wizard-description-count err-wizard-description"
                    : "wizard-description-count"
                }
              />
            )}
          />
          <WizardFieldError
            id="err-wizard-description"
            message={descErr != null ? String(descErr) : undefined}
          />
          <p id="wizard-description-count" className="mt-1 text-xs text-neutral-500">
            {description.length} / 4000 characters (minimum 10)
          </p>
        </div>

        <div>
          <WizardFieldHint
            htmlFor="wizard-inline-req-0"
            label="Inline requirements"
            hint="Supplementary requirements beyond the description. One per line item."
          />
          <WizardFieldError
            id="err-wizard-inline-req"
            message={inlineReqErr != null ? String(inlineReqErr) : undefined}
          />
          <div className="space-y-3">
            {inlineRequirements.map((line, index) => (
              <div key={`inline-${index}`} className="flex gap-2">
                <Textarea
                  id={index === 0 ? "wizard-inline-req-0" : undefined}
                  rows={3}
                  className="min-h-[72px] flex-1"
                  value={line}
                  onChange={(e) => updateInlineRequirement(index, e.target.value)}
                />
                <Button type="button" variant="outline" onClick={() => removeInlineRequirement(index)}>
                  Remove
                </Button>
              </div>
            ))}
            <Button type="button" variant="secondary" onClick={appendInlineRequirement}>
              Add requirement
            </Button>
          </div>
        </div>
      </div>
    </WizardStepPanel>
  );
}
