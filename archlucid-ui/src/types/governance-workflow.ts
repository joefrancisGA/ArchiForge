/** Approval workflow row (v1/governance); mirrors ArchLucid.Contracts.Governance.GovernanceApprovalRequest JSON. */
export type GovernanceApprovalRequest = {
  approvalRequestId: string;
  runId: string;
  manifestVersion: string;
  sourceEnvironment: string;
  targetEnvironment: string;
  status: string;
  requestedBy: string;
  reviewedBy: string | null;
  requestComment: string | null;
  reviewComment: string | null;
  requestedUtc: string;
  reviewedUtc: string | null;
  /** Optional — present when SLA is configured for the request. */
  slaDeadlineUtc?: string | null;
  slaBreachNotifiedUtc?: string | null;
};

/**
 * Promotion audit row; wire uses promotionRecordId (C# PromotionRecordId).
 * Prompt alias "promotionId" refers to this identifier.
 */
export type GovernancePromotionRecord = {
  promotionRecordId: string;
  runId: string;
  manifestVersion: string;
  sourceEnvironment: string;
  targetEnvironment: string;
  promotedBy: string;
  approvalRequestId: string | null;
  notes: string | null;
  promotedUtc: string;
};

/** Environment activation row; API does not return activatedBy (actor comes from server context). */
export type GovernanceEnvironmentActivation = {
  activationId: string;
  runId: string;
  manifestVersion: string;
  environment: string;
  isActive: boolean;
  activatedUtc: string;
};
