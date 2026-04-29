"use client";

import Link from "next/link";
import { useEffect, useState } from "react";

import { EmptyState } from "@/components/EmptyState";
import { LayerHeader } from "@/components/LayerHeader";
import { OperatorPageHeader } from "@/components/OperatorPageHeader";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { getRunExplanationSummary, listRunsByProjectPaged } from "@/lib/api";
import {
  SHOWCASE_STATIC_DEMO_MANIFEST_ID,
  SHOWCASE_STATIC_DEMO_PRIMARY_FINDING_ID,
  SHOWCASE_STATIC_DEMO_RUN_ID,
} from "@/lib/showcase-static-demo";
import type { FindingTraceConfidenceDto } from "@/types/explanation";
import type { RunSummary } from "@/types/authority";

export type GovernanceFindingQueueRow = {
  runId: string;
  runLabel: string;
  /** Canonical manifest UUID when known, or "—". */
  manifestId: string;
  findingId: string;
  title: string;
  severity: string;
  category: string;
  status: string;
  recommended: string;
};

function isPublicDemoMode(): boolean {
  return process.env.NEXT_PUBLIC_DEMO_MODE === "true" || process.env.NEXT_PUBLIC_DEMO_MODE === "1";
}

function demoPhiRow(): GovernanceFindingQueueRow {
  return {
    runId: SHOWCASE_STATIC_DEMO_RUN_ID,
    runLabel: "Claims Intake Modernization",
    manifestId: SHOWCASE_STATIC_DEMO_MANIFEST_ID,
    findingId: SHOWCASE_STATIC_DEMO_PRIMARY_FINDING_ID,
    title: "PHI Minimization Risk",
    severity: "High",
    category: "Privacy / regulated data",
    status: "Open",
    recommended:
      "Review PHI handling posture with intake and security owners before production rollout.",
  };
}

function severityFromTrace(label: string | null | undefined): string {
  const t = (label ?? "").trim();

  if (t.length === 0) {
    return "—";
  }

  if (/high|critical|severe/i.test(t)) {
    return "High";
  }

  if (/low|minimal/i.test(t)) {
    return "Low";
  }

  if (/medium|moderate/i.test(t)) {
    return "Medium";
  }

  return t.length > 32 ? `${t.slice(0, 29)}…` : t;
}

function traceRowsForRun(run: RunSummary, traces: FindingTraceConfidenceDto[]): GovernanceFindingQueueRow[] {
  return traces
    .filter((t) => (t.findingId ?? "").trim().length > 0)
    .map((t) => {
      const findingId = t.findingId.trim();
      const titleRaw = (t.findingTitle ?? findingId).trim();
      const manifestRaw = (run.goldenManifestId ?? "").trim();

      return {
        runId: run.runId,
        runLabel: ((run.description ?? "").trim().length > 0 ? run.description : run.runId) ?? run.runId,
        manifestId: manifestRaw.length > 0 ? manifestRaw : "—",
        findingId,
        title: titleRaw.length > 0 ? titleRaw : findingId,
        severity: severityFromTrace(t.traceConfidenceLabel),
        category: (t.ruleId ?? "—").replace(/;/g, ", "),
        status: "Open",
        recommended: "Open the finding to review rationale, evidence, and recommended next steps.",
      };
    });
}

function dedupeRows(rows: GovernanceFindingQueueRow[]): GovernanceFindingQueueRow[] {
  const seen = new Set<string>();
  const out: GovernanceFindingQueueRow[] = [];

  for (const r of rows) {
    const key = `${r.runId}:${r.findingId}`;

    if (seen.has(key)) {
      continue;
    }

    seen.add(key);
    out.push(r);
  }

  return out;
}

function inspectHref(runId: string, findingId: string): string {
  return `/runs/${encodeURIComponent(runId)}/findings/${encodeURIComponent(findingId)}/inspect`;
}

function manifestHref(manifestId: string): string {
  return `/manifests/${encodeURIComponent(manifestId)}`;
}

/**
 * Findings hub: cross-run queue from explainability aggregates, plus a deterministic PHI sample row in public demo mode.
 */
export default function GovernanceFindingsQueueClient() {
  const [rows, setRows] = useState<GovernanceFindingQueueRow[]>([]);
  const [loading, setLoading] = useState(true);
  const [loadFailed, setLoadFailed] = useState(false);

  useEffect(() => {
    let cancelled = false;

    void (async () => {
      setLoading(true);
      setLoadFailed(false);

      const demo = isPublicDemoMode();

      try {
        const page = await listRunsByProjectPaged("default", 1, 25);
        const runItems = page.items ?? [];
        const maxRuns = Math.min(runItems.length, 12);
        const slice = runItems.slice(0, maxRuns);
        const collected: GovernanceFindingQueueRow[] = [];

        await Promise.all(
          slice.map(async (r) => {
            try {
              const summary = await getRunExplanationSummary(r.runId);
              const traces =
                summary.findingTraceConfidences ?? summary.explanation.findingTraceConfidences ?? [];

              if (traces === null || traces.length === 0) {
                return;
              }

              collected.push(...traceRowsForRun(r, traces));
            } catch {
              /* omit runs that cannot load aggregate (permissions, draft run, etc.) */
            }
          }),
        );

        if (cancelled) {
          return;
        }

        let merged = dedupeRows(collected);

        if (demo) {
          const hasPhi = merged.some((x) => x.findingId === SHOWCASE_STATIC_DEMO_PRIMARY_FINDING_ID);

          if (!hasPhi) {
            merged = [demoPhiRow(), ...merged];
          }
        }

        setRows(merged);
      } catch {
        if (cancelled) {
          return;
        }

        setLoadFailed(true);

        if (demo) {
          setRows([demoPhiRow()]);
        } else {
          setRows([]);
        }
      } finally {
        if (!cancelled) {
          setLoading(false);
        }
      }
    })();

    return () => {
      cancelled = true;
    };
  }, []);

  return (
    <>
      <LayerHeader pageKey="governance-findings" density="compact" />
      <OperatorPageHeader title="Findings" />

      <div className="mt-4 space-y-4">
        <p className="m-0 max-w-3xl text-sm text-neutral-600 dark:text-neutral-400">
          Findings from architecture runs — severity, category, and links to inspect each item in context.
        </p>

        {loading ? (
          <p className="m-0 text-sm text-neutral-500 dark:text-neutral-400">Loading findings…</p>
        ) : null}

        {!loading && rows.length > 0 ? (
          <div className="space-y-3">
            {rows.map((row) => (
              <Card
                key={`${row.runId}:${row.findingId}`}
                className="border border-neutral-200 shadow-sm dark:border-neutral-800"
              >
                <CardHeader className="space-y-1 pb-2">
                  <CardTitle className="text-base font-semibold text-neutral-900 dark:text-neutral-100">
                    <Link
                      className="text-teal-800 underline hover:text-teal-900 dark:text-teal-300 dark:hover:text-teal-200"
                      href={inspectHref(row.runId, row.findingId)}
                    >
                      {row.title}
                    </Link>
                  </CardTitle>
                  <p className="m-0 text-xs text-neutral-500 dark:text-neutral-400">
                    {row.runLabel} · {row.findingId}
                  </p>
                  <div className="mt-2 grid gap-3 border-t border-neutral-100 pt-2 text-xs sm:grid-cols-3 dark:border-neutral-800">
                    <div>
                      <div className="font-medium text-neutral-600 dark:text-neutral-400">Manifest</div>
                      <div className="mt-0.5">
                        {row.manifestId !== "—" ? (
                          <Link
                            className="text-teal-800 underline hover:text-teal-900 dark:text-teal-300 dark:hover:text-teal-200"
                            href={manifestHref(row.manifestId)}
                          >
                            Open manifest
                          </Link>
                        ) : (
                          <span className="text-neutral-500 dark:text-neutral-400">—</span>
                        )}
                      </div>
                    </div>
                    <div>
                      <div className="font-medium text-neutral-600 dark:text-neutral-400">Run</div>
                      <div className="mt-0.5">
                        <Link
                          className="text-teal-800 underline hover:text-teal-900 dark:text-teal-300 dark:hover:text-teal-200"
                          href={`/runs/${encodeURIComponent(row.runId)}`}
                        >
                          {row.runLabel}
                        </Link>
                      </div>
                    </div>
                    <div className="sm:col-span-1">
                      <div className="font-medium text-neutral-600 dark:text-neutral-400">Recommended action</div>
                      <p className="m-0 mt-0.5 text-neutral-600 dark:text-neutral-400">{row.recommended}</p>
                    </div>
                  </div>
                </CardHeader>
                <CardContent className="grid gap-2 pt-0 text-sm sm:grid-cols-2">
                  <div>
                    <span className="font-medium text-neutral-700 dark:text-neutral-300">Severity</span>
                    <p className="m-0 mt-0.5 text-neutral-600 dark:text-neutral-400">{row.severity}</p>
                  </div>
                  <div>
                    <span className="font-medium text-neutral-700 dark:text-neutral-300">Category</span>
                    <p className="m-0 mt-0.5 text-neutral-600 dark:text-neutral-400">{row.category}</p>
                  </div>
                  <div>
                    <span className="font-medium text-neutral-700 dark:text-neutral-300">Status</span>
                    <p className="m-0 mt-0.5 text-neutral-600 dark:text-neutral-400">{row.status}</p>
                  </div>
                  <div className="sm:col-span-2">
                    <Button asChild variant="outline" size="sm" className="h-9 border-teal-300 dark:border-teal-700">
                      <Link href={inspectHref(row.runId, row.findingId)}>Open finding</Link>
                    </Button>
                  </div>
                </CardContent>
              </Card>
            ))}
          </div>
        ) : null}

        {!loading && rows.length === 0 ? (
          <EmptyState
            title="No findings in queue yet"
            description={
              loadFailed
                ? "Runs could not be loaded. Create a request, complete a pipeline run, then return here."
                : "When runs produce findings, they appear here with links to inspect. You can still open a run from the list to review findings in context."
            }
            actions={[
              { label: "View runs", href: "/runs?projectId=default", variant: "primary" },
              { label: "Governance workflow", href: "/governance", variant: "outline" },
            ]}
          />
        ) : null}

        {!loading && rows.length === 0 ? (
          <details className="rounded-lg border border-neutral-200 bg-white p-4 dark:border-neutral-800 dark:bg-neutral-950/60">
            <summary className="cursor-pointer text-sm font-semibold text-neutral-800 dark:text-neutral-200">
              What findings look like
            </summary>
            <p className="mt-2 text-sm text-neutral-600 dark:text-neutral-400">
              Each finding includes a severity level, category, rationale, supporting evidence, and a recommended action
              when the analysis produced one. Findings are attached to architecture runs.
            </p>
            <ol className="mb-0 mt-3 list-decimal space-y-2 pl-5 text-sm text-neutral-600 dark:text-neutral-400">
              <li>Create an architecture request and wait for the pipeline to complete.</li>
              <li>Finalize the run to lock the manifest and surface findings.</li>
              <li>Return here or open run detail to review findings for that run.</li>
            </ol>
          </details>
        ) : null}
      </div>
    </>
  );
}
