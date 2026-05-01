import { notFound } from "next/navigation";

import { getFindingInspect } from "@/lib/api";
import type { ApiLoadFailureState } from "@/lib/api-load-failure";
import { isApiNotFoundFailure, toApiLoadFailure } from "@/lib/api-load-failure";
import { tryStaticDemoFindingInspect } from "@/lib/operator-static-demo";
import { isInvalidDynamicRouteToken, isInvalidGuidOrSlugRouteToken } from "@/lib/route-dynamic-param";
import { tryLoadRunExecutionFootnote } from "@/lib/try-load-run-execution-footnote";
import type { FindingInspectPayload } from "@/types/finding-inspect";

import { FindingInspectView, sameAuthorityRunId } from "../FindingInspectView";

/**
 * First-class technical inspection: persisted payload, rule linkage, evidence citations, and audit correlation.
 * ReadAuthority only; no writes. `useOperateCapability` applies when future write affordances are added.
 */
export default async function FindingInspectPage({
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
    payload = await getFindingInspect(runId, decodedFindingId);
  } catch (e) {
    failure = toApiLoadFailure(e);

    const staticInspect = tryStaticDemoFindingInspect(runId, decodedFindingId);

    if (staticInspect !== null) {
      payload = staticInspect;
      failure = null;
    } else if (isApiNotFoundFailure(failure)) {
      notFound();
    }
  }

  if (payload !== null) {
    const staticInspect = tryStaticDemoFindingInspect(runId, decodedFindingId);

    if (staticInspect !== null && !sameAuthorityRunId(payload.runId, runId)) {
      payload = staticInspect;
      failure = null;
    }
  }

  const runExecutionFootnote = await tryLoadRunExecutionFootnote(runId);

  return (
    <FindingInspectView
      runId={runId}
      decodedFindingId={decodedFindingId}
      payload={payload}
      failure={failure}
      runExecutionFootnote={runExecutionFootnote}
    />
  );
}
