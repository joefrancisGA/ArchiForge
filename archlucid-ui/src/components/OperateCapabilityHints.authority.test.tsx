/**
 * Rank-gated **Capability** surface hints (same `AUTHORITY_RANK.ExecuteAuthority` threshold as nav filtering and
 * `useOperateCapability` / deprecated `useEnterpriseMutationCapability`). Asserts presence/absence and canonical copy imports.
 */
import { render, screen } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";

import {
  alertsInboxRankOperatorLine,
  alertsInboxRankReaderLine,
  alertOperatorToolingOperatorRankLine,
  alertOperatorToolingReaderRankLine,
  auditLogRankOperatorLine,
  auditLogRankReaderLine,
  enterpriseExecutePageHintReaderRank,
  enterpriseNavHintOperatorRank,
  enterpriseNavHintReaderRank,
  governanceDashboardReaderActionLine,
  governanceResolutionRankOperatorLine,
  governanceResolutionRankReaderLine,
} from "@/lib/enterprise-controls-context-copy";

/** Literal `1` — hoisted factory runs before `AUTHORITY_RANK` import is safe to reference. */
const navCallerAuthorityRank = vi.hoisted(() => ({ current: 1 }));

vi.mock("@/components/OperatorNavAuthorityProvider", () => ({
  useNavCallerAuthorityRank: (): number => navCallerAuthorityRank.current,
}));

import { AUTHORITY_RANK } from "@/lib/nav-authority";

import {
  AlertsInboxRankCue,
  AlertOperatorToolingRankCue,
  AuditLogRankCue,
  GovernanceDashboardReaderActionCue,
  GovernanceResolutionRankCue,
  OperateCapabilityNavGroupHint,
  OperateExecutePageHint,
  OperateExecutePlusPageCue,
} from "./OperateCapabilityHints";

describe("OperateCapabilityHints authority shaping", () => {
  afterEach(() => {
    navCallerAuthorityRank.current = AUTHORITY_RANK.ReadAuthority;
  });

  describe("OperateExecutePageHint", () => {
    it("shows reader execute warning below Execute rank", () => {
      navCallerAuthorityRank.current = AUTHORITY_RANK.ReadAuthority;
      render(<OperateExecutePageHint />);

      expect(screen.getByRole("note")).toHaveTextContent(enterpriseExecutePageHintReaderRank);
    });

    it("renders nothing at Execute+ to avoid stacking with operator-plus cues", () => {
      navCallerAuthorityRank.current = AUTHORITY_RANK.ExecuteAuthority;
      const { container } = render(<OperateExecutePageHint />);

      expect(container.firstChild).toBeNull();
    });
  });

  describe("OperateExecutePlusPageCue", () => {
    const operatorPlusMessage = "Operator-plus cue fixture";

    it("is hidden below Execute", () => {
      navCallerAuthorityRank.current = AUTHORITY_RANK.ReadAuthority;
      const { container } = render(<OperateExecutePlusPageCue message={operatorPlusMessage} />);

      expect(container.firstChild).toBeNull();
    });

    it("shows the message at Execute+", () => {
      navCallerAuthorityRank.current = AUTHORITY_RANK.ExecuteAuthority;
      render(<OperateExecutePlusPageCue message={operatorPlusMessage} />);

      expect(screen.getByRole("note")).toHaveTextContent(operatorPlusMessage);
    });
  });

  describe("OperateCapabilityNavGroupHint", () => {
    it("uses reader nav copy below Execute", () => {
      navCallerAuthorityRank.current = AUTHORITY_RANK.ReadAuthority;
      render(<OperateCapabilityNavGroupHint />);

      expect(screen.getByText(enterpriseNavHintReaderRank)).toBeInTheDocument();
    });

    it("uses operator nav copy at Execute+", () => {
      navCallerAuthorityRank.current = AUTHORITY_RANK.ExecuteAuthority;
      render(<OperateCapabilityNavGroupHint />);

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

  /** Governance resolution page: inspect vs configure copy must flip at the same rank as nav + mutation hook. */
  describe("GovernanceResolutionRankCue", () => {
    it("selects reader vs operator lines at the Execute boundary", () => {
      navCallerAuthorityRank.current = AUTHORITY_RANK.ReadAuthority;
      const { unmount } = render(<GovernanceResolutionRankCue />);

      expect(screen.getByRole("note")).toHaveTextContent(governanceResolutionRankReaderLine);

      unmount();
      navCallerAuthorityRank.current = AUTHORITY_RANK.ExecuteAuthority;
      render(<GovernanceResolutionRankCue />);

      expect(screen.getByRole("note")).toHaveTextContent(governanceResolutionRankOperatorLine);
    });
  });

  /** Audit log page: rank cue uses the same Execute threshold (CSV role story is separate on the page). */
  describe("AuditLogRankCue", () => {
    it("selects reader vs operator lines at the Execute boundary", () => {
      navCallerAuthorityRank.current = AUTHORITY_RANK.ReadAuthority;
      const { unmount } = render(<AuditLogRankCue />);

      expect(screen.getByRole("note")).toHaveTextContent(auditLogRankReaderLine);

      unmount();
      navCallerAuthorityRank.current = AUTHORITY_RANK.ExecuteAuthority;
      render(<AuditLogRankCue />);

      expect(screen.getByRole("note")).toHaveTextContent(auditLogRankOperatorLine);
    });
  });

  /** Alerts inbox: second line under LayerHeader; must stay aligned with triage soft-disable threshold. */
  describe("AlertsInboxRankCue", () => {
    it("selects reader vs operator lines at the Execute boundary", () => {
      navCallerAuthorityRank.current = AUTHORITY_RANK.ReadAuthority;
      const { unmount } = render(<AlertsInboxRankCue />);

      expect(screen.getByRole("note")).toHaveTextContent(alertsInboxRankReaderLine);

      unmount();
      navCallerAuthorityRank.current = AUTHORITY_RANK.ExecuteAuthority;
      render(<AlertsInboxRankCue />);

      expect(screen.getByRole("note")).toHaveTextContent(alertsInboxRankOperatorLine);
    });
  });

  /**
   * Governance dashboard: reader-only queue cue is rank-gated so Execute+ operators are not told their queue is read-only.
   */
  describe("GovernanceDashboardReaderActionCue", () => {
    it("shows the reader queue line below Execute rank", () => {
      navCallerAuthorityRank.current = AUTHORITY_RANK.ReadAuthority;
      render(<GovernanceDashboardReaderActionCue />);

      expect(screen.getByRole("note")).toHaveTextContent(governanceDashboardReaderActionLine);
    });

    it("renders nothing at Execute+ (row actions available; avoid contradicting enabled controls)", () => {
      navCallerAuthorityRank.current = AUTHORITY_RANK.ExecuteAuthority;
      const { container } = render(<GovernanceDashboardReaderActionCue />);

      expect(container.firstChild).toBeNull();
    });
  });
});
