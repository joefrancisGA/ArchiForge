"use client";

import { ChevronDown } from "lucide-react";
import Link from "next/link";
import { useMemo, useState } from "react";
import { useFormContext } from "react-hook-form";

import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardFooter, CardHeader, CardTitle } from "@/components/ui/card";
import { Collapsible, CollapsibleContent, CollapsibleTrigger } from "@/components/ui/collapsible";
import { Textarea } from "@/components/ui/textarea";
import { WizardStepPanel } from "@/components/wizard/WizardStepPanel";
import { applySecondRunPasteToWizard } from "@/lib/second-run-paste";
import { applyWizardPreset, wizardPresets, type WizardPreset } from "@/lib/wizard-presets";
import { documentationArchitectureRequestWizardPresets } from "@/lib/docs-architecture-request-presets";
import { getDocHref } from "@/lib/help-topics";
import { verticalBriefWizardPresets } from "@/lib/vertical-wizard-presets";
import { buildDefaultWizardValues, type WizardFormValues } from "@/lib/wizard-schema";
import { cn } from "@/lib/utils";

const HERO_VERTICAL_IDS = new Set<string>(["vertical-healthcare", "vertical-financial-services"]);

function verticalTemplateActionLabel(preset: WizardPreset): string {
  return `Use ${preset.label.toLowerCase()} starter`;
}

export type WizardStepPresetProps = {
  /** Optional hook for analytics/tests when a named preset is applied (not fired for “Use defaults”). */
  onPresetSelect?: (presetId: string) => void;
  /** Trial onboarding: highlight the seeded demo run created for this tenant. */
  featuredSampleRunId?: string | null;
  /** Optional toast / inline notice when import paste succeeds or fails. */
  onWizardNotice?: (kind: "ok" | "err", message: string) => void;
  /**
   * Called after the user picks a starting point (blank, industry/quick preset, or successful import). The parent
   * wizard should move off step 0; otherwise the in-place form reset has no visible effect when values were already
   * defaults.
   */
  onStartingPointCommitted?: () => void;
};

/**
 * Step 1: pick a preset or start from scratch (`reset` with defaults).
 */
export function WizardStepPreset(props: WizardStepPresetProps = {}) {
  const { onPresetSelect, featuredSampleRunId, onWizardNotice, onStartingPointCommitted } = props;
  const { reset, getValues } = useFormContext<WizardFormValues>();
  const [secondRunPaste, setSecondRunPaste] = useState("");
  const [importOpen, setImportOpen] = useState(false);

  const architectureTemplatesDocHref = useMemo(
    () => getDocHref("docs/templates/architecture-requests/README.md"),
    [],
  );

  const { heroVerticals, otherVerticals } = useMemo(() => {
    const hero: WizardPreset[] = [];
    const rest: WizardPreset[] = [];

    for (const preset of verticalBriefWizardPresets) {
      if (HERO_VERTICAL_IDS.has(preset.id)) {
        hero.push(preset);
      } else {
        rest.push(preset);
      }
    }

    hero.sort((a, b) => {
      if (a.id === "vertical-healthcare") {
        return -1;
      }

      if (b.id === "vertical-healthcare") {
        return 1;
      }

      return 0;
    });

    return { heroVerticals: hero, otherVerticals: rest };
  }, []);

  const applyStartingPointChoice = () => {
    onStartingPointCommitted?.();
  };

  const applySecondRunPaste = () => {
    const current = getValues();
    const outcome = applySecondRunPasteToWizard(secondRunPaste, current);

    if (!outcome.ok) {
      onWizardNotice?.("err", outcome.error);

      return;
    }

    reset(outcome.values);
    onWizardNotice?.("ok", "Request imported — continue to the next step to confirm details.");
    applyStartingPointChoice();
  };

  const selectPreset = (presetId: string, values: Partial<WizardFormValues>) => {
    onPresetSelect?.(presetId);
    const merged = applyWizardPreset(buildDefaultWizardValues(), values);
    reset(merged);
    applyStartingPointChoice();
  };

  const startScratch = () => {
    reset(buildDefaultWizardValues());
    applyStartingPointChoice();
  };

  return (
    <WizardStepPanel
      title="Start your architecture request"
      description={
        <div className="space-y-2">
          <p className="m-0">
            An architecture request captures the system, goals, constraints, and context ArchLucid uses to generate a
            manifest, findings, and artifacts.
          </p>
          <p className="m-0">
            Start from scratch, use an industry starter, Quick shapes or documentation-aligned templates, or import a
            prepared request file.
          </p>
        </div>
      }
    >
      {featuredSampleRunId !== null && featuredSampleRunId !== undefined && featuredSampleRunId.length > 0 ? (
        <Card className="mb-6 border-teal-200 bg-teal-50/80 dark:border-teal-900 dark:bg-teal-950/40">
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

      <Card className="mb-6 border-teal-200/90 bg-white dark:border-teal-900/50 dark:bg-neutral-950/40">
        <CardHeader>
          <CardTitle className="text-base">Start from scratch</CardTitle>
          <CardDescription>
            Use validated defaults and enter only the request details you know. You can refine identity, goals, and
            constraints in the following steps.
          </CardDescription>
        </CardHeader>
        <CardFooter>
          <Button 
            type="button" 
            className="w-full bg-teal-600 text-white hover:bg-teal-700 dark:bg-teal-700 dark:text-white dark:hover:bg-teal-600 sm:w-auto" 
            onClick={startScratch} 
            data-testid="wizard-start-blank"
          >
            Start from scratch
          </Button>
        </CardFooter>
      </Card>

      <div className="mb-6">
        <h3 className="mb-1 text-base font-semibold text-neutral-900 dark:text-neutral-100">Industry starters</h3>
        <p className="mb-3 max-w-prose text-sm text-neutral-600 dark:text-neutral-400">
          Pre-fill regulated-industry context. You can add matching policy packs later.
        </p>
        <div className="grid gap-4 sm:grid-cols-2">
          {heroVerticals.map((preset) => (
            <Card
              key={preset.id}
              className="flex flex-col border-teal-300/80 shadow-sm dark:border-teal-800/80"
            >
              <CardHeader>
                <CardTitle className="text-base">{preset.label}</CardTitle>
                <CardDescription>{preset.description}</CardDescription>
              </CardHeader>
              <CardContent className="flex-1" />
              <CardFooter>
                <Button
                  type="button"
                  className={cn(
                    "w-full border-teal-700 text-teal-900 hover:bg-teal-50 dark:border-teal-600 dark:text-teal-100 dark:hover:bg-teal-950/50",
                  )}
                  variant="outline"
                  onClick={() => selectPreset(preset.id, preset.values)}
                >
                  {verticalTemplateActionLabel(preset)}
                </Button>
              </CardFooter>
            </Card>
          ))}
        </div>
      </div>

      {otherVerticals.length > 0 ? (
        <div className="mb-6">
          <h3 className="mb-1 text-base font-semibold text-neutral-900 dark:text-neutral-100">
            More industry templates
          </h3>
          <div className="grid gap-4 sm:grid-cols-2">
            {otherVerticals.map((preset) => (
              <Card key={preset.id} className="flex flex-col">
                <CardHeader>
                  <CardTitle className="text-base">{preset.label}</CardTitle>
                  <CardDescription>{preset.description}</CardDescription>
                </CardHeader>
                <CardContent className="flex-1" />
                <CardFooter>
                  <Button
                    type="button"
                    className="w-full border-teal-700 text-teal-900 hover:bg-teal-50 dark:border-teal-600 dark:text-teal-100 dark:hover:bg-teal-950/50"
                    variant="outline"
                    onClick={() => selectPreset(preset.id, preset.values)}
                  >
                    {verticalTemplateActionLabel(preset)}
                  </Button>
                </CardFooter>
              </Card>
            ))}
          </div>
        </div>
      ) : null}

      <div className="mb-6">
        <h3 className="mb-1 text-base font-semibold text-neutral-900 dark:text-neutral-100">Quick shapes</h3>
        <p className="mb-3 max-w-prose text-sm text-neutral-600 dark:text-neutral-400">
          Opinionated starters for common delivery patterns (greenfield, modernization, data platform).
        </p>
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
                  className="w-full border-teal-700 text-teal-900 hover:bg-teal-50 dark:border-teal-600 dark:text-teal-100 dark:hover:bg-teal-950/50"
                  variant="outline"
                  onClick={() => selectPreset(preset.id, preset.values)}
                >
                  Use {preset.label.toLowerCase()}
                </Button>
              </CardFooter>
            </Card>
          ))}
        </div>
      </div>

      <div className="mb-6">
        <h3 className="mb-1 text-base font-semibold text-neutral-900 dark:text-neutral-100">Architecture request templates</h3>
        <p className="mb-3 max-w-prose text-sm text-neutral-600 dark:text-neutral-400">
          Opinionated payloads aligned with <code className="text-xs">POST /v1/architecture/request</code> (
          <code className="text-xs">ArchitectureRequest</code>). Same JSON bodies live under{" "}
          <code className="break-all text-xs">docs/templates/architecture-requests/</code> for paste-import.
          {architectureTemplatesDocHref !== null ? (
            <>
              {" "}
              <Link className="text-teal-700 underline" href={architectureTemplatesDocHref} target="_blank" rel="noreferrer">
                Open template index on GitHub
              </Link>
              .
            </>
          ) : null}
        </p>
        <div className="grid gap-4 sm:grid-cols-2">
          {documentationArchitectureRequestWizardPresets.map((preset, index) => (
            <Card key={preset.id} className="flex flex-col">
              <CardHeader>
                <CardTitle className="text-base">{preset.label}</CardTitle>
                <CardDescription>{preset.description}</CardDescription>
              </CardHeader>
              <CardContent className="flex-1" />
              <CardFooter>
                <Button
                  type="button"
                  variant="outline"
                  className="w-full border-teal-700 text-teal-900 hover:bg-teal-50 dark:border-teal-600 dark:text-teal-100 dark:hover:bg-teal-950/50"
                  data-testid={index === 0 ? "wizard-docs-architecture-template-first" : undefined}
                  onClick={() => selectPreset(preset.id, preset.values)}
                >
                  Use this template
                </Button>
              </CardFooter>
            </Card>
          ))}
        </div>
      </div>

      <Collapsible open={importOpen} onOpenChange={setImportOpen}>
        <CollapsibleTrigger asChild>
          <Button
            type="button"
            variant="outline"
            className="flex h-auto w-full items-center justify-between gap-2 py-3 text-left font-semibold"
            data-testid="wizard-import-request-toggle"
          >
            <span>Import prepared request</span>
            <ChevronDown className={cn("h-4 w-4 shrink-0 transition-transform", importOpen ? "rotate-180" : "")} aria-hidden />
          </Button>
        </CollapsibleTrigger>
        <CollapsibleContent className="pt-3">
          <Card className="border-dashed border-neutral-300 bg-neutral-50/80 dark:border-neutral-600 dark:bg-neutral-950/40">
            <CardHeader>
              <CardTitle className="text-base">Paste TOML or JSON</CardTitle>
              <CardDescription>
                Paste a prepared request in TOML or JSON format. ArchLucid validates fields, then pre-fills the wizard so
                you can review identity and constraints before continuing.
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
              <p className="m-0 text-xs text-neutral-500 dark:text-neutral-400">
                Compatible with the{" "}
                <Link className="text-teal-700 underline" href="https://github.com/joefrancisGA/ArchLucid/blob/main/docs/SECOND_RUN.md">
                  SECOND_RUN
                </Link>{" "}
                schema.
              </p>
            </CardContent>
            <CardFooter>
              <Button type="button" variant="secondary" data-testid="second-run-apply-paste" onClick={applySecondRunPaste}>
                Apply import
              </Button>
            </CardFooter>
          </Card>
        </CollapsibleContent>
      </Collapsible>
    </WizardStepPanel>
  );
}
