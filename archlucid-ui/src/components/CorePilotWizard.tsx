"use client";

import { Compass } from "lucide-react";
import Link from "next/link";
import type { ReactNode } from "react";
import { useCallback, useEffect, useReducer, useState } from "react";

import { HelpLink } from "@/components/HelpLink";
import { Button } from "@/components/ui/button";
import {
  Dialog,
  DialogDescription,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Progress } from "@/components/ui/progress";
import {
  CORE_PILOT_WIZARD_OPEN_EVENT,
  CORE_PILOT_WIZARD_STEP_COUNT,
  CORE_PILOT_WIZARD_STORAGE_KEY,
  corePilotWizardReducer,
  createInitialCorePilotWizardState,
  parseStoredCorePilotWizardState,
  stringifyCorePilotWizardState,
} from "@/lib/core-pilot-wizard-state";

type WizardBlueprintStep = {
  id: string;
  title: string;
  body: ReactNode;
};

const BLUEPRINT_STEPS: WizardBlueprintStep[] = [
  {
    id: "welcome",
    title: "Welcome to the Core Pilot",
    body: (
      <p className="m-0 leading-relaxed text-neutral-700 dark:text-neutral-200">
        This guided path follows <strong>docs/library/V1_SCOPE.md (section Core operator happy path, Pilot)</strong> — the shortest journey every pilot must complete
        (configure → readiness → structured request → execute → commit → review). Experts can dismiss anytime; progress is remembered
        in this browser.
      </p>
    ),
  },
  {
    id: "configure",
    title: "Configure storage, connection string, and auth",
    body: (
      <div className="space-y-2 leading-relaxed text-neutral-700 dark:text-neutral-200">
        <p className="m-0">
          Stand up Sql + credential wiring for your scope (see PILOT_GUIDE). When unsure, anchor on tenant isolation expectations and documented
          private endpoints.
        </p>
        <p className="m-0">
          <Link
            href="/getting-started"
            className="font-medium text-teal-700 underline-offset-4 hover:underline dark:text-teal-400"
          >
            Open Getting started →
          </Link>
        </p>
      </div>
    ),
  },
  {
    id: "health",
    title: "Start the API and confirm readiness",
    body: (
      <div className="space-y-2 leading-relaxed text-neutral-700 dark:text-neutral-200">
        <p className="m-0">
          Hit <strong>live/ready</strong> probes and jot the <strong>version</strong> string for ticketing. The footer strip surfaces quick health cues in-product.
        </p>
        <p className="m-0">
          <Link href="/admin/health" className="font-medium text-teal-700 underline-offset-4 hover:underline dark:text-teal-400">
            Open admin health diagnostics →
          </Link>
        </p>
      </div>
    ),
  },
  {
    id: "create-request",
    title: "Create a structured architecture request",
    body: (
      <div className="space-y-2 leading-relaxed text-neutral-700 dark:text-neutral-200">
        <p className="m-0">Use the operator wizard to capture identity, constraints, and advanced inputs (`POST /v1/architecture/request`).</p>
        <p className="m-0">
          <Link href="/runs/new" className="font-medium text-teal-700 underline-offset-4 hover:underline dark:text-teal-400">
            Launch new-request wizard →
          </Link>
        </p>
      </div>
    ),
  },
  {
    id: "execute",
    title: "Execute the pipeline and watch progress",
    body: (
      <div className="space-y-2 leading-relaxed text-neutral-700 dark:text-neutral-200">
        <p className="m-0">
          The coordinator hydrates snapshots and authority phases. Simulator mode finishes quickly; tracked mode surfaces the Pipeline timeline inside run detail.
        </p>
        <p className="m-0">
          <Link
            href="/runs?projectId=default"
            className="font-medium text-teal-700 underline-offset-4 hover:underline dark:text-teal-400"
          >
            Open Runs → choose your run →
          </Link>
        </p>
      </div>
    ),
  },
  {
    id: "commit",
    title: "Commit the reviewed manifest",
    body: (
      <div className="space-y-2 leading-relaxed text-neutral-700 dark:text-neutral-200">
        <p className="m-0">
          Finalize commits the reviewed manifest and synthesizes artifacts — nothing comparable until this succeeds via the Architecture run commit route on your API host.
        </p>
        <p className="m-0">
          <Link
            href="/runs?projectId=default"
            className="font-medium text-teal-700 underline-offset-4 hover:underline dark:text-teal-400"
          >
            Navigate to Runs → finalize from run detail →
          </Link>
        </p>
      </div>
    ),
  },
  {
    id: "review",
    title: "Review manifest + artifact bundle exports",
    body: (
      <div className="space-y-2 leading-relaxed text-neutral-700 dark:text-neutral-200">
        <p className="m-0">
          After finalize, skim manifest summary + artifact previews, then optionally export bundle ZIP off run detail — this satisfies the Pilot deliverable in CORE_PILOT.md.
        </p>
      </div>
    ),
  },
];

function wizardHiddenForAutomation(): boolean {
  if (typeof window === "undefined") {
    return false;
  }

  if (process.env.NEXT_PUBLIC_SUPPRESS_CORE_PILOT_WIZARD === "1") {
    return true;
  }

  const navigatorLike = window.navigator as Navigator & { webdriver?: boolean };

  if (navigatorLike.webdriver === true) {
    return true;
  }

  return false;
}

/** Modal Core Pilot navigator + FAB launcher persisted under `archlucid.corePilotWizard.v1`. */
export function CorePilotWizardLauncher() {
  const [hydrated, setHydrated] = useState(false);
  const [dialogOpen, setDialogOpen] = useState(false);
  const [suppressFabOffer, setSuppressFabOffer] = useState(false);
  const [state, dispatch] = useReducer(corePilotWizardReducer, createInitialCorePilotWizardState());

  /** Hydrate from localStorage once on mount. */
  useEffect(() => {
    try {
      const parsed = parseStoredCorePilotWizardState(window.localStorage.getItem(CORE_PILOT_WIZARD_STORAGE_KEY));
      dispatch({ type: "hydrate", state: parsed ?? createInitialCorePilotWizardState() });
    } catch {
      dispatch({ type: "hydrate", state: createInitialCorePilotWizardState() });
    }

    setHydrated(true);
  }, []);

  /** Persist durable preferences + checkpoints. */
  useEffect(() => {
    if (!hydrated) {
      return;
    }

    try {
      window.localStorage.setItem(CORE_PILOT_WIZARD_STORAGE_KEY, stringifyCorePilotWizardState(state));
    } catch {
      /* private mode quota */
    }
  }, [hydrated, state]);

  useEffect(() => {
    function onWizardOpenRequested() {
      setDialogOpen(true);
    }

    window.addEventListener(CORE_PILOT_WIZARD_OPEN_EVENT, onWizardOpenRequested);

    return () => {
      window.removeEventListener(CORE_PILOT_WIZARD_OPEN_EVENT, onWizardOpenRequested);
    };
  }, []);

  const handleDialogChange = useCallback((open: boolean) => {
    setDialogOpen(open);

    if (!open) {
      dispatch({ type: "closePreserveProgress" });
    }
  }, []);

  useEffect(() => {
    setSuppressFabOffer(false);
  }, [state.stepIndex]);

  const step = BLUEPRINT_STEPS[state.stepIndex] ?? BLUEPRINT_STEPS[0];
  const pct = Math.round(((state.stepIndex + 1) / CORE_PILOT_WIZARD_STEP_COUNT) * 100);
  const onLastScreen = state.stepIndex >= CORE_PILOT_WIZARD_STEP_COUNT - 1;
  const navigatorAllowed = hydrated && !state.preferences.dontShowNavigator && !wizardHiddenForAutomation();

  function handlePrimaryAdvance() {
    if (state.status === "completed" && state.stepIndex === CORE_PILOT_WIZARD_STEP_COUNT - 1) {
      if (suppressFabOffer) {
        dispatch({ type: "suppressNavigator" });
      }

      setDialogOpen(false);

      return;
    }

    if (onLastScreen) {
      dispatch({ type: "markCompleted" });

      if (suppressFabOffer) {
        dispatch({ type: "suppressNavigator" });
      }

      setDialogOpen(false);

      return;
    }

    dispatch({ type: "next" });
  }

  if (!navigatorAllowed || !BLUEPRINT_STEPS.length) {
    return null;
  }

  const resumeLabel =
    state.status === "completed"
      ? "Core Pilot recap"
      : `Core Pilot wizard (${Math.min(state.stepIndex + 1, CORE_PILOT_WIZARD_STEP_COUNT)}/${CORE_PILOT_WIZARD_STEP_COUNT})`;

  return (
    <>
      <Button
        type="button"
        size="sm"
        data-core-pilot-wizard-trigger=""
        aria-haspopup="dialog"
        aria-expanded={dialogOpen}
        title={`Open guided Core Pilot (${state.stepIndex + 1}/${CORE_PILOT_WIZARD_STEP_COUNT})`}
        className={`fixed bottom-5 right-4 z-40 h-11 gap-2 rounded-full px-4 shadow-lg print:!hidden lg:bottom-7 ${
          dialogOpen ? "ring-2 ring-teal-500/70" : ""
        }`}
        variant="secondary"
        onClick={() => {
          setDialogOpen(true);
        }}
      >
        <Compass className="h-4 w-4" aria-hidden />
        <span>{resumeLabel}</span>
      </Button>

      <Dialog open={dialogOpen} onOpenChange={handleDialogChange}>
        <DialogContent
          className="max-h-[calc(100vh-3rem)] w-[min(100vw-2rem,32rem)] max-w-xl overflow-y-auto pb-10 sm:p-8"
          aria-describedby="core-pilot-wizard-body"
        >
          <DialogHeader className="space-y-4 text-left">
            <DialogDescription className="sr-only">
              Guided checklist aligned with docs/library/V1_SCOPE.md pilot-path section and docs/CORE_PILOT.md; progress saves locally in this browser.
            </DialogDescription>

            <div className="flex flex-wrap items-center gap-3">
              <DialogTitle>{step.title}</DialogTitle>

              <HelpLink docPath="/docs/CORE_PILOT.md" label="Open the Core Pilot guide on GitHub (new tab)" />
            </div>

            <div className="space-y-3">
              <div className="flex items-center justify-between text-[11px] font-semibold uppercase tracking-wide text-neutral-600 dark:text-neutral-400">
                <span>
                  Pilot step <span className="text-neutral-900 dark:text-neutral-100">{state.stepIndex + 1}</span> /{" "}
                  {CORE_PILOT_WIZARD_STEP_COUNT}
                </span>
                <span>{pct}%</span>
              </div>

              <Progress value={pct} className="h-1.5" />
            </div>
          </DialogHeader>

          <div id="core-pilot-wizard-body" className="space-y-4 pb-8 text-sm text-neutral-800 dark:text-neutral-100">
            {step.body}
          </div>

          <DialogFooter className="space-y-3 sm:flex-row sm:items-start sm:justify-between sm:gap-4">
            {onLastScreen ? (
              <label className="flex items-start gap-2 text-xs text-neutral-600 dark:text-neutral-300">
                <input
                  type="checkbox"
                  className="mt-1 h-4 w-4 shrink-0 rounded border border-neutral-400 text-teal-600 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-1 focus-visible:outline-teal-600 dark:border-neutral-500 dark:bg-neutral-950"
                  checked={suppressFabOffer}
                  onChange={(e) => {
                    setSuppressFabOffer(e.target.checked);
                  }}
                />
                Don&apos;t show the Core Pilot launcher again (preference stored on this browser)
              </label>
            ) : (
              <span className="text-xs text-neutral-500 dark:text-neutral-400">Close anytime — your step resumes from the Compass button.</span>
            )}
            <div className="flex flex-col gap-2 sm:flex-row">
              <Button
                variant="outline"
                type="button"
                disabled={state.stepIndex === 0}
                onClick={() => {
                  dispatch({ type: "back" });
                }}
              >
                Back
              </Button>

              <Button type="button" variant="secondary" onClick={handlePrimaryAdvance}>
                {onLastScreen ? (state.status === "completed" ? "Done" : "Finish") : "Continue"}
              </Button>
            </div>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </>
  );
}
