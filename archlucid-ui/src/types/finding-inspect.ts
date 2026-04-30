/** GET /v1/architecture/run/{runId}/findings/{findingId}/inspect */
export type FindingInspectEvidence = {
  artifactId: string | null;
  lineRange: string | null;
  excerpt: string | null;
};

export type FindingInspectPayload = {
  findingId: string;
  typedPayload: unknown;
  decisionRuleId: string | null;
  decisionRuleName: string | null;
  evidence: FindingInspectEvidence[];
  auditRowId: string | null;
  runId: string;
  manifestVersion: string | null;
  /** Inspect API fields when returned (FindingInspectResponse). */
  modelDeploymentName?: string | null;
  promptTemplateVersion?: string | null;
};
