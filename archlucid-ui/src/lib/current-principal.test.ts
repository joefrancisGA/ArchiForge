import { describe, expect, it } from "vitest";

import { normalizeAuthMeResponse } from "@/lib/current-principal";
import { enterpriseMutationCapabilityFromRank } from "@/lib/enterprise-mutation-capability";
import { AUTHORITY_RANK } from "@/lib/nav-authority";

/** Guards the `/me` → `CurrentPrincipal` seam used by `OperatorNavAuthorityProvider` (rank + enterprise surfacing flag). */
describe("normalizeAuthMeResponse", () => {
  it("maps Operator role to Execute rank and enables enterprise operator surfacing", () => {
    const principal = normalizeAuthMeResponse({
      name: "ops",
      claims: [{ type: "roles", value: "Operator" }],
    });

    expect(principal.provenance).toBe("auth-me");
    expect(principal.authorityRank).toBe(AUTHORITY_RANK.ExecuteAuthority);
    expect(principal.maxAuthority).toBe("ExecuteAuthority");
    expect(principal.primaryAppRole).toBe("Operator");
    expect(principal.hasEnterpriseOperatorSurfaces).toBe(true);
    expect(principal.roleClaimValues).toEqual(["Operator"]);
  });

  it("maps Reader to Read rank without enterprise operator surfacing", () => {
    const principal = normalizeAuthMeResponse({
      claims: [{ type: "http://schemas.microsoft.com/ws/2008/06/identity/claims/role", value: "Reader" }],
    });

    expect(principal.authorityRank).toBe(AUTHORITY_RANK.ReadAuthority);
    expect(principal.maxAuthority).toBe("ReadAuthority");
    expect(principal.hasEnterpriseOperatorSurfaces).toBe(false);
    expect(principal.primaryAppRole).toBe("Reader");
  });

  it("maps Auditor to Read rank but preserves Auditor as primaryAppRole", () => {
    const principal = normalizeAuthMeResponse({
      claims: [{ type: "roles", value: "Auditor" }],
    });

    expect(principal.authorityRank).toBe(AUTHORITY_RANK.ReadAuthority);
    expect(principal.primaryAppRole).toBe("Auditor");
    expect(principal.hasEnterpriseOperatorSurfaces).toBe(false);
  });

  it("picks Admin when multiple role claims are present", () => {
    const principal = normalizeAuthMeResponse({
      claims: [
        { type: "roles", value: "Reader" },
        { type: "roles", value: "Admin" },
      ],
    });

    expect(principal.authorityRank).toBe(AUTHORITY_RANK.AdminAuthority);
    expect(principal.maxAuthority).toBe("AdminAuthority");
    expect(principal.hasEnterpriseOperatorSurfaces).toBe(true);
    expect(principal.primaryAppRole).toBe("Admin");
  });

  /**
   * `hasEnterpriseOperatorSurfaces` and `useEnterpriseMutationCapability` must stay on the same Execute floor;
   * diverging formulas would mis-label principals or soft-enable writes inconsistently.
   */
  it("keeps hasEnterpriseOperatorSurfaces aligned with enterpriseMutationCapabilityFromRank for /me-shaped payloads", () => {
    const payloads = [
      { claims: [{ type: "roles", value: "Reader" }] },
      { claims: [{ type: "roles", value: "Auditor" }] },
      {
        claims: [
          {
            type: "https://login.microsoftonline.com/tenant/v2.0/claims/role",
            value: "Operator",
          },
        ],
      },
      { claims: [{ type: "roles", value: "Operator" }] },
      { claims: [{ type: "roles", value: "Admin" }] },
      { claims: [] as { type: string; value: string }[] },
    ];

    for (const body of payloads) {
      const principal = normalizeAuthMeResponse(body);

      expect(principal.hasEnterpriseOperatorSurfaces).toBe(
        enterpriseMutationCapabilityFromRank(principal.authorityRank),
      );
    }
  });
});
