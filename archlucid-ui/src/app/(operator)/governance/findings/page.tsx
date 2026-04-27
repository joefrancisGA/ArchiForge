"use client";

import Link from "next/link";

import { LayerHeader } from "@/components/LayerHeader";
import { OperatorPageHeader } from "@/components/OperatorPageHeader";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";

/**
 * Hub route for **Findings** in the sidebar — detailed findings are run-scoped (run detail + inspect UIs).
 */
export default function GovernanceFindingsPage() {
  return (
    <>
      <LayerHeader pageKey="governance-findings" />
      <OperatorPageHeader title="Findings" />
      <div className="mt-4 space-y-4">
        <Card className="border border-neutral-200 shadow-sm dark:border-neutral-800">
          <CardHeader className="space-y-1">
            <CardTitle className="text-base font-semibold text-neutral-900 dark:text-neutral-100">
              Where to review findings
            </CardTitle>
            <p className="m-0 text-sm text-neutral-600 dark:text-neutral-400">
              Findings are attached to architecture runs. Open a run to see severity, citations, and review context.
            </p>
          </CardHeader>
          <CardContent className="flex flex-wrap gap-2">
            <Button asChild variant="primary" size="sm" className="h-9">
              <Link href="/runs?projectId=default">View runs</Link>
            </Button>
            <Button asChild variant="outline" size="sm" className="h-9 border-teal-300 dark:border-teal-700">
              <Link href="/governance">Governance workflow</Link>
            </Button>
          </CardContent>
        </Card>

        <Card className="border border-neutral-200 shadow-sm dark:border-neutral-800">
          <CardHeader className="space-y-1">
            <CardTitle className="text-base font-semibold text-neutral-900 dark:text-neutral-100">
              What findings look like
            </CardTitle>
            <p className="m-0 text-sm text-neutral-600 dark:text-neutral-400">
              Each finding includes a severity level, category, rationale, supporting evidence, and a recommended
              action when the analysis produced one.
            </p>
          </CardHeader>
          <CardContent>
            <p className="m-0 text-sm text-neutral-600 dark:text-neutral-400">
              Findings are produced as the architecture analysis pipeline runs and are attached to each run.
            </p>
          </CardContent>
        </Card>

        <Card className="border border-neutral-200 shadow-sm dark:border-neutral-800">
          <CardHeader className="space-y-1">
            <CardTitle className="text-base font-semibold text-neutral-900 dark:text-neutral-100">
              After your first run
            </CardTitle>
          </CardHeader>
          <CardContent>
            <ol className="m-0 list-decimal space-y-2 pl-5 text-sm text-neutral-600 dark:text-neutral-400">
              <li>Create an architecture request and wait for the pipeline to complete.</li>
              <li>Finalize the run to lock the manifest and surface findings.</li>
              <li>Return here or open run detail to review findings for that run.</li>
            </ol>
          </CardContent>
        </Card>
      </div>
    </>
  );
}
