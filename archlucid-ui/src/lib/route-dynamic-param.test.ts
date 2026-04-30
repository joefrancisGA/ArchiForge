import { describe, expect, it } from "vitest";

import {
  isCanonicalUuidToken,
  isInvalidDynamicRouteToken,
  isInvalidGuidOrSlugRouteToken,
  isInvalidManifestRouteId,
} from "./route-dynamic-param";

describe("isInvalidDynamicRouteToken", () => {
  it("treats null and undefined as invalid", () => {
    expect(isInvalidDynamicRouteToken(undefined)).toBe(true);
    expect(isInvalidDynamicRouteToken(null)).toBe(true);
  });

  it("rejects empty and leaked JS literals", () => {
    expect(isInvalidDynamicRouteToken("")).toBe(true);
    expect(isInvalidDynamicRouteToken("   ")).toBe(true);
    expect(isInvalidDynamicRouteToken("undefined")).toBe(true);
    expect(isInvalidDynamicRouteToken("UNDEFINED")).toBe(true);
    expect(isInvalidDynamicRouteToken("null")).toBe(true);
    expect(isInvalidDynamicRouteToken("none")).toBe(true);
  });

  it("accepts normal opaque ids", () => {
    expect(isInvalidDynamicRouteToken("pack-1")).toBe(false);
    expect(isInvalidDynamicRouteToken("e2e-policy-pack-001")).toBe(false);
    expect(isInvalidDynamicRouteToken("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee")).toBe(false);
  });

  it("rejects placeholder leak tokens", () => {
    expect(isInvalidDynamicRouteToken("fixture")).toBe(true);
    expect(isInvalidDynamicRouteToken("mock")).toBe(true);
    expect(isInvalidDynamicRouteToken("r1")).toBe(true);
    expect(isInvalidDynamicRouteToken("localhost")).toBe(true);
    expect(isInvalidDynamicRouteToken("Execute+")).toBe(true);
  });
});

describe("isCanonicalUuidToken", () => {
  it("accepts lowercase and uppercase hex uuids", () => {
    expect(isCanonicalUuidToken("a1c2e3f4-a5b6-7890-abcd-ef1234567890")).toBe(true);
    expect(isCanonicalUuidToken("A1C2E3F4-A5B6-7890-ABCD-EF1234567890")).toBe(true);
  });

  it("rejects non-hex in uuid slots", () => {
    expect(isCanonicalUuidToken("gggggggg-gggg-gggg-gggg-gggggggggggg")).toBe(false);
  });
});

describe("isInvalidGuidOrSlugRouteToken", () => {
  it("rejects invalid dynamic tokens", () => {
    expect(isInvalidGuidOrSlugRouteToken("undefined")).toBe(true);
  });

  it("allows slug run ids and showcase ids", () => {
    expect(isInvalidGuidOrSlugRouteToken("claims-intake-modernization")).toBe(false);
    expect(isInvalidGuidOrSlugRouteToken("e2e-fixture-run-001")).toBe(false);
    expect(isInvalidGuidOrSlugRouteToken("phi-minimization-risk")).toBe(false);
  });

  it("allows canonical uuids", () => {
    expect(isInvalidGuidOrSlugRouteToken("a1c2e3f4-a5b6-7890-abcd-ef1234567890")).toBe(false);
  });

  it("rejects 8-4-4-4-12 shape with invalid hex", () => {
    expect(isInvalidGuidOrSlugRouteToken("gggggggg-gggg-gggg-gggg-gggggggggggg")).toBe(true);
  });

  it("rejects single-segment placeholder slugs for runs", () => {
    expect(isInvalidGuidOrSlugRouteToken("fixture")).toBe(true);
    expect(isInvalidGuidOrSlugRouteToken("mock")).toBe(true);
    expect(isInvalidGuidOrSlugRouteToken("r1")).toBe(true);
  });
});

describe("isInvalidManifestRouteId", () => {
  it("requires a canonical uuid", () => {
    expect(isInvalidManifestRouteId("a1c2e3f4-a5b6-7890-abcd-ef1234567890")).toBe(false);
    expect(isInvalidManifestRouteId("claims-intake-modernization")).toBe(true);
    expect(isInvalidManifestRouteId("undefined")).toBe(true);
  });
});
