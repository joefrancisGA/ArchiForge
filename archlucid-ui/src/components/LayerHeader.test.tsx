import { render, screen } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

import { layerHeaderEnterpriseOperatorRankLine } from "@/lib/enterprise-controls-context-copy";
import { LAYER_PAGE_GUIDANCE, type LayerGuidancePageKey } from "@/lib/layer-guidance";
import { AUTHORITY_RANK } from "@/lib/nav-authority";

/** Default Admin rank for tests — literal `3` because `vi.hoisted` runs before `AUTHORITY_RANK` is available. */
const navCallerAuthorityRank = vi.hoisted(() => ({ current: 3 }));

vi.mock("@/components/OperatorNavAuthorityProvider", () => ({
  useNavCallerAuthorityRank: (): number => navCallerAuthorityRank.current,
}));

import { LayerHeader } from "./LayerHeader";

describe("LayerHeader", () => {
  beforeEach(() => {
    navCallerAuthorityRank.current = AUTHORITY_RANK.AdminAuthority;
  });

  afterEach(() => {
    navCallerAuthorityRank.current = AUTHORITY_RANK.AdminAuthority;
  });

  it("renders Operate guidance for compare (analysis slice)", () => {
    render(<LayerHeader pageKey="compare" />);

    expect(screen.getByText("Operate")).toBeInTheDocument();
    expect(screen.getByText(/what changed between two finalized runs/i)).toBeInTheDocument();
  });

  it("renders Operate governance responsibility footnote on audit", () => {
    render(<LayerHeader pageKey="audit" />);

    expect(screen.getByText(/Search \+ bounded CSV export\./i)).toBeInTheDocument();
  });

  /**
   * Discoverability: `LayerHeader` puts badge + headline in `aria-label` on the `<aside>` (implicit `complementary`).
   */
  it("exposes Operate audit strip accessible name from badge and headline", () => {
    render(<LayerHeader pageKey="audit" />);

    expect(
      screen.getByRole("complementary", { name: /Operate:.*tenant audit trail/i }),
    ).toBeInTheDocument();
  });

  it("renders governance resolution Operate footnote", () => {
    render(<LayerHeader pageKey="governance-resolution" />);

    expect(screen.getByText(/Read-only stack; edits on Packs or Workflow\./i)).toBeInTheDocument();
  });

  it("renders Execute+ rank cue on Operate governance audit when caller rank is Execute+", () => {
    navCallerAuthorityRank.current = AUTHORITY_RANK.ExecuteAuthority;
    render(<LayerHeader pageKey="audit" />);

    expect(screen.getByTestId("layer-header-operate-execute-rank-cue")).toHaveTextContent(
      layerHeaderEnterpriseOperatorRankLine,
    );
  });

  it("does not render Execute+ rank cue on Operate governance audit when caller is Read", () => {
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

  it("does not render Execute+ rank cue on Operate analysis pages", () => {
    render(<LayerHeader pageKey="compare" />);

    expect(screen.queryByTestId("layer-header-operate-execute-rank-cue")).toBeNull();
  });

  /**
   * Every Operate governance guidance key must surface the Execute+ rank-aware note when rank allows — packaging ↔ nav floor.
   */
  it("renders Execute+ rank cue for every Operate governance layer-guidance page key at Execute rank", () => {
    const governanceKeys = (Object.keys(LAYER_PAGE_GUIDANCE) as LayerGuidancePageKey[]).filter(
      (key) =>
        LAYER_PAGE_GUIDANCE[key].layerBadge === "Operate" && LAYER_PAGE_GUIDANCE[key].enterpriseFootnote != null,
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
