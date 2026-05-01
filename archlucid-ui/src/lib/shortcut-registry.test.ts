import { describe, expect, it } from "vitest";

import {
  ALERTS_PAGE_SHORTCUTS,
  SHORTCUTS,
  findShortcutByKey,
  registryKeyToAriaKeyShortcuts,
} from "./shortcut-registry";

describe("shortcut-registry", () => {
  it("maps registry combo keys to aria-keyshortcuts form", () => {
    expect(registryKeyToAriaKeyShortcuts("alt+n")).toBe("Alt+N");
    expect(registryKeyToAriaKeyShortcuts("alt+r")).toBe("Alt+R");
    expect(registryKeyToAriaKeyShortcuts("shift+?")).toBe("Shift+?");
    expect(registryKeyToAriaKeyShortcuts("alt+1")).toBe("Alt+1");
    expect(registryKeyToAriaKeyShortcuts("alt+y")).toBe("Alt+Y");
  });

  it("has non-empty key, label, and description on every entry", () => {
    for (const entry of SHORTCUTS) {
      expect(entry.key.trim().length).toBeGreaterThan(0);
      expect(entry.label.trim().length).toBeGreaterThan(0);
      expect(entry.description.trim().length).toBeGreaterThan(0);
    }
  });

  it("has no duplicate key combos (case-insensitive)", () => {
    const seen = new Set<string>();

    for (const entry of SHORTCUTS) {
      const normalized = entry.key.toLowerCase().trim();
      expect(seen.has(normalized)).toBe(false);
      seen.add(normalized);
    }
  });

  it("has valid page shortcut entries with no duplicate keys", () => {
    const seen = new Set<string>();

    for (const entry of ALERTS_PAGE_SHORTCUTS) {
      expect(entry.key.trim().length).toBeGreaterThan(0);
      expect(entry.label.trim().length).toBeGreaterThan(0);
      expect(entry.description.trim().length).toBeGreaterThan(0);

      const normalized = entry.key.toLowerCase().trim();
      expect(seen.has(normalized)).toBe(false);
      seen.add(normalized);
    }
  });

  it("does not overlap global shortcut combos with alerts page combos", () => {
    const globalKeys = new Set(SHORTCUTS.map((e) => e.key.toLowerCase().trim()));

    for (const entry of ALERTS_PAGE_SHORTCUTS) {
      expect(globalKeys.has(entry.key.toLowerCase().trim())).toBe(false);
    }
  });

  it("findShortcutByKey returns the matching entry regardless of casing", () => {
    const byLower = findShortcutByKey("alt+n");
    expect(byLower?.label).toBe("New request");

    const byMixed = findShortcutByKey("Alt+N");
    expect(byMixed?.route).toBe("/reviews/new");

    const help = findShortcutByKey("Shift+?");
    expect(help?.label).toBe("Help");

    expect(findShortcutByKey("not-a-real-combo")).toBeUndefined();
  });
});
