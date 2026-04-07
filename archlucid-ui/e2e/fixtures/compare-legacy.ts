import type { RunComparison } from "@/types/authority";

import { FIXTURE_LEFT_RUN_ID, FIXTURE_RIGHT_RUN_ID } from "./ids";

/** Legacy flat compare payload that passes `coerceRunComparison`. */
export function fixtureLegacyRunComparison(): RunComparison {
  return {
    leftRunId: FIXTURE_LEFT_RUN_ID,
    rightRunId: FIXTURE_RIGHT_RUN_ID,
    runLevelDiffs: [
      {
        section: "topology",
        key: "serviceCount",
        diffKind: "Changed",
        beforeValue: "3",
        afterValue: "4",
        notes: "E2E fixture diff row.",
      },
    ],
    manifestComparison: {
      leftManifestId: "manifest-left-fixture",
      rightManifestId: "manifest-right-fixture",
      leftManifestHash: "sha256:left",
      rightManifestHash: "sha256:right",
      addedCount: 1,
      removedCount: 0,
      changedCount: 1,
      diffs: [
        {
          section: "decisions",
          key: "decision-1",
          diffKind: "Changed",
          beforeValue: "A",
          afterValue: "B",
        },
      ],
    },
    runLevelDiffCount: 1,
    hasManifestComparison: true,
  };
}
