import { render } from "@testing-library/react";
import { axe, toHaveNoViolations } from "jest-axe";
import { describe, expect, it, vi } from "vitest";

vi.mock("next/navigation", () => ({
  useRouter: () => ({ push: vi.fn(), replace: vi.fn(), back: vi.fn() }),
  usePathname: () => "/value-report",
  useSearchParams: () => ({
    get: () => null,
    toString: () => "",
  }),
  redirect: vi.fn(),
}));

vi.mock("@/lib/api", () => ({
  apiGet: vi.fn().mockResolvedValue([]),
  downloadValueReportDocx: vi.fn(),
  downloadBoardPackPdf: vi.fn(),
  listAdvisorySchedules: vi.fn().mockResolvedValue([]),
  getAdvisoryScheduleDetail: vi.fn().mockResolvedValue(null),
  listAdvisoryExecutions: vi.fn().mockResolvedValue([]),
  createAdvisorySchedule: vi.fn(),
  updateAdvisorySchedule: vi.fn(),
  deleteAdvisorySchedule: vi.fn(),
  listDigests: vi.fn().mockResolvedValue([]),
  getDigestDetail: vi.fn().mockResolvedValue(null),
  listDigestSubscriptions: vi.fn().mockResolvedValue([]),
  createDigestSubscription: vi.fn(),
  updateDigestSubscription: vi.fn(),
  deleteDigestSubscription: vi.fn(),
  listPlanningPlans: vi.fn().mockResolvedValue([]),
  createPlanningPlan: vi.fn(),
  getPlanningPlanDetail: vi.fn().mockResolvedValue(null),
}));

vi.mock("@/lib/toast", () => ({
  showError: vi.fn(),
  showSuccess: vi.fn(),
}));

vi.mock("@/hooks/use-enterprise-mutation-capability", () => ({
  useEnterpriseMutationCapability: () => false,
}));

// LayerHeader reads layerGuidance + contextHints.layerHeaderEnterpriseRankCue; stub the full shape.
vi.mock("@/lib/use-nav-surface", () => ({
  useNavSurface: () => ({
    links: [],
    mutationCapability: false,
    layerGuidance: {
      layerBadge: "Pilot",
      headline: "Stub headline",
      useWhen: "For accessibility tests.",
      firstPilotNote: null,
      enterpriseFootnote: null,
    },
    contextHints: {
      enterpriseNavGroupHint: "",
      enterpriseExecutePageHint: null,
      layerHeaderEnterpriseRankCue: null,
      governanceResolutionRank: "",
      alertsInboxRank: "",
      auditLogRank: "",
      alertOperatorToolingRank: "",
      governanceDashboardReaderAction: null,
    },
    callerAuthorityRank: 0,
    showExtended: true,
    showAdvanced: true,
    mounted: true,
  }),
}));

vi.mock("@/hooks/useViewportNarrow", () => ({
  useViewportNarrow: () => false,
}));

vi.mock("@/lib/current-principal", () => ({
  buildAuthMeProxyRequestInit: vi.fn().mockReturnValue({}),
}));

vi.mock("@/lib/scope-defaults", () => ({
  DEFAULT_DEV_TENANT_ID: "00000000-0000-0000-0000-000000000000",
}));

vi.mock("@/components/OperatorNavAuthorityProvider", () => ({
  useNavCallerAuthorityRank: () => "admin",
  useOperatorNavAuthority: () => ({
    rank: "admin",
    isLoading: false,
    refresh: vi.fn(),
  }),
}));

vi.mock("@/components/advisory/AdvisoryHubClient", () => ({
  AdvisoryHubClient: () => <div data-testid="stub-advisory-hub">Advisory Hub</div>,
}));

vi.mock("@/components/advisory/AdvisorySchedulingClient", () => ({
  AdvisorySchedulingClient: () => <div data-testid="stub-advisory-scheduling">Scheduling</div>,
}));

vi.mock("@/components/digests/DigestsHubClient", () => ({
  DigestsHubClient: () => <div data-testid="stub-digests-hub">Digests</div>,
}));

vi.mock("@/components/digests/DigestSubscriptionsClient", () => ({
  DigestSubscriptionsClient: () => <div data-testid="stub-digest-subscriptions">Subscriptions</div>,
}));

vi.mock("@/components/planning/PlanningHubClient", () => ({
  PlanningHubClient: () => <div data-testid="stub-planning-hub">Planning</div>,
}));

import ValueReportPage from "@/app/(operator)/value-report/page";
import AdvisoryPage from "@/app/(operator)/advisory/page";
import AdvisorySchedulingPage from "@/app/(operator)/advisory-scheduling/page";
import DigestsPage from "@/app/(operator)/digests/page";
import DigestSubscriptionsPage from "@/app/(operator)/digest-subscriptions/page";
import PlanningPage from "@/app/(operator)/planning/page";

expect.extend(toHaveNoViolations);

describe("operator value + advisory pages — axe (Vitest)", () => {
  it(
    "ValueReportPage has no serious axe violations",
    async () => {
      const { container } = render(<ValueReportPage />);

      expect(await axe(container)).toHaveNoViolations();
    },
    20_000,
  );

  it(
    "AdvisoryPage has no serious axe violations",
    async () => {
      const { container } = render(<AdvisoryPage />);

      expect(await axe(container)).toHaveNoViolations();
    },
    20_000,
  );

  it(
    "AdvisorySchedulingPage has no serious axe violations",
    async () => {
      const { container } = render(<AdvisorySchedulingPage />);

      expect(await axe(container)).toHaveNoViolations();
    },
    20_000,
  );

  it(
    "DigestsPage has no serious axe violations",
    async () => {
      const { container } = render(<DigestsPage />);

      expect(await axe(container)).toHaveNoViolations();
    },
    20_000,
  );

  it(
    "DigestSubscriptionsPage has no serious axe violations",
    async () => {
      const { container } = render(<DigestSubscriptionsPage />);

      expect(await axe(container)).toHaveNoViolations();
    },
    20_000,
  );

  it(
    "PlanningPage has no serious axe violations",
    async () => {
      const { container } = render(<PlanningPage />);

      expect(await axe(container)).toHaveNoViolations();
    },
    20_000,
  );
});
