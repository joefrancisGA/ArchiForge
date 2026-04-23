"use client";

import { useNavCallerAuthorityRank } from "@/components/OperatorNavAuthorityProvider";
import { operateCapabilityFromRank } from "@/lib/operate-capability";

/**
 * **Capability** surface (UI shaping): Execute+ mutation soft-enable, aligned with `nav-config` / `nav-authority`.
 * API `[Authorize]` remains authoritative.
 */
export function useOperateCapability(): boolean {
  const rank = useNavCallerAuthorityRank();

  return operateCapabilityFromRank(rank);
}
