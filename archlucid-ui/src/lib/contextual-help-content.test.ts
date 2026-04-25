import { describe, expect, it } from "vitest";

import { contextualHelpByKey, toDocsBlobUrl } from "./contextual-help-content";

/**
 * Every `helpKey` used by a production <ContextualHelp />. Grep: `helpKey=`.
 * Excludes `*.test.tsx` / story examples so the index is the source of body copy in the shell.
 * When you add a new in-app help, append here so the index cannot drift.
 */
const contextualHelpComponentKeys = [
  "new-run-wizard",
  "run-pipeline-status",
  "commit-manifest",
  "manifest-review",
  "governance-gate",
  "alerts-inbox",
  "governance-dashboard",
  "compare-runs",
  "replay-run",
  "architecture-graph",
  "audit-log",
  "policy-packs",
  "advisory-hub",
  "semantic-search",
  "ask-archlucid",
  "operator-scope-switcher",
  "tenant-settings-page",
  "admin-users-page",
  "system-health",
] as const;

describe("contextualHelpByKey", () => {
  it("lists no duplicate ContextualHelp keys (catch copy-paste errors)", () => {
    const keyList: readonly string[] = [...contextualHelpComponentKeys];

    expect(new Set(keyList).size, "duplicate in contextualHelpComponentKeys").toBe(keyList.length);
  });

  it("defines every helpKey used by ContextualHelp in production", () => {
    for (const key of contextualHelpComponentKeys) {
      const entry = contextualHelpByKey[key];
      expect(entry, key).toBeDefined();
    }
  });

  it("defines all contextual help keys with non-empty text under 200 chars", () => {
    for (const key of contextualHelpComponentKeys) {
      const entry = contextualHelpByKey[key];
      expect(entry, key).toBeDefined();
      expect(entry.text.length, key).toBeGreaterThan(0);
      expect(entry.text.length, key).toBeLessThan(200);
    }
  });

  it("uses /-prefixed learn more paths when present", () => {
    for (const key of contextualHelpComponentKeys) {
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
