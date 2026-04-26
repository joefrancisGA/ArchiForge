"use client";

import Link from "next/link";

import { LayerHeader } from "@/components/LayerHeader";
import { OperatorPageHeader } from "@/components/OperatorPageHeader";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";

/**
 * Hub route for **Findings** in the sidebar — detailed findings are run-scoped (run detail + inspect UIs).
 * This page orients pilots without a separate cross-run findings index yet.
 */
export default function GovernanceFindingsPage() {
  return (
    <>
      <LayerHeader pageKey="governance-findings" />
      <OperatorPageHeader title="Findings" />
      <Card className="mt-4 border border-neutral-200 shadow-sm dark:border-neutral-800">
        <CardHeader className="space-y-1">
          <CardTitle className="text-base font-semibold text-neutral-900 dark:text-neutral-100">
            Where to review findings
          </CardTitle>
          <p className="m-0 text-sm text-neutral-600 dark:text-neutral-400">
            Findings are attached to architecture runs (snapshots, explainability, and governance lineage). Open a run to see
            severity, citations, and review state.
          </p>
        </CardHeader>
        <CardContent className="flex flex-wrap gap-2">
          <Button asChild variant="primary" size="sm" className="h-9">
            <Link href="/runs?projectId=default">View runs</Link>
          </Button>
          <Button asChild variant="outline" size="sm" className="h-9 border-teal-300 dark:border-teal-700">
            <Link href="/governance/dashboard">Governance dashboard</Link>
          </Button>
        </CardContent>
      </Card>
    </>
  );
}
