import { render } from "@testing-library/react";
import { axe, toHaveNoViolations } from "jest-axe";
import { describe, expect, it, vi } from "vitest";

vi.mock("next/navigation", () => ({
  useRouter: () => ({ push: vi.fn(), replace: vi.fn(), back: vi.fn() }),
  usePathname: () => "/governance",
  useSearchParams: () => ({
    get: () => null,
    toString: () => "",
  }),
}));

vi.mock("@/lib/api", () => ({
  apiGet: vi.fn().mockResolvedValue([]),
  listApprovalRequests: vi.fn().mockResolvedValue([]),
  listPromotions: vi.fn().mockResolvedValue([]),
  listActivations: vi.fn().mockResolvedValue([]),
  submitApprovalRequest: vi.fn(),
  approveRequest: vi.fn(),
  rejectRequest: vi.fn(),
  promoteManifest: vi.fn(),
  activateEnvironment: vi.fn(),
  getGovernanceDashboard: vi.fn().mockResolvedValue({
    pendingApprovals: [],
    recentDecisions: [],
    recentChanges: [],
    pendingCount: 0,
  }),
  getComplianceDriftTrend: vi.fn().mockResolvedValue([]),
  getGovernanceResolution: vi.fn().mockResolvedValue({
    notes: [],
    effectiveContent: {},
    conflicts: [],
    decisions: [],
  }),
  listPolicyPacks: vi.fn().mockResolvedValue([]),
  getEffectivePolicyPacks: vi.fn().mockResolvedValue({ packs: [] }),
  getEffectivePolicyContent: vi.fn().mockResolvedValue({}),
  listPolicyPackVersions: vi.fn().mockResolvedValue([]),
  createPolicyPack: vi.fn(),
  publishPolicyPackVersion: vi.fn(),
  assignPolicyPack: vi.fn(),
}));

vi.mock("@/hooks/use-enterprise-mutation-capability", () => ({
  useEnterpriseMutationCapability: () => false,
}));

vi.mock("@/lib/use-nav-surface", () => ({
  useNavSurface: () => ({ mutationCapability: false }),
}));

vi.mock("@/hooks/useViewportNarrow", () => ({
  useViewportNarrow: () => false,
}));

vi.mock("@/lib/toast", () => ({
  showError: vi.fn(),
  showSuccess: vi.fn(),
}));

import GovernanceWorkflowPage from "@/app/(operator)/governance/page";
import GovernanceDashboardPage from "@/app/(operator)/governance/dashboard/page";
import GovernanceResolutionPage from "@/app/(operator)/governance-resolution/page";
import GovernanceFindingsPage from "@/app/(operator)/governance/findings/page";
import PolicyPacksPage from "@/app/(operator)/policy-packs/page";

expect.extend(toHaveNoViolations);

describe("operator governance pages — axe (Vitest)", () => {
  it(
    "GovernanceWorkflowPage has no serious axe violations",
    async () => {
      const { container } = render(<GovernanceWorkflowPage />);

      expect(await axe(container)).toHaveNoViolations();
    },
    20_000,
  );

  it(
    "GovernanceDashboardPage has no serious axe violations",
    async () => {
      const { container } = render(<GovernanceDashboardPage />);

      expect(await axe(container)).toHaveNoViolations();
    },
    20_000,
  );

  it(
    "GovernanceResolutionPage has no serious axe violations",
    async () => {
      const { container } = render(<GovernanceResolutionPage />);

      expect(await axe(container)).toHaveNoViolations();
    },
    20_000,
  );

  it(
    "GovernanceFindingsPage has no serious axe violations",
    async () => {
      const { container } = render(<GovernanceFindingsPage />);

      expect(await axe(container)).toHaveNoViolations();
    },
    20_000,
  );

  it(
    "PolicyPacksPage has no serious axe violations",
    async () => {
      const { container } = render(<PolicyPacksPage />);

      expect(await axe(container)).toHaveNoViolations();
    },
    20_000,
  );
});
