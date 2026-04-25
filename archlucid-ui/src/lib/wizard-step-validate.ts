import { z } from "zod";

import { wizardFormSchema, type WizardFormValues } from "@/lib/wizard-schema";
import { WIZARD_STEP_FIELD_GROUPS } from "@/lib/wizard-step-fields";

const stepPickSchema: Record<number, z.ZodTypeAny | null> = {
  0: null,
  1: wizardFormSchema.pick({
    systemName: true,
    environment: true,
    cloudProvider: true,
    priorManifestVersion: true,
  }),
  2: wizardFormSchema.pick({ description: true, inlineRequirements: true }),
  3: wizardFormSchema.pick({ constraints: true, requiredCapabilities: true, assumptions: true }),
  4: wizardFormSchema.pick({
    policyReferences: true,
    topologyHints: true,
    securityBaselineHints: true,
    documents: true,
    infrastructureDeclarations: true,
  }),
  5: null,
  6: null,
};

export type WizardStepFieldError = { field: string; message: string };

/**
 * Per-step Zod `pick` validation. Empty array = valid to advance. Used on Next, not async.
 */
export function validateWizardStep(stepIndex: number, values: WizardFormValues): WizardStepFieldError[] {
  const sub = stepPickSchema[stepIndex];

  if (sub === null) {
    return [];
  }

  const result = sub.safeParse(values);

  if (result.success) {
    return [];
  }

  const allowed = new Set<string>(
    (WIZARD_STEP_FIELD_GROUPS[stepIndex] ?? []) as string[],
  );
  if (allowed.size === 0) {
    return [];
  }

  const byPath = new Map<string, string>();
  for (const issue of result.error.issues) {
    const root = issue.path[0];

    if (root === undefined || !allowed.has(String(root))) {
      continue;
    }

    const path = issue.path.map((p) => p.toString()).join(".");
    if (!byPath.has(path)) {
      byPath.set(path, issue.message);
    }
  }
  return [...byPath].map(([field, message]) => ({ field, message }));
}
