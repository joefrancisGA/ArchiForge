import { z } from "zod";

const wizardDocumentSchema = z.object({
  name: z.string(),
  contentType: z.string(),
  content: z.string(),
});

const wizardInfrastructureDeclarationSchema = z.object({
  name: z.string(),
  format: z.string(),
  content: z.string(),
});

/** Optional prior manifest: empty, dashed UUID, or 32 hex (no dashes). */
function isPriorManifestVersionValid(value: string): boolean {
  const t = value.trim();
  if (t.length === 0) {
    return true;
  }

  if (
    /^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$/i.test(t)
  ) {
    return true;
  }

  return /^[0-9a-fA-F]{32}$/i.test(t);
}

/**
 * Zod model for the new-run wizard — mirrors `CreateArchitectureRunRequestPayload` (`api.ts`) with
 * `cloudProvider` fixed to Azure. Array fields are required; use `buildDefaultWizardValues()` or presets for empties.
 */
export const wizardFormSchema = z.object({
  requestId: z.string().min(1),
  description: z
    .string()
    .min(10, "Description must be at least 10 characters so agents have enough context.")
    .max(4000, "Description is too long."),
  systemName: z.string().min(1, "System name is required — use a short project slug, e.g. OrderService."),
  environment: z.string().min(1, "Select an environment."),
  cloudProvider: z.literal("Azure"),
  constraints: z.array(z.string()),
  requiredCapabilities: z.array(z.string()),
  assumptions: z.array(z.string()),
  priorManifestVersion: z.string().refine(isPriorManifestVersionValid, {
    message: "Use a valid UUID, or leave blank for greenfield.",
  }),
  inlineRequirements: z.array(z.string()),
  documents: z.array(wizardDocumentSchema),
  policyReferences: z.array(z.string()),
  topologyHints: z.array(z.string()),
  securityBaselineHints: z.array(z.string()),
  infrastructureDeclarations: z.array(wizardInfrastructureDeclarationSchema),
});

export type WizardFormValues = z.infer<typeof wizardFormSchema>;

/**
 * Fresh wizard state: new `requestId` (32-char hex, no dashes — aligns with .NET `Guid.ToString("N")`),
 * Azure + staging, empty lists, placeholder text satisfying validation minima.
 */
export function buildDefaultWizardValues(): WizardFormValues {
  const requestId: string = crypto.randomUUID().replace(/-/g, "");

  return wizardFormSchema.parse({
    requestId,
    description:
      "Describe the system, scope, and what the architecture must achieve (at least ten characters).",
    systemName: "TargetSystem",
    environment: "staging",
    cloudProvider: "Azure",
    priorManifestVersion: "",
    constraints: [],
    requiredCapabilities: [],
    assumptions: [],
    inlineRequirements: [],
    documents: [],
    policyReferences: [],
    topologyHints: [],
    securityBaselineHints: [],
    infrastructureDeclarations: [],
  });
}
