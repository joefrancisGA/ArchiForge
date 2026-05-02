import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

const BACKUP_ENV = process.env;

describe("buyer-safe-review-navigation", () => {
  beforeEach(() => {
    vi.resetModules();
  });

  afterEach(() => {
    process.env = { ...BACKUP_ENV };
    vi.restoreAllMocks();
  });

  it("prefers authenticated manifest link for curated demo IDs when DEMO_MODE buyer chrome is enabled", async () => {
    process.env = { ...BACKUP_ENV, NEXT_PUBLIC_DEMO_MODE: "true", NEXT_PUBLIC_DEMO_STATIC_OPERATOR: "false" };

    const mod = await import("./buyer-safe-review-navigation");

    expect(mod.isBuyerSafePrimaryReviewNavigationPreferred("claims-intake-modernization")).toBe(true);
    expect(mod.isBuyerSafePrimaryReviewNavigationPreferred("claims-intake-run-v1")).toBe(true);

    const link = mod.getBuyerSafeReviewsTableLink("claims-intake-modernization");

    expect(link.label).toBe("Review package");
    expect(link.href).toContain("/manifests/");
  });

  it("uses curated manifest link for static spine IDs even when buyer chrome env is off", async () => {
    process.env = { ...BACKUP_ENV, NEXT_PUBLIC_DEMO_MODE: "false", NEXT_PUBLIC_DEMO_STATIC_OPERATOR: "false" };

    const mod = await import("./buyer-safe-review-navigation");

    expect(mod.isBuyerSafePrimaryReviewNavigationPreferred("claims-intake-modernization")).toBe(false);

    const link = mod.getBuyerSafeReviewsTableLink("claims-intake-modernization");

    expect(link.label).toBe("Review package");
    expect(link.href).toContain("/manifests/");
  });

  it("canonicalizes workspace href for alias demo IDs", async () => {
    process.env = { ...BACKUP_ENV, NEXT_PUBLIC_DEMO_MODE: "false", NEXT_PUBLIC_DEMO_STATIC_OPERATOR: "false" };

    const mod = await import("./buyer-safe-review-navigation");

    expect(mod.getCanonicalReviewWorkspaceHref("claims-intake-modernization-run")).toBe(
      "/reviews/claims-intake-modernization",
    );
  });

  it("expose stable manifest slug for manifests route", async () => {
    process.env = { ...BACKUP_ENV, NEXT_PUBLIC_DEMO_MODE: "false", NEXT_PUBLIC_DEMO_STATIC_OPERATOR: "false" };

    const mod = await import("./buyer-safe-review-navigation");

    expect(mod.getShowcaseManifestHref()).toContain("a1c2e3f4-a5b6-7890-abcd-ef1234567890");
  });
});
