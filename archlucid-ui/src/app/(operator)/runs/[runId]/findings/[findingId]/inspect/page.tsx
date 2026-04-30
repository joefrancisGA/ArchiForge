import { notFound } from "next/navigation";

import { getFindingInspect } from "@/lib/api";
import type { ApiLoadFailureState } from "@/lib/api-load-failure";
import { isApiNotFoundFailure, toApiLoadFailure } from "@/lib/api-load-failure";
import { isInvalidDynamicRouteToken, isInvalidGuidOrSlugRouteToken } from "@/lib/route-dynamic-param";
import { tryLoadRunExecutionFootnote } from "@/lib/try-load-run-execution-footnote";
import type { FindingInspectPayload } from "@/types/finding-inspect";

import { FindingInspectView } from "../FindingInspectView";

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

    if (isApiNotFoundFailure(failure)) {
      notFound();
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
