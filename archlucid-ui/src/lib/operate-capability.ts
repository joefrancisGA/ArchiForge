import { enterpriseMutationCapabilityFromRank } from "@/lib/enterprise-mutation-capability";

/**
 * Whether the caller’s numeric rank should **soft-enable** Execute-class POST/toggle controls on **Operate** routes.
 * Delegates to the legacy helper so rank thresholds stay single-sourced until the deprecated symbol is removed.
 */
export function operateCapabilityFromRank(rank: number): boolean {
  return enterpriseMutationCapabilityFromRank(rank);
}
