import { describe, expect, it } from "vitest";

import type { DiffItem } from "@/types/authority";
import type { GoldenManifestComparison } from "@/types/comparison";

import { sortDiffItems, sortGoldenManifestComparison } from "./compare-display-sort";

describe("sortDiffItems", () => {
  it("orders by section, key, then diffKind", () => {
    const items: DiffItem[] = [
      { section: "b", key: "y", diffKind: "Changed" },
      { section: "a", key: "z", diffKind: "Added" },
      { section: "a", key: "z", diffKind: "Removed" },
      { section: "a", key: "a", diffKind: "Changed" },
    ];
    const sorted = sortDiffItems(items);

    expect(sorted.map((i) => `${i.section}:${i.key}:${i.diffKind}`)).toEqual([
      "a:a:Changed",
      "a:z:Added",
      "a:z:Removed",
      "b:y:Changed",
    ]);
  });
});

describe("sortGoldenManifestComparison", () => {
  it("sorts all sub-lists deterministically", () => {
    const golden: GoldenManifestComparison = {
      baseRunId: "b",
      targetRunId: "t",
      decisionChanges: [
        { decisionKey: "z", changeType: "X", baseValue: null, targetValue: null },
        { decisionKey: "a", changeType: "Y", baseValue: null, targetValue: null },
      ],
      requirementChanges: [
        { requirementName: "b", changeType: "Added" },
        { requirementName: "a", changeType: "Removed" },
      ],
      securityChanges: [
        { controlName: "b", baseStatus: "on", targetStatus: null },
        { controlName: "a", baseStatus: null, targetStatus: "off" },
      ],
      topologyChanges: [
        { resource: "vm2", changeType: "Modified" },
        { resource: "vm1", changeType: "Added" },
      ],
      costChanges: [
        { baseCost: 200, targetCost: 300 },
        { baseCost: 100, targetCost: 150 },
      ],
      summaryHighlights: ["zebra", "apple"],
    };

    const sorted = sortGoldenManifestComparison(golden);

    expect(sorted.decisionChanges.map((d) => d.decisionKey)).toEqual(["a", "z"]);
    expect(sorted.requirementChanges.map((r) => r.requirementName)).toEqual(["a", "b"]);
    expect(sorted.securityChanges.map((s) => s.controlName)).toEqual(["a", "b"]);
    expect(sorted.topologyChanges.map((t) => t.resource)).toEqual(["vm1", "vm2"]);
    expect(sorted.costChanges[0].baseCost).toBe(100);
    expect(sorted.summaryHighlights).toEqual(["apple", "zebra"]);
  });
});
