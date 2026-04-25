"use client";

import type { ReactNode } from "react";
import type { FieldErrors } from "react-hook-form";
import { useFormContext } from "react-hook-form";

import { Separator } from "@/components/ui/separator";
import { RunWizardCostPreviewCard } from "@/components/wizard/RunWizardCostPreviewCard";
import { WizardStepPanel } from "@/components/wizard/WizardStepPanel";
import type { WizardFormValues } from "@/lib/wizard-schema";

function ErrorList({ errors }: { errors: FieldErrors<WizardFormValues> }) {
  const messages: string[] = [];

  const walk = (prefix: string, err: unknown): void => {
    if (!err || typeof err !== "object") {
      return;
    }

    if ("message" in err && typeof (err as { message?: string }).message === "string") {
      const msg = (err as { message: string }).message;

      if (msg) {
        messages.push(prefix ? `${prefix}: ${msg}` : msg);
      }

      return;
    }

    for (const [key, val] of Object.entries(err)) {
      if (key === "ref" || key === "type" || key === "types") {
        continue;
      }

      walk(prefix ? `${prefix}.${key}` : key, val);
    }
  };

  walk("", errors);

  if (messages.length === 0) {
    return null;
  }

  return (
    <div
      role="alert"
      className="rounded-md border border-red-200 bg-red-50 p-3 text-sm text-red-900 dark:border-red-900 dark:bg-red-950/40 dark:text-red-100"
    >
      <p className="m-0 font-semibold">Fix validation errors before creating the run:</p>
      <ul className="mt-2 mb-0 list-disc pl-5">
        {messages.map((m) => (
          <li key={m}>{m}</li>
        ))}
      </ul>
    </div>
  );
}

function ReadOnlyBlock(props: { title: string; children: ReactNode }) {
  return (
    <section>
      <h3 className="text-sm font-semibold text-neutral-800 dark:text-neutral-200">{props.title}</h3>
      <div className="mt-2 text-sm text-neutral-700 dark:text-neutral-300">{props.children}</div>
    </section>
  );
}

/**
 * Step 6: read-only summary and validation error surface before API submit (handled by parent nav).
 */
export function WizardStepReview() {
  const { watch, formState } = useFormContext<WizardFormValues>();
  const v = watch();

  return (
    <WizardStepPanel
      title="Review & submit"
      description="Confirm values below. Use Back to edit earlier steps. Create run sends POST /v1/architecture/request."
    >
      <div className="space-y-4">
        <ErrorList errors={formState.errors} />

        <RunWizardCostPreviewCard />

        <ReadOnlyBlock title="Identity">
          <dl className="m-0 grid gap-1 sm:grid-cols-[8rem_1fr]">
            <dt className="text-neutral-500">System</dt>
            <dd className="m-0">{v.systemName}</dd>
            <dt className="text-neutral-500">Environment</dt>
            <dd className="m-0">{v.environment}</dd>
            <dt className="text-neutral-500">Cloud</dt>
            <dd className="m-0">{v.cloudProvider}</dd>
            <dt className="text-neutral-500">Prior manifest</dt>
            <dd className="m-0">{v.priorManifestVersion?.trim() || "—"}</dd>
          </dl>
        </ReadOnlyBlock>

        <Separator />

        <ReadOnlyBlock title="Description">
          <p className="m-0 whitespace-pre-wrap">{v.description}</p>
          {(v.inlineRequirements ?? []).some((s) => s.trim()) ? (
            <div className="mt-2">
              <p className="m-0 text-xs font-medium text-neutral-500">Inline requirements</p>
              <ul className="mt-1 list-disc pl-5">
                {(v.inlineRequirements ?? [])
                  .map((s) => s.trim())
                  .filter(Boolean)
                  .map((line) => (
                    <li key={line}>{line}</li>
                  ))}
              </ul>
            </div>
          ) : null}
        </ReadOnlyBlock>

        <Separator />

        <ReadOnlyBlock title="Constraints & capabilities">
          <p className="m-0 text-xs text-neutral-500">Constraints</p>
          <ul className="mt-1 list-disc pl-5">
            {(v.constraints ?? []).map((c) => (
              <li key={c}>{c}</li>
            ))}
          </ul>
          <p className="mt-2 m-0 text-xs text-neutral-500">Required capabilities</p>
          <ul className="mt-1 list-disc pl-5">
            {(v.requiredCapabilities ?? []).map((c) => (
              <li key={c}>{c}</li>
            ))}
          </ul>
          <p className="mt-2 m-0 text-xs text-neutral-500">Assumptions</p>
          <ul className="mt-1 list-disc pl-5">
            {(v.assumptions ?? []).map((c) => (
              <li key={c}>{c}</li>
            ))}
          </ul>
        </ReadOnlyBlock>

        <Separator />

        <ReadOnlyBlock title="Advanced">
          <p className="m-0 text-xs text-neutral-500">Policy references</p>
          <p className="m-0">{(v.policyReferences ?? []).join(", ") || "—"}</p>
          <p className="mt-2 m-0 text-xs text-neutral-500">Topology hints</p>
          <p className="m-0">{(v.topologyHints ?? []).join(", ") || "—"}</p>
          <p className="mt-2 m-0 text-xs text-neutral-500">Security baseline hints</p>
          <p className="m-0">{(v.securityBaselineHints ?? []).join(", ") || "—"}</p>
          <p className="mt-2 m-0 text-xs text-neutral-500">Documents</p>
          <p className="m-0">{(v.documents ?? []).filter((d) => d.name.trim()).length} attached</p>
          <p className="mt-2 m-0 text-xs text-neutral-500">Infrastructure declarations</p>
          <p className="m-0">
            {(v.infrastructureDeclarations ?? []).filter((d) => d.name.trim()).length} declaration(s)
          </p>
        </ReadOnlyBlock>

        <Separator />

        <ReadOnlyBlock title="Request id">
          <code className="text-xs">{v.requestId}</code>
        </ReadOnlyBlock>
      </div>
    </WizardStepPanel>
  );
}
