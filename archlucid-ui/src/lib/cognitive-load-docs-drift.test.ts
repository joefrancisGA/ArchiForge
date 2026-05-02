import { existsSync } from "node:fs";
import path from "node:path";

import { describe, expect, it } from "vitest";

import { HELP_TOPICS } from "@/lib/help-topics";

/** Vitest runs with cwd = `archlucid-ui` (`npm test` in package). */
const repoRoot = path.resolve(process.cwd(), "..");

describe("cognitive-load docs drift guard", () => {
  it("HELP_TOPICS doc paths resolve to files under the repo root", () => {
    for (const topic of HELP_TOPICS) {
      const relative = topic.docPath.replace(/^\/+/, "");
      const abs = path.join(repoRoot, ...relative.split("/"));

      expect(existsSync(abs), `missing doc for help topic ${topic.id}: ${topic.docPath}`).toBe(true);
    }
  });
});
