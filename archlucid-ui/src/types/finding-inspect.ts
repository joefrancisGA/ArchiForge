/** GET /v1/findings/{findingId}/inspect */
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
};
