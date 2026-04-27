import { render } from "@testing-library/react";
import { axe, toHaveNoViolations } from "jest-axe";
import { describe, expect, it, vi } from "vitest";

vi.mock("next/navigation", () => ({
  useRouter: () => ({ push: vi.fn(), replace: vi.fn(), back: vi.fn() }),
  usePathname: () => "/compare",
  useSearchParams: () => ({
    get: () => null,
    toString: () => "",
  }),
}));

vi.mock("@/lib/api", () => ({
  apiGet: vi.fn().mockResolvedValue([]),
  compareRuns: vi.fn().mockResolvedValue({}),
  compareGoldenManifestRuns: vi.fn().mockResolvedValue({}),
  explainComparisonRuns: vi.fn().mockResolvedValue({}),
  replayRun: vi.fn().mockResolvedValue({}),
  fetchEvolutionCandidates: vi.fn().mockResolvedValue([]),
  fetchEvolutionResults: vi.fn().mockResolvedValue(null),
  postEvolutionSimulate: vi.fn().mockResolvedValue({}),
}));

vi.mock("@/lib/graph-api", () => ({
  getProvenanceGraph: vi.fn().mockResolvedValue({ nodes: [], edges: [] }),
  getDecisionSubgraph: vi.fn().mockResolvedValue({ nodes: [], edges: [] }),
  getNodeNeighborhood: vi.fn().mockResolvedValue({ nodes: [], edges: [] }),
  getArchitectureGraph: vi.fn().mockResolvedValue({ nodes: [], edges: [] }),
}));

vi.mock("@/lib/toast", () => ({
  showError: vi.fn(),
  showSuccess: vi.fn(),
}));

import ComparePage from "@/app/(operator)/compare/page";
import ReplayPage from "@/app/(operator)/replay/page";
import GraphPage from "@/app/(operator)/graph/page";
import EvolutionReviewPage from "@/app/(operator)/evolution-review/page";

expect.extend(toHaveNoViolations);

describe("operator analysis pages — axe (Vitest)", () => {
  it(
    "ComparePage has no serious axe violations",
    async () => {
      const { container } = render(<ComparePage />);

      expect(await axe(container)).toHaveNoViolations();
    },
    20_000,
  );

  it(
    "ReplayPage has no serious axe violations",
    async () => {
      const { container } = render(<ReplayPage />);

      expect(await axe(container)).toHaveNoViolations();
    },
    20_000,
  );

  it(
    "GraphPage has no serious axe violations",
    async () => {
      const { container } = render(<GraphPage />);

      expect(await axe(container)).toHaveNoViolations();
    },
    20_000,
  );

  it(
    "EvolutionReviewPage has no serious axe violations",
    async () => {
      const { container } = render(<EvolutionReviewPage />);

      expect(await axe(container)).toHaveNoViolations();
    },
    20_000,
  );
});
