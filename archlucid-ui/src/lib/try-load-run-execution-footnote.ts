import type { OperatorEvidenceLimitsExecutionProps } from "@/components/OperatorEvidenceLimitsFooter";

import { getRunDetail } from "@/lib/api";
import { coerceRunDetail } from "@/lib/operator-response-guards";

/** Loads persisted run execution flags for evidence footers (best-effort; no throw). */
export async function tryLoadRunExecutionFootnote(
  runId: string,
): Promise<OperatorEvidenceLimitsExecutionProps | null> {
  try {
    const detailEnvelope = await getRunDetail(runId.trim());
    const coercedDetail = coerceRunDetail(detailEnvelope.data);

    if (!coercedDetail.ok) {
      return null;
    }

    return {
      realModeFellBackToSimulator: coercedDetail.value.run.realModeFellBackToSimulator,
      pilotAoaiDeploymentSnapshot: coercedDetail.value.run.pilotAoaiDeploymentSnapshot ?? null,
    };
  } catch {
    return null;
  }
}
