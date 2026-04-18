import { renderHook } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

import { AUTHORITY_RANK } from "@/lib/nav-authority";

/** Mutable rank injected into `useNavCallerAuthorityRank` — literals only (hoisted factory runs before imports resolve). */
const navCallerAuthorityRank = vi.hoisted(() => ({ current: 1 }));

vi.mock("@/components/OperatorNavAuthorityProvider", () => ({
  useNavCallerAuthorityRank: (): number => navCallerAuthorityRank.current,
}));

import { useEnterpriseMutationCapability } from "@/hooks/use-enterprise-mutation-capability";

describe("useEnterpriseMutationCapability", () => {
  beforeEach(() => {
    navCallerAuthorityRank.current = AUTHORITY_RANK.ReadAuthority;
  });

  afterEach(() => {
    navCallerAuthorityRank.current = AUTHORITY_RANK.ReadAuthority;
  });

  it("is false when nav rank is Read (same threshold as Enterprise soft-disable)", () => {
    navCallerAuthorityRank.current = AUTHORITY_RANK.ReadAuthority;

    const { result } = renderHook(() => useEnterpriseMutationCapability());

    expect(result.current).toBe(false);
  });

  it("is true when nav rank is Execute or Admin", () => {
    navCallerAuthorityRank.current = AUTHORITY_RANK.ExecuteAuthority;
    expect(renderHook(() => useEnterpriseMutationCapability()).result.current).toBe(true);

    navCallerAuthorityRank.current = AUTHORITY_RANK.AdminAuthority;
    expect(renderHook(() => useEnterpriseMutationCapability()).result.current).toBe(true);
  });

  /** Aligns with `LayerHeader` Enterprise rank cue: sub-Read numeric rank stays non-mutating (conservative shell). */
  it("is false when nav rank is below Read policy floor (e.g. unset 0)", () => {
    navCallerAuthorityRank.current = 0;

    const { result } = renderHook(() => useEnterpriseMutationCapability());

    expect(result.current).toBe(false);
  });
});
