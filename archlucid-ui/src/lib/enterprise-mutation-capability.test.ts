import { describe, expect, it } from "vitest";

import { AUTHORITY_RANK } from "@/lib/nav-authority";
import { enterpriseMutationCapabilityFromRank } from "@/lib/enterprise-mutation-capability";

describe("enterpriseMutationCapabilityFromRank", () => {
  it("returns false for read rank", () => {
    expect(enterpriseMutationCapabilityFromRank(AUTHORITY_RANK.ReadAuthority)).toBe(false);
  });

  it("returns true for execute and admin ranks", () => {
    expect(enterpriseMutationCapabilityFromRank(AUTHORITY_RANK.ExecuteAuthority)).toBe(true);
    expect(enterpriseMutationCapabilityFromRank(AUTHORITY_RANK.AdminAuthority)).toBe(true);
  });

  /** Matches conservative nav / LayerHeader branch: unset sub-Read rank must not soft-enable mutations. */
  it("returns false for numeric rank below Read policy floor", () => {
    expect(enterpriseMutationCapabilityFromRank(0)).toBe(false);
  });
});
