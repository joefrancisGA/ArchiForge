import { getFindingInspect } from "@/lib/api";
import type { ApiLoadFailureState } from "@/lib/api-load-failure";
import { toApiLoadFailure } from "@/lib/api-load-failure";
import type { FindingInspectPayload } from "@/types/finding-inspect";

import { FindingInspectView } from "../FindingInspectView";

/**
 * First-class "Why this finding?" inspector: persisted payload, rule linkage, evidence citations, and audit correlation.
 * ReadAuthority only; no writes. `useOperateCapability` applies when future write affordances are added.
 */
export default async function FindingInspectPage({
  params,
}: {
  params: Promise<{ runId: string; findingId: string }>;
}) {
  const { runId, findingId } = await params;
  const decodedFindingId = decodeURIComponent(findingId);

  let payload: FindingInspectPayload | null = null;
  let failure: ApiLoadFailureState | null = null;

  try {
    payload = await getFindingInspect(decodedFindingId);
  } catch (e) {
    failure = toApiLoadFailure(e);
  }

  return (
    <FindingInspectView
      runId={runId}
      decodedFindingId={decodedFindingId}
      payload={payload}
      failure={failure}
    />
  );
}
