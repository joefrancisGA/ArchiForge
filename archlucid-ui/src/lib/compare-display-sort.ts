import type { DiffItem } from "@/types/authority";
import type {
  CostDelta,
  DecisionDelta,
  GoldenManifestComparison,
  RequirementDelta,
  SecurityDelta,
  TopologyDelta,
} from "@/types/comparison";

/**
 * Sorts flat diff rows for stable table order (section → key → kind).
 * Same API payload always renders the same row order.
 */
export function sortDiffItems(items: DiffItem[]): DiffItem[] {
  return [...items].sort((a, b) => {
    const section = a.section.localeCompare(b.section, "en");
    if (section !== 0) {
      return section;
    }

    const key = a.key.localeCompare(b.key, "en");
    if (key !== 0) {
      return key;
    }

    return a.diffKind.localeCompare(b.diffKind, "en");
  });
}

function compareNullableString(a: string | null | undefined, b: string | null | undefined): number {
  const as = a ?? "";
  const bs = b ?? "";

  return as.localeCompare(bs, "en");
}

/**
 * Returns a shallow copy of the golden comparison with every list sorted for deterministic tables.
 */
export function sortGoldenManifestComparison(
  golden: GoldenManifestComparison,
): GoldenManifestComparison {
  const decisionChanges: DecisionDelta[] = [...golden.decisionChanges].sort((a, b) => {
    const keyCmp = a.decisionKey.localeCompare(b.decisionKey, "en");
    if (keyCmp !== 0) {
      return keyCmp;
    }

    return a.changeType.localeCompare(b.changeType, "en");
  });

  const requirementChanges: RequirementDelta[] = [...golden.requirementChanges].sort((a, b) => {
    const nameCmp = a.requirementName.localeCompare(b.requirementName, "en");
    if (nameCmp !== 0) {
      return nameCmp;
    }

    return a.changeType.localeCompare(b.changeType, "en");
  });

  const securityChanges: SecurityDelta[] = [...golden.securityChanges].sort((a, b) => {
    const controlCmp = a.controlName.localeCompare(b.controlName, "en");
    if (controlCmp !== 0) {
      return controlCmp;
    }

    return compareNullableString(a.baseStatus, b.baseStatus);
  });

  const topologyChanges: TopologyDelta[] = [...golden.topologyChanges].sort((a, b) => {
    const resourceCmp = a.resource.localeCompare(b.resource, "en");
    if (resourceCmp !== 0) {
      return resourceCmp;
    }

    return a.changeType.localeCompare(b.changeType, "en");
  });

  const costChanges: CostDelta[] = [...golden.costChanges].sort((a, b) => {
    const baseA = a.baseCost ?? Number.NEGATIVE_INFINITY;
    const baseB = b.baseCost ?? Number.NEGATIVE_INFINITY;
    if (baseA !== baseB) {
      return baseA - baseB;
    }

    const targetA = a.targetCost ?? Number.NEGATIVE_INFINITY;
    const targetB = b.targetCost ?? Number.NEGATIVE_INFINITY;

    return targetA - targetB;
  });

  const summaryHighlights: string[] = [...golden.summaryHighlights].sort((x, y) =>
    x.localeCompare(y, "en"),
  );

  return {
    ...golden,
    decisionChanges,
    requirementChanges,
    securityChanges,
    topologyChanges,
    costChanges,
    summaryHighlights,
  };
}
