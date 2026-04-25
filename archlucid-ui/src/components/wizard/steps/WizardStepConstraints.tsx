"use client";

import { useState } from "react";
import { useFormContext } from "react-hook-form";

import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Separator } from "@/components/ui/separator";
import { WizardFieldError } from "@/components/wizard/WizardFieldError";
import { WizardFieldHint } from "@/components/wizard/WizardFieldHint";
import { WizardStepPanel } from "@/components/wizard/WizardStepPanel";
import type { WizardFormValues } from "@/lib/wizard-schema";

type ChipFieldName = "constraints" | "requiredCapabilities" | "assumptions";

function ChipListBlock(props: {
  fieldName: ChipFieldName;
  label: string;
  hint: string;
  inputId: string;
}) {
  const { watch, setValue, formState, clearErrors } = useFormContext<WizardFormValues>();
  const { errors } = formState;
  const items: string[] = watch(props.fieldName) ?? [];
  const [draft, setDraft] = useState("");
  const fieldError = errors[props.fieldName]?.message;

  const addItem = () => {
    const trimmed = draft.trim();

    if (!trimmed) {
      return;
    }

    clearErrors(props.fieldName);
    setValue(props.fieldName, [...items, trimmed], { shouldValidate: true, shouldDirty: true });
    setDraft("");
  };

  const removeItem = (index: number) => {
    clearErrors(props.fieldName);
    setValue(
      props.fieldName,
      items.filter((_, idx) => idx !== index),
      { shouldValidate: true, shouldDirty: true },
    );
  };

  return (
    <div className="space-y-2">
      <WizardFieldHint htmlFor={props.inputId} label={props.label} hint={props.hint} />
      <div className="flex flex-wrap gap-2">
        <Input
          id={props.inputId}
          value={draft}
          onChange={(e) => {
            setDraft(e.target.value);
            clearErrors(props.fieldName);
          }}
          onKeyDown={(e) => {
            if (e.key === "Enter") {
              e.preventDefault();
              addItem();
            }
          }}
          className="max-w-md flex-1 min-w-[12rem]"
          placeholder="Type and Add"
        />
        <Button type="button" variant="secondary" onClick={addItem}>
          Add
        </Button>
      </div>
      <WizardFieldError
        id={`err-wizard-${props.fieldName}`}
        message={fieldError != null ? String(fieldError) : undefined}
      />
      {items.length > 0 ? (
        <ul className="flex flex-wrap gap-2 p-0 list-none">
          {items.map((item, index) => (
            <li key={`${props.fieldName}-${index}-${item.slice(0, 12)}`}>
              <Badge variant="outline" className="gap-1 py-1 pl-2 pr-1 font-normal">
                <span className="max-w-[240px] truncate">{item}</span>
                <Button
                  type="button"
                  variant="ghost"
                  size="sm"
                  className="h-6 px-1 text-neutral-600"
                  onClick={() => removeItem(index)}
                  aria-label={`Remove ${item}`}
                >
                  ×
                </Button>
              </Badge>
            </li>
          ))}
        </ul>
      ) : (
        <p className="text-xs text-neutral-500">No items yet.</p>
      )}
    </div>
  );
}

/**
 * Step 4: constraints, required capabilities, assumptions as chip lists.
 */
export function WizardStepConstraints() {
  return (
    <WizardStepPanel
      title="Constraints, capabilities & assumptions"
      description="Capture hard limits, required platform traits, and safe assumptions for agents."
    >
      <div className="space-y-8">
        <ChipListBlock
          fieldName="constraints"
          label="Constraints"
          hint="Hard limits the proposed architecture must not violate (budget, regions, compliance, etc.)."
          inputId="wizard-constraints-draft"
        />
        <Separator />
        <ChipListBlock
          fieldName="requiredCapabilities"
          label="Required capabilities"
          hint="What the system must support — e.g. HTTPS ingress, managed database, observability."
          inputId="wizard-capabilities-draft"
        />
        <Separator />
        <ChipListBlock
          fieldName="assumptions"
          label="Assumptions"
          hint="Statements agents may treat as true unless contradicted by evidence (team skills, timelines)."
          inputId="wizard-assumptions-draft"
        />
      </div>
    </WizardStepPanel>
  );
}
