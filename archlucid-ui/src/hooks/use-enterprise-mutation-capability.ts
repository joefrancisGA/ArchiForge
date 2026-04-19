"use client";

import { useNavCallerAuthorityRank } from "@/components/OperatorNavAuthorityProvider";
import { enterpriseMutationCapabilityFromRank } from "@/lib/enterprise-mutation-capability";

/**
 * Whether the resolved shell principal should see **enabled** Execute-tier mutation affordances on **Enterprise Controls**
 * (and related) routes. Uses the same rank source as nav filtering (`useNavCallerAuthorityRank`) so **sidebar omission**
 * and **button disabled** state stay aligned with **docs/PRODUCT_PACKAGING.md**. **UI shaping only** — API `[Authorize]`
 * returns 401/403.
 *
 * @see `use-enterprise-mutation-capability.test.tsx`, `authority-seam-regression.test.ts`, `authority-execute-floor-regression.test.ts`,
 *   `authority-shaped-ui-regression.test.ts` (catalog **`ExecuteAuthority`** nav vs this hook’s **Execute** floor).
 */
export function useEnterpriseMutationCapability(): boolean {
  const rank = useNavCallerAuthorityRank();

  return enterpriseMutationCapabilityFromRank(rank);
}
