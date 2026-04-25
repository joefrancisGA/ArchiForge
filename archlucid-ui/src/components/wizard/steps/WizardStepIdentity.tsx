"use client";

import { Controller, useFormContext } from "react-hook-form";

import { Badge } from "@/components/ui/badge";
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

/**
 * Step 2: system name, environment, cloud (Azure only), optional prior manifest version.
 */
export function WizardStepIdentity() {
  const { register, control, formState, clearErrors } = useFormContext<WizardFormValues>();
  const { errors } = formState;
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
                <SelectTrigger id="wizard-environment" className="w-full max-w-md">
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
                <SelectTrigger id="wizard-cloud-provider" className="w-full max-w-md">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="Azure">Microsoft Azure</SelectItem>
                  <SelectItem disabled value="Aws">
                    <span className="flex items-center gap-2">
                      Amazon Web Services
                      <Badge variant="secondary">Coming soon</Badge>
                    </span>
                  </SelectItem>
                  <SelectItem disabled value="Gcp">
                    <span className="flex items-center gap-2">
                      Google Cloud
                      <Badge variant="secondary">Coming soon</Badge>
                    </span>
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
