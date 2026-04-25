import type { FieldErrors } from "react-hook-form";

import type { WizardFormValues } from "@/lib/wizard-schema";

/** RHF field groups validated before leaving each step (0 = preset, 5/6 = N/A for Next). */
export const WIZARD_STEP_FIELD_GROUPS: Record<number, (keyof WizardFormValues)[] | null> = {
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
  5: null,
  6: null,
};

/**
 * True when the current step has RHF errors that should block the primary action (used on review for full form).
 * For input steps, primary navigation is always clickable; validate on click sets errors.
 */
export function stepHasBlockingFormErrors(
  stepIndex: number,
  errors: FieldErrors<WizardFormValues>,
): boolean {
  if (stepIndex === 5) {
    return Object.keys(errors).length > 0;
  }

  const fields = WIZARD_STEP_FIELD_GROUPS[stepIndex];

  if (!fields || fields.length === 0) {
    return false;
  }

  return fields.some((field) => errors[field] != null);
}
