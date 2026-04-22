"use client";

import Link from "next/link";
import { useState } from "react";
import { useFormContext } from "react-hook-form";

import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardFooter, CardHeader, CardTitle } from "@/components/ui/card";
import { Textarea } from "@/components/ui/textarea";
import { WizardStepPanel } from "@/components/wizard/WizardStepPanel";
import { applySecondRunPasteToWizard } from "@/lib/second-run-paste";
import { applyWizardPreset, wizardPresets } from "@/lib/wizard-presets";
import { verticalBriefWizardPresets } from "@/lib/vertical-wizard-presets";
import { buildDefaultWizardValues, type WizardFormValues } from "@/lib/wizard-schema";

export type WizardStepPresetProps = {
  /** Optional hook for analytics/tests when a named preset is applied (not fired for “Use defaults”). */
  onPresetSelect?: (presetId: string) => void;
  /** Trial onboarding: highlight the seeded demo run created for this tenant. */
  featuredSampleRunId?: string | null;
  /** Optional toast / inline notice when SECOND_RUN paste succeeds or fails. */
  onWizardNotice?: (kind: "ok" | "err", message: string) => void;
};

/**
 * Step 1: pick a preset or start from scratch (`reset` with defaults).
 */
export function WizardStepPreset(props: WizardStepPresetProps = {}) {
  const { onPresetSelect, featuredSampleRunId, onWizardNotice } = props;
  const { reset, getValues } = useFormContext<WizardFormValues>();
  const [secondRunPaste, setSecondRunPaste] = useState("");

  const applySecondRunPaste = () => {
    const current = getValues();
    const outcome = applySecondRunPasteToWizard(secondRunPaste, current);

    if (!outcome.ok) {
      onWizardNotice?.("err", outcome.error);

      return;
    }

    reset(outcome.values);
    onWizardNotice?.("ok", "Applied SECOND_RUN paste — review Identity on the next step.");
  };

  const selectPreset = (presetId: string, values: Partial<WizardFormValues>) => {
    onPresetSelect?.(presetId);
    const merged = applyWizardPreset(buildDefaultWizardValues(), values);
    reset(merged);
  };

  const startScratch = () => {
    reset(buildDefaultWizardValues());
  };

  return (
    <WizardStepPanel
      title="Choose a starting point"
      description="Pick a template to pre-fill common fields, or start from scratch with validated defaults."
    >
      <Card className="mb-6 border-dashed border-teal-300/80 bg-neutral-50/80 dark:border-teal-800 dark:bg-neutral-950/40">
        <CardHeader>
          <CardTitle className="text-base">Paste a SECOND_RUN.toml (or JSON)</CardTitle>
          <CardDescription>
            Same one-page schema as <code className="rounded bg-neutral-100 px-1 text-xs dark:bg-neutral-800">archlucid second-run</code> —{" "}
            <Link className="text-teal-700 underline" href="https://github.com/joefrancisGA/ArchLucid/blob/main/docs/SECOND_RUN.md">
              docs/SECOND_RUN.md
            </Link>
            . Apply, then click <strong>Next</strong> to confirm identity and constraints.
          </CardDescription>
        </CardHeader>
        <CardContent className="space-y-3">
          <Textarea
            data-testid="second-run-paste-textarea"
            value={secondRunPaste}
            onChange={(e) => setSecondRunPaste(e.target.value)}
            placeholder={`name = "My.Service"\ndescription = "At least ten characters describing goals."`}
            className="min-h-[140px] font-mono text-xs"
            spellCheck={false}
          />
        </CardContent>
        <CardFooter>
          <Button type="button" variant="secondary" data-testid="second-run-apply-paste" onClick={applySecondRunPaste}>
            Apply pasted SECOND_RUN
          </Button>
        </CardFooter>
      </Card>

      {featuredSampleRunId !== null && featuredSampleRunId !== undefined && featuredSampleRunId.length > 0 ? (
        <Card className="mb-4 border-teal-200 bg-teal-50/80 dark:border-teal-900 dark:bg-teal-950/40">
          <CardHeader>
            <CardTitle className="text-base text-teal-950 dark:text-teal-50">Trial sample run (pre-seeded)</CardTitle>
            <CardDescription className="text-teal-900/90 dark:text-teal-100/90">
              Open the governed demo pipeline we created for your workspace, or continue below to author a brand-new
              architecture request.
            </CardDescription>
          </CardHeader>
          <CardFooter>
            <Button asChild type="button" className="w-full sm:w-auto">
              <Link href={`/runs/${featuredSampleRunId}`} data-testid="wizard-open-trial-sample-run">
                Open sample run
              </Link>
            </Button>
          </CardFooter>
        </Card>
      ) : null}

      <div className="mb-6">
        <h3 className="mb-1 text-base font-semibold text-neutral-900 dark:text-neutral-100">
          Start from a vertical template
        </h3>
        <p className="mb-3 max-w-prose text-sm text-neutral-600 dark:text-neutral-400">
          Pre-fill the wizard with regulated-industry starters (see{" "}
          <Link href="https://github.com/joefrancisGA/ArchLucid/blob/main/templates/README.md#vertical-industry-starters-prompt-11">
            templates/README.md
          </Link>
          ). Pair with a matching policy pack from Policy packs → Import a vertical policy pack.
        </p>
        <div className="grid gap-4 sm:grid-cols-2">
          {verticalBriefWizardPresets.map((preset) => (
            <Card key={preset.id} className="flex flex-col">
              <CardHeader>
                <CardTitle className="text-base">{preset.label}</CardTitle>
                <CardDescription>{preset.description}</CardDescription>
              </CardHeader>
              <CardContent className="flex-1" />
              <CardFooter>
                <Button
                  type="button"
                  className="w-full"
                  variant="secondary"
                  onClick={() => selectPreset(preset.id, preset.values)}
                >
                  Use vertical template
                </Button>
              </CardFooter>
            </Card>
          ))}
        </div>
      </div>

      <div className="grid gap-4 sm:grid-cols-2">
        {wizardPresets.map((preset) => (
          <Card key={preset.id} className="flex flex-col">
            <CardHeader>
              <CardTitle className="text-base">{preset.label}</CardTitle>
              <CardDescription>{preset.description}</CardDescription>
            </CardHeader>
            <CardContent className="flex-1" />
            <CardFooter>
              <Button
                type="button"
                className="w-full"
                variant="default"
                onClick={() => selectPreset(preset.id, preset.values)}
              >
                Select
              </Button>
            </CardFooter>
          </Card>
        ))}
        <Card className="flex flex-col border-dashed">
          <CardHeader>
            <CardTitle className="text-base">Start from scratch</CardTitle>
            <CardDescription>Reset the form to empty lists and placeholder text only.</CardDescription>
          </CardHeader>
          <CardContent className="flex-1" />
          <CardFooter>
            <Button type="button" className="w-full" variant="outline" onClick={startScratch}>
              Use defaults
            </Button>
          </CardFooter>
        </Card>
      </div>
    </WizardStepPanel>
  );
}
