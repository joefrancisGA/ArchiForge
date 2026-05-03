import { render, screen } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

import { layerHeaderEnterpriseOperatorRankLine } from "@/lib/enterprise-controls-context-copy";
import { LAYER_PAGE_GUIDANCE, type LayerGuidancePageKey } from "@/lib/layer-guidance";
import { AUTHORITY_RANK } from "@/lib/nav-authority";

/** Default Admin rank for tests — literal `3` because `vi.hoisted` runs before `AUTHORITY_RANK` is available. */
const navCallerAuthorityRank = vi.hoisted(() => ({ current: 3 }));

vi.mock("@/components/OperatorNavAuthorityProvider", () => ({
  useNavCallerAuthorityRank: (): number => navCallerAuthorityRank.current,
  /** Matches `composeNavSurface(..., hasCommittedArchitectureReview = true)` — LayerHeader ignores nav links from the surface. */
  useNavCommittedArchitectureReview: (): boolean => true,
}));

import { LayerHeader } from "./LayerHeader";

describe("LayerHeader", () => {
  beforeEach(() => {
    navCallerAuthorityRank.current = AUTHORITY_RANK.AdminAuthority;
  });

  afterEach(() => {
    navCallerAuthorityRank.current = AUTHORITY_RANK.AdminAuthority;
  });

  it("renders Analysis guidance for compare (analysis slice)", () => {
    render(<LayerHeader pageKey="compare" />);

    expect(screen.getByText("Analysis")).toBeInTheDocument();
    expect(screen.getByText(/what changed between two finalized reviews/i)).toBeInTheDocument();
  });

  it("renders Governance responsibility footnote on audit", () => {
    render(<LayerHeader pageKey="audit" />);

    expect(screen.getByText("Governance")).toBeInTheDocument();
    expect(screen.getByText(/Search first; CSV export for auditors and admins\./i)).toBeInTheDocument();
  });

  /**
   * Discoverability: `LayerHeader` puts badge + headline in `aria-label` on the `<aside>` (implicit `complementary`).
   */
  it("exposes Governance audit strip accessible name from badge and headline", () => {
    render(<LayerHeader pageKey="audit" />);

    expect(
      screen.getByRole("complementary", { name: /Governance:.*tenant audit trail/i }),
    ).toBeInTheDocument();
  });

  it("renders governance resolution Governance footnote", () => {
    render(<LayerHeader pageKey="governance-resolution" />);

    expect(screen.getByText(/Read-only stack; edits on Packs or Workflow\./i)).toBeInTheDocument();
  });

  it("renders Execute+ rank cue on Governance audit when caller rank is Execute+", () => {
    navCallerAuthorityRank.current = AUTHORITY_RANK.ExecuteAuthority;
    render(<LayerHeader pageKey="audit" />);

    expect(screen.getByTestId("layer-header-operate-execute-rank-cue")).toHaveTextContent(
      layerHeaderEnterpriseOperatorRankLine,
    );
  });

  it("does not render Execute+ rank cue on Governance audit when caller is Read", () => {
    navCallerAuthorityRank.current = AUTHORITY_RANK.ReadAuthority;
    render(<LayerHeader pageKey="audit" />);

    expect(screen.queryByTestId("layer-header-operate-execute-rank-cue")).toBeNull();
  });

  /** Below numeric Read (e.g. unset rank): no Execute strip on governance pages. */
  it("does not render Execute+ rank cue when caller rank is below Execute", () => {
    navCallerAuthorityRank.current = 0;
    render(<LayerHeader pageKey="audit" />);

    expect(screen.queryByTestId("layer-header-operate-execute-rank-cue")).toBeNull();
  });

  it("does not render Execute+ rank cue on Analysis pages", () => {
    render(<LayerHeader pageKey="compare" />);

    expect(screen.queryByTestId("layer-header-operate-execute-rank-cue")).toBeNull();
  });

  /**
   * Every Governance guidance key must surface the Execute+ rank-aware note when rank allows — packaging ↔ nav floor.
   */
  it("renders Execute+ rank cue for every Governance layer-guidance page key at Execute rank", () => {
    const governanceKeys = (Object.keys(LAYER_PAGE_GUIDANCE) as LayerGuidancePageKey[]).filter(
      (key) =>
        LAYER_PAGE_GUIDANCE[key].layerBadge === "Governance" && LAYER_PAGE_GUIDANCE[key].enterpriseFootnote != null,
    );

    expect(governanceKeys.length).toBeGreaterThan(0);
    navCallerAuthorityRank.current = AUTHORITY_RANK.ExecuteAuthority;

    for (const pageKey of governanceKeys) {
      const { unmount } = render(<LayerHeader pageKey={pageKey} />);

      expect(screen.getByTestId("layer-header-operate-execute-rank-cue")).toBeInTheDocument();
      unmount();
    }
  });
});
