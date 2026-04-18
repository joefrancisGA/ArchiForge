/**
 * `useNavCallerAuthorityRank` is the conservative rank fed into nav filtering and Enterprise mutation hooks while JWT
 * `/me` is in flight. A regression here causes Execute-tier destinations or write affordances to flash before claims
 * resolve (see `OperatorNavAuthorityProvider` implementation comments).
 */
import { render, screen, waitFor } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";

import { normalizeAuthMeResponse, type CurrentPrincipal } from "@/lib/current-principal";
import { AUTHORITY_RANK } from "@/lib/nav-authority";

const pathnameRef = vi.hoisted(() => ({ current: "/" }));
const loadCurrentPrincipalMock = vi.hoisted(() => vi.fn());
let fetchCount = 0;

vi.mock("next/navigation", () => ({
  usePathname: (): string => pathnameRef.current,
}));

vi.mock("@/lib/oidc/config", () => ({
  isJwtAuthMode: (): boolean => true,
}));

vi.mock("@/lib/oidc/session", () => ({
  isLikelySignedIn: (): boolean => true,
}));

vi.mock("@/lib/current-principal", async (importOriginal) => {
  const actual = await importOriginal<typeof import("@/lib/current-principal")>();

  return {
    ...actual,
    loadCurrentPrincipal: (): Promise<CurrentPrincipal> => loadCurrentPrincipalMock(),
  };
});

import {
  OperatorNavAuthorityProvider,
  useNavCallerAuthorityRank,
} from "@/components/OperatorNavAuthorityProvider";

function RankProbe() {
  const rank = useNavCallerAuthorityRank();

  return <span data-testid="nav-caller-rank">{rank}</span>;
}

describe("OperatorNavAuthorityProvider", () => {
  beforeEach(() => {
    fetchCount = 0;
    pathnameRef.current = "/";
    loadCurrentPrincipalMock.mockReset();
    loadCurrentPrincipalMock.mockImplementation(async () => {
      fetchCount += 1;

      if (fetchCount === 1) {
        return normalizeAuthMeResponse({ claims: [{ type: "roles", value: "Operator" }] });
      }

      return new Promise<CurrentPrincipal>(() => {
        /* hang: simulates slow /me during route change while prior rank was already Execute */
      });
    });
  });

  it("useNavCallerAuthorityRank stays Read during JWT /me refetch after rank had reached Execute", async () => {
    const { rerender } = render(
      <OperatorNavAuthorityProvider>
        <RankProbe />
      </OperatorNavAuthorityProvider>,
    );

    await waitFor(() => {
      expect(screen.getByTestId("nav-caller-rank")).toHaveTextContent(String(AUTHORITY_RANK.ExecuteAuthority));
    });

    pathnameRef.current = "/compare";

    rerender(
      <OperatorNavAuthorityProvider>
        <RankProbe />
      </OperatorNavAuthorityProvider>,
    );

    await waitFor(() => {
      expect(screen.getByTestId("nav-caller-rank")).toHaveTextContent(String(AUTHORITY_RANK.ReadAuthority));
    });
  });

  it("resolves to Execute rank after /me returns Operator", async () => {
    loadCurrentPrincipalMock.mockImplementation(async () =>
      normalizeAuthMeResponse({ claims: [{ type: "roles", value: "Operator" }] }),
    );

    render(
      <OperatorNavAuthorityProvider>
        <RankProbe />
      </OperatorNavAuthorityProvider>,
    );

    await waitFor(() => {
      expect(screen.getByTestId("nav-caller-rank")).toHaveTextContent(String(AUTHORITY_RANK.ExecuteAuthority));
    });
  });

  it("falls back to Read rank when /me rejects", async () => {
    loadCurrentPrincipalMock.mockRejectedValueOnce(new Error("network"));

    render(
      <OperatorNavAuthorityProvider>
        <RankProbe />
      </OperatorNavAuthorityProvider>,
    );

    await waitFor(() => {
      expect(screen.getByTestId("nav-caller-rank")).toHaveTextContent(String(AUTHORITY_RANK.ReadAuthority));
    });
  });
});
