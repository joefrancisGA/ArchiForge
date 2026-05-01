import Link from "next/link";
import { notFound } from "next/navigation";

import { FindingInspectFindingBody } from "@/app/(operator)/runs/[runId]/findings/[findingId]/FindingInspectFindingBody";
import { OperatorApiProblem } from "@/components/OperatorApiProblem";
import { getFindingInspect } from "@/lib/api";
import type { ApiLoadFailureState } from "@/lib/api-load-failure";
import { isApiNotFoundFailure, toApiLoadFailure } from "@/lib/api-load-failure";
import { findingDetailHeadingTitle } from "@/lib/finding-display-from-inspect";
import { tryStaticDemoFindingInspect } from "@/lib/operator-static-demo";
import { isInvalidDynamicRouteToken, isInvalidGuidOrSlugRouteToken } from "@/lib/route-dynamic-param";
import { sameAuthorityRunId } from "@/app/(operator)/runs/[runId]/findings/[findingId]/FindingInspectView";
import type { FindingInspectPayload } from "@/types/finding-inspect";

function normalizeInspectPayload(payload: FindingInspectPayload): FindingInspectPayload {
  return {
    ...payload,
    recommendedActions: payload.recommendedActions ?? [],
  };
}

/**
 * Executive finding detail: buyer-facing narrative via {@link FindingInspectFindingBody} (detail variant).
 */
export default async function ExecutiveFindingDetailPage({
  params,
}: {
  params: Promise<{ runId: string; findingId: string }>;
}) {
  const { runId, findingId } = await params;

  if (isInvalidGuidOrSlugRouteToken(runId)) {
    notFound();
  }

  if (isInvalidDynamicRouteToken(findingId)) {
    notFound();
  }

  const decodedFindingId = decodeURIComponent(findingId);

  let payload: FindingInspectPayload | null = null;
  let failure: ApiLoadFailureState | null = null;

  try {
    payload = normalizeInspectPayload(await getFindingInspect(runId, decodedFindingId));
  } catch (e) {
    failure = toApiLoadFailure(e);

    const staticInspect = tryStaticDemoFindingInspect(runId, decodedFindingId);

    if (staticInspect !== null) {
      payload = normalizeInspectPayload(staticInspect);
      failure = null;
    } else if (isApiNotFoundFailure(failure)) {
      notFound();
    }
  }

  if (failure !== null || payload === null) {
    return (
      <div className="space-y-4">
        <Link
          href={`/executive/reviews/${encodeURIComponent(runId)}`}
          className="text-sm font-medium text-teal-800 underline dark:text-teal-300"
        >
          ← Back to risk review
        </Link>
        <h1 className="m-0 text-xl font-semibold text-neutral-900 dark:text-neutral-100">Finding</h1>
        <OperatorApiProblem
          problem={failure?.problem ?? null}
          fallbackMessage={failure?.message ?? "Finding could not be loaded."}
          correlationId={failure?.correlationId ?? null}
        />
      </div>
    );
  }

  if (!sameAuthorityRunId(payload.runId, runId)) {
    return (
      <div className="space-y-4">
        <p className="m-0 text-sm text-neutral-700 dark:text-neutral-300">
          This finding belongs to a different review. Open it from the correct risk review.
        </p>
        <Link
          href={`/executive/reviews/${encodeURIComponent(payload.runId)}/findings/${encodeURIComponent(decodedFindingId)}`}
          className="text-teal-800 underline dark:text-teal-300"
        >
          Open under the correct review
        </Link>
      </div>
    );
  }

  const safePayload = payload;

  return (
    <div className="space-y-6">
      <div className="flex flex-wrap items-center gap-3 text-sm">
        <Link
          href={`/executive/reviews/${encodeURIComponent(runId)}`}
          className="font-medium text-teal-800 underline hover:text-teal-900 dark:text-teal-300 dark:hover:text-teal-200"
        >
          ← Risk review findings
        </Link>
        <span className="text-neutral-400 dark:text-neutral-600" aria-hidden>
          |
        </span>
        <Link
          href={`/reviews/${encodeURIComponent(runId)}/findings/${encodeURIComponent(decodedFindingId)}/inspect`}
          className="text-neutral-600 underline hover:text-neutral-800 dark:text-neutral-400 dark:hover:text-neutral-200"
        >
          Technical inspection (operator)
        </Link>
      </div>

      <header className="space-y-1">
        <p className="m-0 text-sm font-medium uppercase tracking-wide text-teal-800 dark:text-teal-300">Finding</p>
        <h1 className="m-0 text-2xl font-semibold text-neutral-900 dark:text-neutral-100">
          {findingDetailHeadingTitle(safePayload)}
        </h1>
        <p className="m-0 text-xs text-neutral-500 dark:text-neutral-400">
          Evidence-linked detail — use recommended actions below for next steps.
        </p>
      </header>

      <FindingInspectFindingBody
        runId={runId}
        decodedFindingId={decodedFindingId}
        payload={safePayload}
        variant="detail"
      />
    </div>
  );
}
