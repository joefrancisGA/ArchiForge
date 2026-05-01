import Link from "next/link";
import { notFound } from "next/navigation";

import { getArchitecturePackageDocxUrl, getRunExplanationSummary, getRunSummary } from "@/lib/api";
import type { ApiLoadFailureState } from "@/lib/api-load-failure";
import { isApiNotFoundFailure, toApiLoadFailure } from "@/lib/api-load-failure";
import { severityFromTrace, severitySortRank } from "@/lib/executive-finding-severity";
import { isInvalidGuidOrSlugRouteToken } from "@/lib/route-dynamic-param";
import type { FindingTraceConfidenceDto } from "@/types/explanation";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader } from "@/components/ui/card";

type ExecutiveFindingRow = {
  findingId: string;
  title: string;
  severity: string;
  confidence: string;
  recommended: string;
};

function traceToRows(traces: FindingTraceConfidenceDto[]): ExecutiveFindingRow[] {
  const withTrace = traces
    .filter((t) => (t.findingId ?? "").trim().length > 0)
    .map((t) => {
      const findingId = t.findingId.trim();
      const titleRaw = (t.findingTitle ?? findingId).trim();
      const firstAction = (t.recommendedActions ?? []).find((a: string) => a.trim().length > 0)?.trim();

      const row: ExecutiveFindingRow = {
        findingId,
        title: titleRaw.length > 0 ? titleRaw : findingId,
        severity: severityFromTrace(t.traceConfidenceLabel),
        confidence:
          (t.traceConfidenceLabel ?? "—").trim().length > 0 ? String(t.traceConfidenceLabel).trim() : "—",
        recommended: firstAction ?? "See finding detail for recommended next steps.",
      };

      return { row, sortKey: severitySortRank(t.traceConfidenceLabel) };
    });

  withTrace.sort((a, b) => a.sortKey - b.sortKey);

  return withTrace.map((x) => x.row);
}

function findingExecutiveHref(runId: string, findingId: string): string {
  return `/executive/reviews/${encodeURIComponent(runId)}/findings/${encodeURIComponent(findingId)}`;
}

/**
 * Single-review executive findings board: severity-sorted table + architecture package export.
 */
export default async function ExecutiveReviewFindingsPage({ params }: { params: Promise<{ runId: string }> }) {
  const { runId } = await params;

  if (isInvalidGuidOrSlugRouteToken(runId)) {
    notFound();
  }

  let summary: Awaited<ReturnType<typeof getRunExplanationSummary>> | null = null;
  let runSummary: Awaited<ReturnType<typeof getRunSummary>> | null = null;
  let failure: ApiLoadFailureState | null = null;

  try {
    runSummary = await getRunSummary(runId);
  } catch (e) {
    if (isApiNotFoundFailure(toApiLoadFailure(e))) {
      notFound();
    }
  }

  try {
    summary = await getRunExplanationSummary(runId);
  } catch (e) {
    const f = toApiLoadFailure(e);

    if (isApiNotFoundFailure(f)) {
      notFound();
    }

    failure = f;
  }

  const headline =
    runSummary !== null && (runSummary.description ?? "").trim().length > 0
      ? (runSummary.description ?? "").trim()
      : runId;

  const traces =
    summary?.findingTraceConfidences ?? summary?.explanation?.findingTraceConfidences ?? [];
  const rows = traceToRows(traces ?? []);

  return (
    <div className="space-y-6">
      <div className="flex flex-wrap items-center gap-3 text-sm">
        <Link
          href="/executive/reviews"
          className="font-medium text-teal-800 underline hover:text-teal-900 dark:text-teal-300 dark:hover:text-teal-200"
        >
          ← All reviews
        </Link>
        <span className="text-neutral-400 dark:text-neutral-600" aria-hidden>
          |
        </span>
        <Link
          href={`/reviews/${encodeURIComponent(runId)}`}
          className="text-neutral-600 underline hover:text-neutral-800 dark:text-neutral-400 dark:hover:text-neutral-200"
        >
          Open in operator shell
        </Link>
      </div>

      <header className="space-y-2">
        <p className="m-0 text-sm font-medium uppercase tracking-wide text-teal-800 dark:text-teal-300">
          Risk review
        </p>
        <h1 className="m-0 text-2xl font-semibold text-neutral-900 dark:text-neutral-100">{headline}</h1>
        {summary !== null ? (
          <p className="m-0 max-w-2xl text-sm leading-relaxed text-neutral-600 dark:text-neutral-400">
            <span className="font-medium text-neutral-800 dark:text-neutral-200">Risk posture:</span>{" "}
            {summary.riskPosture}
          </p>
        ) : null}
      </header>

      {failure !== null && summary === null ? (
        <Card className="border-red-200 bg-red-50/40 dark:border-red-900 dark:bg-red-950/30">
          <CardHeader className="pb-2">
            <CardDescription className="text-base font-medium text-neutral-900 dark:text-neutral-100">
              Could not load review summary
            </CardDescription>
          </CardHeader>
          <CardContent>
            <p className="m-0 text-sm text-neutral-700 dark:text-neutral-300">{failure.message}</p>
            {failure.httpStatus !== null ? (
              <p className="m-0 mt-2 text-xs text-neutral-500">HTTP {failure.httpStatus}</p>
            ) : null}
          </CardContent>
        </Card>
      ) : null}

      {summary !== null ? (
        <Card>
          <CardContent className="grid gap-3 pt-6 sm:grid-cols-2">
            <div>
              <p className="m-0 text-xs font-semibold uppercase tracking-wide text-neutral-500 dark:text-neutral-400">
                Findings
              </p>
              <p className="m-0 mt-1 text-lg font-semibold text-neutral-900 dark:text-neutral-100">
                {summary.findingCount}
              </p>
            </div>
            <div>
              <p className="m-0 text-xs font-semibold uppercase tracking-wide text-neutral-500 dark:text-neutral-400">
                Unresolved issues
              </p>
              <p className="m-0 mt-1 text-lg font-semibold text-neutral-900 dark:text-neutral-100">
                {summary.unresolvedIssueCount}
              </p>
            </div>
            <div>
              <p className="m-0 text-xs font-semibold uppercase tracking-wide text-neutral-500 dark:text-neutral-400">
                Compliance gaps
              </p>
              <p className="m-0 mt-1 text-lg font-semibold text-neutral-900 dark:text-neutral-100">
                {summary.complianceGapCount}
              </p>
            </div>
            <div>
              <p className="m-0 text-xs font-semibold uppercase tracking-wide text-neutral-500 dark:text-neutral-400">
                Overall assessment
              </p>
              <p className="m-0 mt-1 text-sm text-neutral-700 dark:text-neutral-300">{summary.overallAssessment}</p>
            </div>
          </CardContent>
        </Card>
      ) : null}

      {summary !== null ? (
        <div className="space-y-3">
          <div className="flex flex-wrap items-center justify-between gap-3">
            <h2 className="m-0 text-lg font-semibold text-neutral-900 dark:text-neutral-100">Prioritized findings</h2>
            <Button variant="outline" size="sm" asChild>
              <a href={getArchitecturePackageDocxUrl(runId)}>Download architecture package (DOCX)</a>
            </Button>
          </div>

          {rows.length === 0 ? (
            <p className="m-0 text-sm text-neutral-600 dark:text-neutral-400">
              No findings were returned for this review. Check operator shell for pipeline status.
            </p>
          ) : (
            <div className="hidden overflow-x-auto rounded-lg border border-neutral-200 dark:border-neutral-800 md:block">
              <table className="w-full min-w-[720px] border-collapse text-left text-sm">
                <thead className="bg-neutral-100 text-xs font-semibold uppercase tracking-wide text-neutral-600 dark:bg-neutral-900 dark:text-neutral-400">
                  <tr>
                    <th className="px-3 py-2">Severity</th>
                    <th className="px-3 py-2">Finding</th>
                    <th className="px-3 py-2">Confidence</th>
                    <th className="px-3 py-2">Recommended action</th>
                    <th className="px-3 py-2"> </th>
                  </tr>
                </thead>
                <tbody>
                  {rows.map((row) => (
                    <tr
                      key={row.findingId}
                      className="border-t border-neutral-200 dark:border-neutral-800"
                    >
                      <td className="px-3 py-2 align-top text-neutral-800 dark:text-neutral-200">{row.severity}</td>
                      <td className="px-3 py-2 align-top font-medium text-neutral-900 dark:text-neutral-100">
                        <Link
                          className="text-teal-800 underline hover:text-teal-900 dark:text-teal-300 dark:hover:text-teal-200"
                          href={findingExecutiveHref(runId, row.findingId)}
                        >
                          {row.title}
                        </Link>
                        <div className="mt-0.5 font-mono text-[11px] font-normal text-neutral-500">{row.findingId}</div>
                      </td>
                      <td className="px-3 py-2 align-top text-xs text-neutral-600 dark:text-neutral-400">
                        {row.confidence}
                      </td>
                      <td className="px-3 py-2 align-top text-xs text-neutral-600 dark:text-neutral-400">
                        {row.recommended}
                      </td>
                      <td className="px-3 py-2 align-top">
                        <Button variant="outline" size="sm" className="h-8" asChild>
                          <Link href={findingExecutiveHref(runId, row.findingId)}>Open</Link>
                        </Button>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}

          {rows.length > 0 ? (
            <div className="space-y-3 md:hidden">
              {rows.map((row) => (
                <Card key={row.findingId} className="border border-neutral-200 dark:border-neutral-800">
                  <CardHeader className="space-y-1 pb-2">
                    <CardDescription className="text-xs font-medium text-neutral-500 dark:text-neutral-400">
                      {row.severity} · {row.confidence}
                    </CardDescription>
                    <p className="m-0 text-base font-semibold text-neutral-900 dark:text-neutral-100">
                      <Link
                        className="text-teal-800 underline hover:text-teal-900 dark:text-teal-300 dark:hover:text-teal-200"
                        href={findingExecutiveHref(runId, row.findingId)}
                      >
                        {row.title}
                      </Link>
                    </p>
                  </CardHeader>
                  <CardContent className="space-y-2 pt-0">
                    <p className="m-0 text-sm text-neutral-600 dark:text-neutral-400">{row.recommended}</p>
                    <Button variant="outline" size="sm" asChild>
                      <Link href={findingExecutiveHref(runId, row.findingId)}>Open finding</Link>
                    </Button>
                  </CardContent>
                </Card>
              ))}
            </div>
          ) : null}
        </div>
      ) : null}
    </div>
  );
}
