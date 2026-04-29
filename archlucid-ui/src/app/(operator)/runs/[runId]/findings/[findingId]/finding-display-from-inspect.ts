import type { FindingInspectPayload } from "@/types/finding-inspect";

/**
 * Reads common optional typed-payload shapes (demo + rule engines) without assuming a single schema.
 */
export function typedPayloadLookupString(payload: FindingInspectPayload, key: string): string | null {
  const value: unknown = payload.typedPayload;

  if (value === null || value === undefined || typeof value !== "object") {
    return null;
  }

  const record = value as Record<string, unknown>;

  if (!(key in record)) {
    return null;
  }

  const extracted = record[key];

  if (typeof extracted !== "string" || extracted.trim().length === 0) {
    return null;
  }

  return extracted.trim();
}

/** Human-visible labels derived from persisted inspect payload — primary narrative before raw JSON/explanations. */
export function findingInspectPrimaryLabels(payload: FindingInspectPayload): {
  severityLabel: string | null;
  categoryLabel: string | null;
  impactedAreaLabel: string | null;
  recommendedAction: string | null;
} {
  return {
    severityLabel:
      typedPayloadLookupString(payload, "severity") ?? typedPayloadLookupString(payload, "Severity"),
    categoryLabel:
      typedPayloadLookupString(payload, "category") ??
      typedPayloadLookupString(payload, "Category") ??
      payload.decisionRuleName ??
      payload.decisionRuleId ??
      null,
    impactedAreaLabel:
      typedPayloadLookupString(payload, "impactedArea") ??
      typedPayloadLookupString(payload, "ImpactedArea") ??
      typedPayloadLookupString(payload, "impactArea"),
    recommendedAction:
      typedPayloadLookupString(payload, "recommendedAction") ??
      typedPayloadLookupString(payload, "RecommendedAction") ??
      typedPayloadLookupString(payload, "remediationSuggestion"),
  };
}
