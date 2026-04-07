import type { PolicyPackContentDocument } from "@/types/policy-packs";

/** A candidate policy pack entry competing in governance resolution (with precedence rank). */
export type GovernanceResolutionCandidate = {
  policyPackId: string;
  policyPackName: string;
  version: string;
  scopeLevel: string;
  precedenceRank: number;
  wasSelected: boolean;
  valueJson: string;
  assignmentId: string;
  assignedUtc: string;
};

/** A single governance merge decision: which policy pack won for a given item. */
export type GovernanceResolutionDecision = {
  itemType: string;
  itemKey: string;
  winningPolicyPackId: string;
  winningPolicyPackName: string;
  winningVersion: string;
  winningScopeLevel: string;
  resolutionReason: string;
  candidates: GovernanceResolutionCandidate[];
};

/** A conflict detected during governance resolution (overlapping or contradictory policy packs). */
export type GovernanceConflictRecord = {
  itemType: string;
  itemKey: string;
  conflictType: string;
  description: string;
  candidates: GovernanceResolutionCandidate[];
};

/** Full governance resolution result: effective content, merge decisions, and any conflicts. */
export type EffectiveGovernanceResolutionResult = {
  tenantId: string;
  workspaceId: string;
  projectId: string;
  effectiveContent: PolicyPackContentDocument;
  decisions: GovernanceResolutionDecision[];
  conflicts: GovernanceConflictRecord[];
  notes: string[];
};
