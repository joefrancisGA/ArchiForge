import { render, screen } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

import {
  layerHeaderEnterpriseOperatorRankLine,
  layerHeaderEnterpriseReaderRankLine,
} from "@/lib/enterprise-controls-context-copy";
import { AUTHORITY_RANK } from "@/lib/nav-authority";

/** Default Admin rank for tests — literal `3` because `vi.hoisted` runs before `AUTHORITY_RANK` is available. */
const navCallerAuthorityRank = vi.hoisted(() => ({ current: 3 }));

vi.mock("@/components/OperatorNavAuthorityProvider", () => ({
  useNavCallerAuthorityRank: (): number => navCallerAuthorityRank.current,
}));

import { LayerHeader } from "./LayerHeader";

/** Rank lines are asserted via `data-testid` + `enterprise-controls-context-copy` imports (single source of truth). */
describe("LayerHeader", () => {
  beforeEach(() => {
    navCallerAuthorityRank.current = AUTHORITY_RANK.AdminAuthority;
  });

  afterEach(() => {
    navCallerAuthorityRank.current = AUTHORITY_RANK.AdminAuthority;
  });

  it("renders Advanced Analysis guidance for compare", () => {
    render(<LayerHeader pageKey="compare" />);

    expect(screen.getByText("Advanced Analysis")).toBeInTheDocument();
    expect(screen.getByText(/what changed between two committed runs/i)).toBeInTheDocument();
  });

  it("renders Enterprise responsibility footnote on audit", () => {
    render(<LayerHeader pageKey="audit" />);

    expect(
      screen.getByText(/Evidence search and bounded export\. Not required for Core Pilot\./i),
    ).toBeInTheDocument();
  });

  it("renders governance resolution Enterprise footnote", () => {
    render(<LayerHeader pageKey="governance-resolution" />);

    expect(
      screen.getByText(/Effective policy here; changes live in policy packs or workflow/i),
    ).toBeInTheDocument();
  });

  it("renders Enterprise rank cue on Enterprise Controls audit (operator+ rank line)", () => {
    navCallerAuthorityRank.current = AUTHORITY_RANK.ExecuteAuthority;
    render(<LayerHeader pageKey="audit" />);

    expect(screen.getByTestId("layer-header-enterprise-rank-cue")).toHaveTextContent(
      layerHeaderEnterpriseOperatorRankLine,
    );
  });

  it("switches Enterprise rank cue to reader line when caller rank is Read", () => {
    navCallerAuthorityRank.current = AUTHORITY_RANK.ReadAuthority;
    render(<LayerHeader pageKey="audit" />);

    expect(screen.getByTestId("layer-header-enterprise-rank-cue")).toHaveTextContent(
      layerHeaderEnterpriseReaderRankLine,
    );
  });

  /** Below numeric Read (e.g. unset rank): same branch as Reader — conservative UI shaping. */
  it("uses Enterprise reader rank cue when caller rank is below Read policy floor", () => {
    navCallerAuthorityRank.current = 0;
    render(<LayerHeader pageKey="audit" />);

    expect(screen.getByTestId("layer-header-enterprise-rank-cue")).toHaveTextContent(
      layerHeaderEnterpriseReaderRankLine,
    );
  });

  it("does not render Enterprise rank cue on Advanced Analysis pages", () => {
    render(<LayerHeader pageKey="compare" />);

    expect(screen.queryByTestId("layer-header-enterprise-rank-cue")).toBeNull();
  });
});
