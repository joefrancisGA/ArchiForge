/** A policy pack containing compliance rules, alert rules, and advisory defaults. */
export type PolicyPack = {
  policyPackId: string;
  tenantId: string;
  workspaceId: string;
  projectId: string;
  name: string;
  description: string;
  packType: string;
  status: string;
  createdUtc: string;
  activatedUtc?: string | null;
  currentVersion: string;
};

/** A published version of a policy pack with its content document. */
export type PolicyPackVersion = {
  policyPackVersionId: string;
  policyPackId: string;
  version: string;
  contentJson: string;
  createdUtc: string;
  isPublished: boolean;
};

/** Assignment of a policy pack version to a scope (project, workspace, or tenant). */
export type PolicyPackAssignment = {
  assignmentId: string;
  tenantId: string;
  workspaceId: string;
  projectId: string;
  policyPackId: string;
  policyPackVersion: string;
  isEnabled: boolean;
  scopeLevel: string;
  isPinned: boolean;
  assignedUtc: string;
};

/** A resolved (effective) policy pack with its content document JSON. */
export type ResolvedPolicyPack = {
  policyPackId: string;
  name: string;
  version: string;
  packType: string;
  contentJson: string;
};

/** The set of all effective (resolved) policy packs for the current scope. */
export type EffectivePolicyPackSet = {
  tenantId: string;
  workspaceId: string;
  projectId: string;
  packs: ResolvedPolicyPack[];
};

/** Merged content document from all effective policy packs (rules, defaults, metadata). */
export type PolicyPackContentDocument = {
  complianceRuleIds: string[];
  complianceRuleKeys: string[];
  alertRuleIds: string[];
  compositeAlertRuleIds: string[];
  advisoryDefaults: Record<string, string>;
  metadata: Record<string, string>;
};
