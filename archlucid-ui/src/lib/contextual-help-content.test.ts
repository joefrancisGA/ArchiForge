import { describe, expect, it } from "vitest";

import { contextualHelpByKey, toDocsBlobUrl } from "./contextual-help-content";

const requiredKeys = [
  "new-run-wizard",
  "run-pipeline-status",
  "commit-manifest",
  "manifest-review",
  "governance-gate",
] as const;

describe("contextualHelpByKey", () => {
  it("defines all five core pilot help keys with non-empty text under 200 chars", () => {
    for (const key of requiredKeys) {
      const entry = contextualHelpByKey[key];
      expect(entry, key).toBeDefined();
      expect(entry.text.length, key).toBeGreaterThan(0);
      expect(entry.text.length, key).toBeLessThan(200);
    }
  });

  it("uses /-prefixed learn more paths when present", () => {
    for (const key of requiredKeys) {
      const u = contextualHelpByKey[key].learnMoreUrl;

      if (u == null) {
        continue;
      }

      expect(u.startsWith("/"), key).toBe(true);
    }
  });

  it("toDocsBlobUrl builds a GitHub blob URL for default branch", () => {
    const url = toDocsBlobUrl("/docs/CORE_PILOT.md#x");

    expect(url).toMatch(/^https:\/\/github\.com\//);
    expect(url).toContain("docs/CORE_PILOT.md#x");
  });
});
