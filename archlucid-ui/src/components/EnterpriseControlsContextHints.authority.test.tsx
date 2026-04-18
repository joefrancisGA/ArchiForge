/**
 * Rank-gated Enterprise **context hints** (same `AUTHORITY_RANK.ExecuteAuthority` threshold as nav filtering and
 * `useEnterpriseMutationCapability`). Asserts presence/absence and canonical copy imports — not arbitrary UI strings.
 */
import { render, screen } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";

import {
  alertOperatorToolingOperatorRankLine,
  alertOperatorToolingReaderRankLine,
  enterpriseExecutePageHintReaderRank,
  enterpriseNavHintOperatorRank,
  enterpriseNavHintReaderRank,
} from "@/lib/enterprise-controls-context-copy";

/** Literal `1` — hoisted factory runs before `AUTHORITY_RANK` import is safe to reference. */
const navCallerAuthorityRank = vi.hoisted(() => ({ current: 1 }));

vi.mock("@/components/OperatorNavAuthorityProvider", () => ({
  useNavCallerAuthorityRank: (): number => navCallerAuthorityRank.current,
}));

import { AUTHORITY_RANK } from "@/lib/nav-authority";

import {
  AlertOperatorToolingRankCue,
  EnterpriseControlsExecutePageHint,
  EnterpriseControlsNavGroupHint,
  EnterpriseExecutePlusPageCue,
} from "./EnterpriseControlsContextHints";

describe("EnterpriseControlsContextHints authority shaping", () => {
  afterEach(() => {
    navCallerAuthorityRank.current = AUTHORITY_RANK.ReadAuthority;
  });

  describe("EnterpriseControlsExecutePageHint", () => {
    it("shows reader execute warning below Execute rank", () => {
      navCallerAuthorityRank.current = AUTHORITY_RANK.ReadAuthority;
      render(<EnterpriseControlsExecutePageHint />);

      expect(screen.getByRole("note")).toHaveTextContent(enterpriseExecutePageHintReaderRank);
    });

    it("renders nothing at Execute+ to avoid stacking with operator-plus cues", () => {
      navCallerAuthorityRank.current = AUTHORITY_RANK.ExecuteAuthority;
      const { container } = render(<EnterpriseControlsExecutePageHint />);

      expect(container.firstChild).toBeNull();
    });
  });

  describe("EnterpriseExecutePlusPageCue", () => {
    const operatorPlusMessage = "Operator-plus cue fixture";

    it("is hidden below Execute", () => {
      navCallerAuthorityRank.current = AUTHORITY_RANK.ReadAuthority;
      const { container } = render(<EnterpriseExecutePlusPageCue message={operatorPlusMessage} />);

      expect(container.firstChild).toBeNull();
    });

    it("shows the message at Execute+", () => {
      navCallerAuthorityRank.current = AUTHORITY_RANK.ExecuteAuthority;
      render(<EnterpriseExecutePlusPageCue message={operatorPlusMessage} />);

      expect(screen.getByRole("note")).toHaveTextContent(operatorPlusMessage);
    });
  });

  describe("EnterpriseControlsNavGroupHint", () => {
    it("uses reader nav copy below Execute", () => {
      navCallerAuthorityRank.current = AUTHORITY_RANK.ReadAuthority;
      render(<EnterpriseControlsNavGroupHint />);

      expect(screen.getByText(enterpriseNavHintReaderRank)).toBeInTheDocument();
    });

    it("uses operator nav copy at Execute+", () => {
      navCallerAuthorityRank.current = AUTHORITY_RANK.ExecuteAuthority;
      render(<EnterpriseControlsNavGroupHint />);

      expect(screen.getByText(enterpriseNavHintOperatorRank)).toBeInTheDocument();
    });
  });

  describe("AlertOperatorToolingRankCue", () => {
    it("selects reader vs operator tooling lines at the Execute boundary", () => {
      navCallerAuthorityRank.current = AUTHORITY_RANK.ReadAuthority;
      const { unmount } = render(<AlertOperatorToolingRankCue />);

      expect(screen.getByRole("note")).toHaveTextContent(alertOperatorToolingReaderRankLine);

      unmount();
      navCallerAuthorityRank.current = AUTHORITY_RANK.ExecuteAuthority;
      render(<AlertOperatorToolingRankCue />);

      expect(screen.getByRole("note")).toHaveTextContent(alertOperatorToolingOperatorRankLine);
    });
  });
});
