"use client";

import Link from "next/link";
import { useCallback, useEffect, useState } from "react";

import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardFooter, CardHeader, CardTitle } from "@/components/ui/card";

const STORAGE_KEY_STEP = "archlucid_onboarding_wizard_step";
const STORAGE_KEY_DONE = "archlucid_onboarding_wizard_completed";

const STEPS = [
  {
    id: "environment",
    title: "Environment",
    body: (
      <>
        Confirm you have the API and (optional) UI running. Local:{" "}
        <code className="rounded bg-neutral-100 px-1 py-0.5 text-xs dark:bg-neutral-800">archlucid dev up</code> or
        Docker Compose. Azure: follow <code className="text-xs">docs/GOLDEN_PATH.md</code> and{" "}
        <code className="text-xs">docs/REFERENCE_SAAS_STACK_ORDER.md</code> in the repository checkout.
      </>
    ),
  },
  {
    id: "auth",
    title: "Authentication",
    body: (
      <>
        Production-like pilots should use <strong>Entra JWT</strong> or <strong>API keys</strong> with{" "}
        <code className="rounded bg-neutral-100 px-1 py-0.5 text-xs dark:bg-neutral-800">
          Authentication:ApiKey:Enabled
        </code>
        . Development may use DevelopmentBypass only in non-production. Optional strict mode:{" "}
        <code className="rounded bg-neutral-100 px-1 py-0.5 text-xs dark:bg-neutral-800">
          ArchLucidAuth:RequireJwtBearerInProduction=true
        </code>
        .
      </>
    ),
  },
  {
    id: "connection",
    title: "API connection (UI)",
    body: (
      <>
        The operator shell proxies to the API using{" "}
        <code className="rounded bg-neutral-100 px-1 py-0.5 text-xs dark:bg-neutral-800">ARCHLUCID_API_BASE_URL</code>{" "}
        and{" "}
        <code className="rounded bg-neutral-100 px-1 py-0.5 text-xs dark:bg-neutral-800">ARCHLUCID_API_KEY</code> (see{" "}
        <code className="rounded bg-neutral-100 px-1 py-0.5 text-xs dark:bg-neutral-800">archlucid-ui/.env.example</code>
        ).
      </>
    ),
  },
  {
    id: "storage",
    title: "Storage mode",
    body: (
      <>
        <code className="rounded bg-neutral-100 px-1 py-0.5 text-xs dark:bg-neutral-800">ArchLucid:StorageProvider=Sql</code>{" "}
        for real pilots; <strong>InMemory</strong> is for tests and ephemeral demos only. SQL hosts must apply
        migrations on startup and enable RLS session context in staging/production when required.
      </>
    ),
  },
  {
    id: "first-run",
    title: "First run & demo",
    body: (
      <>
        Use the <strong>seven-step wizard</strong> to create a run, then follow manifest and artifacts from run detail.
        Contoso demo: see <code className="text-xs">docs/demo-quickstart.md</code> in the repo.
      </>
    ),
  },
] as const;

/**
 * Lightweight first-run checklist with persisted step index (localStorage).
 */
export function OnboardingWizardClient() {
  const [step, setStep] = useState(0);
  const [mounted, setMounted] = useState(false);

  useEffect(() => {
    setMounted(true);

    try {
      const raw = window.localStorage.getItem(STORAGE_KEY_DONE);

      if (raw === "1") {
        setStep(STEPS.length - 1);

        return;
      }

      const s = window.localStorage.getItem(STORAGE_KEY_STEP);

      if (s != null) {
        const n = Number.parseInt(s, 10);

        if (!Number.isNaN(n) && n >= 0 && n < STEPS.length)
          setStep(n);
      }
    } catch {
      /* private mode */
    }
  }, []);

  const persistStep = useCallback((next: number) => {
    setStep(next);

    try {
      window.localStorage.setItem(STORAGE_KEY_STEP, String(next));
    } catch {
      /* private mode */
    }
  }, []);

  const markComplete = useCallback(() => {
    try {
      window.localStorage.setItem(STORAGE_KEY_DONE, "1");
      window.localStorage.setItem(STORAGE_KEY_STEP, String(STEPS.length - 1));
    } catch {
      /* private mode */
    }
  }, []);

  const reset = useCallback(() => {
    try {
      window.localStorage.removeItem(STORAGE_KEY_DONE);
      window.localStorage.removeItem(STORAGE_KEY_STEP);
    } catch {
      /* private mode */
    }

    setStep(0);
  }, []);

  if (!mounted) {
    return (
      <Card className="max-w-2xl">
        <CardHeader>
          <CardTitle>Operator setup</CardTitle>
          <CardDescription>Loading…</CardDescription>
        </CardHeader>
      </Card>
    );
  }

  const current = STEPS[step];
  const isLast = step === STEPS.length - 1;

  return (
    <Card className="max-w-2xl border-teal-200/80 dark:border-teal-900/50">
      <CardHeader>
        <CardTitle>{current.title}</CardTitle>
        <CardDescription>
          Step {step + 1} of {STEPS.length}
        </CardDescription>
      </CardHeader>
      <CardContent className="space-y-4 text-sm leading-relaxed text-neutral-800 dark:text-neutral-200">
        <div>{current.body}</div>
        <div className="flex flex-wrap gap-2 pt-2">
          <Button type="button" variant="outline" disabled={step === 0} onClick={() => persistStep(step - 1)}>
            Back
          </Button>
          {!isLast ? (
            <Button type="button" onClick={() => persistStep(step + 1)}>
              Next
            </Button>
          ) : (
            <Button type="button" onClick={markComplete}>
              Mark complete
            </Button>
          )}
          <Button type="button" variant="ghost" onClick={reset}>
            Reset progress
          </Button>
        </div>
      </CardContent>
      <CardFooter className="flex flex-wrap gap-3 border-t pt-4 text-sm">
        <Link className="font-medium text-teal-700 underline dark:text-teal-300" href="/runs/new">
          Open new-run wizard
        </Link>
        <span className="text-neutral-400">|</span>
        <Link className="font-medium text-teal-700 underline dark:text-teal-300" href="/">
          Home checklist
        </Link>
      </CardFooter>
    </Card>
  );
}
