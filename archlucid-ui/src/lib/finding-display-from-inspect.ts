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

  statusLabel: string | null;

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

    statusLabel:

      typedPayloadLookupString(payload, "status") ??

      typedPayloadLookupString(payload, "Status") ??

      typedPayloadLookupString(payload, "findingStatus"),

  };

}



/** Title and description for work-item copy — common typed-payload shapes from finding engines. */

export function findingInspectNarrativeFields(payload: FindingInspectPayload): {

  title: string | null;

  description: string | null;

} {

  return {

    title:

      typedPayloadLookupString(payload, "title") ??

      typedPayloadLookupString(payload, "Title") ??

      typedPayloadLookupString(payload, "name") ??

      null,

    description:

      typedPayloadLookupString(payload, "description") ??

      typedPayloadLookupString(payload, "Description") ??

      typedPayloadLookupString(payload, "message") ??

      typedPayloadLookupString(payload, "Message") ??

      typedPayloadLookupString(payload, "detail") ??

      null,

  };

}

/**
 * Preferred page title for Finding detail — human narrative first, then rule context, generic last.
 */

export function findingDetailHeadingTitle(payload: FindingInspectPayload): string {

  const narrative = findingInspectNarrativeFields(payload);

  const titleCandidate = narrative.title?.trim();

  if (titleCandidate !== undefined && titleCandidate.length > 0)

    return titleCandidate;

  const ruleName = payload.decisionRuleName?.trim();

  if (ruleName !== undefined && ruleName.length > 0)

    return ruleName;

  const ruleId = payload.decisionRuleId?.trim();

  if (ruleId !== undefined && ruleId.length > 0)

    return ruleId;

  return "Finding detail";

}

/** Short user-facing primer under the title when structured description is unavailable. */

export function findingDetailLeadSentence(payload: FindingInspectPayload): string {

  const narrative = findingInspectNarrativeFields(payload);

  const description = narrative.description?.trim();

  if (description !== undefined && description.length > 0)

    return description;

  const labels = findingInspectPrimaryLabels(payload);

  const area = labels.impactedAreaLabel?.trim();

  if (area !== undefined && area.length > 0)

    return `Outcome focuses on ${area}. Review evidence and the recommended action before closing or escalating.`;

  return "Review the recommendations and cited evidence below before sign-off.";

}

