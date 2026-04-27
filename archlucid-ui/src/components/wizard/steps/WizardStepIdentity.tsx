"use client";

import { Controller, useFormContext } from "react-hook-form";

import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { Separator } from "@/components/ui/separator";
import { WizardFieldError } from "@/components/wizard/WizardFieldError";
import { WizardFieldHint } from "@/components/wizard/WizardFieldHint";
import { WizardStepPanel } from "@/components/wizard/WizardStepPanel";
import type { WizardFormValues } from "@/lib/wizard-schema";

const ENVIRONMENT_OPTIONS = [
  { value: "staging", label: "Staging" },
  { value: "production", label: "Production" },
  { value: "development", label: "Development" },
  { value: "sandbox", label: "Sandbox" },
] as const;

/** Shared with Environment + Cloud selects so the wizard matches Input focus/border weight. */
const wizardSelectTriggerClassName =
  "w-full max-w-md border-neutral-200/90 bg-white text-left shadow-sm transition-colors hover:border-neutral-300 focus:ring-teal-600/35 dark:border-neutral-600 dark:bg-neutral-950/40 dark:hover:border-neutral-500";

/**
 * Step 2: system name, environment, cloud (Azure only), optional prior manifest version.
 */
export function WizardStepIdentity() {
  const { register, control, formState, clearErrors, watch } = useFormContext<WizardFormValues>();
  const { errors } = formState;
  const systemNameValue = watch("systemName") ?? "";
  const systemErr = errors.systemName?.message;
  const priorErr = errors.priorManifestVersion?.message;
  const environmentErr = errors.environment?.message;
  const cloudErr = errors.cloudProvider?.message;

  return (
    <WizardStepPanel title="System identity" description="Names and deployment targets for this architecture request.">
      <div className="space-y-6">
        <div>
          <WizardFieldHint
            htmlFor="wizard-systemName"
            label="System name"
            hint="Short project slug, e.g. OrderService. Used as the ingestion project ID."
          />
          <Input
            id="wizard-systemName"
            autoComplete="off"
            aria-invalid={systemErr != null && String(systemErr).length > 0}
            aria-describedby={systemErr ? "err-wizard-systemName" : undefined}
            {...register("systemName", {
              onChange: () => {
                clearErrors("systemName");
              },
            })}
          />
          <WizardFieldError id="err-wizard-systemName" message={systemErr != null ? String(systemErr) : undefined} />
          <p className="mt-1 text-xs text-neutral-500 dark:text-neutral-400">
            {systemNameValue.trim().length} characters (minimum 2)
          </p>
        </div>

        <div>
          <Label className="mb-1 block" htmlFor="wizard-environment">
            Environment
          </Label>
          <Controller
            name="environment"
            control={control}
            render={({ field }) => (
              <Select
                value={field.value}
                onValueChange={(v) => {
                  clearErrors("environment");
                  field.onChange(v);
                }}
              >
                <SelectTrigger id="wizard-environment" className={wizardSelectTriggerClassName}>
                  <SelectValue placeholder="Select environment" />
                </SelectTrigger>
                <SelectContent>
                  {ENVIRONMENT_OPTIONS.map((opt) => (
                    <SelectItem key={opt.value} value={opt.value}>
                      {opt.label}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            )}
          />
          <WizardFieldError
            id="err-wizard-environment"
            message={environmentErr != null ? String(environmentErr) : undefined}
          />
        </div>

        <div>
          <Label className="mb-1 block" htmlFor="wizard-cloud-provider">
            Cloud provider
          </Label>
          <Controller
            name="cloudProvider"
            control={control}
            render={({ field }) => (
              <Select
                value={field.value}
                onValueChange={(v) => {
                  clearErrors("cloudProvider");
                  field.onChange(v);
                }}
              >
                <SelectTrigger id="wizard-cloud-provider" className={wizardSelectTriggerClassName}>
                  <SelectValue placeholder="Select cloud provider" />
                </SelectTrigger>
                <SelectContent className="border-neutral-200/90 dark:border-neutral-600">
                  <SelectItem value="Azure">Microsoft Azure</SelectItem>
                  <SelectItem className="text-neutral-500 dark:text-neutral-400" disabled value="Aws">
                    Amazon Web Services
                    <span className="ml-1.5 text-xs text-neutral-400 dark:text-neutral-500">(coming soon)</span>
                  </SelectItem>
                  <SelectItem className="text-neutral-500 dark:text-neutral-400" disabled value="Gcp">
                    Google Cloud
                    <span className="ml-1.5 text-xs text-neutral-400 dark:text-neutral-500">(coming soon)</span>
                  </SelectItem>
                </SelectContent>
              </Select>
            )}
          />
          <WizardFieldError
            id="err-wizard-cloud"
            message={cloudErr != null ? String(cloudErr) : undefined}
          />
          <p className="mt-1 text-xs text-neutral-500">Only Azure is available in this release.</p>
        </div>

        <Separator />

        <div>
          <WizardFieldHint
            htmlFor="wizard-priorManifest"
            label="Prior manifest version (optional)"
            hint="Leave blank for greenfield. Enter a version string to use as baseline for incremental changes."
          />
          <Input
            id="wizard-priorManifest"
            autoComplete="off"
            aria-invalid={priorErr != null && String(priorErr).length > 0}
            aria-describedby={priorErr ? "err-wizard-priorManifest" : undefined}
            {...register("priorManifestVersion", {
              onChange: () => {
                clearErrors("priorManifestVersion");
              },
            })}
          />
          <WizardFieldError id="err-wizard-priorManifest" message={priorErr != null ? String(priorErr) : undefined} />
        </div>
      </div>
    </WizardStepPanel>
  );
}
