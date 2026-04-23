/**
 * Page-level **authority-shaped layout** regression: ordering and visual hierarchy that should stay tied to
 * `useEnterpriseMutationCapability()` (Execute+ floor) and inspect-first patterns — not copy wording.
 *
 * **UI shaping only:** these assertions do not prove authorization; **ArchLucid.Api** `[Authorize]` remains
 * authoritative for POST/toggle. See **docs/PRODUCT_PACKAGING.md** §3 *Four UI shaping surfaces* and *Contributor drift guard*.
 *
 * Broader seam parity lives in `authority-seam-regression.test.ts`, `authority-execute-floor-regression.test.ts`,
 * `nav-shell-visibility.test.ts`, and `enterprise-authority-ui-shaping.test.tsx` (mutation → disabled/readOnly).
 * Rank-gated **note** lines live in `EnterpriseControlsContextHints.authority.test.tsx`.
 */
import { render, screen, waitFor, within } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";

const mutateCapability = vi.hoisted(() => ({ current: false }));

vi.mock("@/hooks/use-enterprise-mutation-capability", () => ({
  useEnterpriseMutationCapability: (): boolean => mutateCapability.current,
}));

// Pages migrated to `useNavSurface()` resolve `mutationCapability` through
// the composed hook; mock it so the same `mutateCapability.current` ref
// drives every page in this suite.
vi.mock("@/lib/use-nav-surface", async (importOriginal) => {
  const mod = await importOriginal<typeof import("@/lib/use-nav-surface")>();

  return {
    ...mod,
    useNavSurface: (routeKey: import("@/lib/layer-guidance").LayerGuidancePageKey) => {
      const real = mod.composeNavSurface(routeKey, 0, false, false, true);

      return { ...real, mutationCapability: mutateCapability.current };
    },
  };
});

vi.mock("next/navigation", () => ({
  useSearchParams: (): URLSearchParams => new URLSearchParams(),
}));

const apiHoisted = vi.hoisted(() => ({
  listPolicyPacks: vi.fn(),
  getEffectivePolicyPacks: vi.fn(),
  getEffectivePolicyContent: vi.fn(),
  listPolicyPackVersions: vi.fn(),
  listAlertsPaged: vi.fn(),
  listApprovalRequests: vi.fn(),
  listPromotions: vi.fn(),
  listActivations: vi.fn(),
  listAlertRoutingSubscriptions: vi.fn(),
}));

vi.mock("@/lib/api", async (importOriginal) => {
  const mod = await importOriginal<typeof import("@/lib/api")>();

  return {
    ...mod,
    listPolicyPacks: apiHoisted.listPolicyPacks,
    getEffectivePolicyPacks: apiHoisted.getEffectivePolicyPacks,
    getEffectivePolicyContent: apiHoisted.getEffectivePolicyContent,
    listPolicyPackVersions: apiHoisted.listPolicyPackVersions,
    listAlertsPaged: apiHoisted.listAlertsPaged,
    listApprovalRequests: apiHoisted.listApprovalRequests,
    listPromotions: apiHoisted.listPromotions,
    listActivations: apiHoisted.listActivations,
    listAlertRoutingSubscriptions: apiHoisted.listAlertRoutingSubscriptions,
  };
});

vi.mock("next/link", () => ({
  default: ({
    href,
    children,
  }: {
    href: string;
    children: import("react").ReactNode;
  }) => <a href={href}>{children}</a>,
}));

import AlertRoutingPage from "./alert-routing/page";
import AlertsPage from "./alerts/page";
import GovernanceWorkflowPage from "./governance/page";
import PolicyPacksPage from "./policy-packs/page";

const sampleAlert = {
  alertId: "alert-layout-1",
  ruleId: "rule-1",
  title: "Layout fixture alert",
  category: "Test",
  severity: "High",
  status: "Open",
  triggerValue: "n/a",
  description: "Synthetic row for layout regression.",
  createdUtc: new Date().toISOString(),
};

describe("authority-shaped layout regression", () => {
  beforeEach(() => {
    mutateCapability.current = false;
    apiHoisted.listPolicyPacks.mockResolvedValue([]);
    apiHoisted.getEffectivePolicyPacks.mockResolvedValue({
      tenantId: "",
      workspaceId: "",
      projectId: "",
      packs: [],
    });
    apiHoisted.getEffectivePolicyContent.mockResolvedValue({
      complianceRuleIds: [],
      complianceRuleKeys: [],
      alertRuleIds: [],
      compositeAlertRuleIds: [],
      advisoryDefaults: {},
      metadata: {},
    });
    apiHoisted.listPolicyPackVersions.mockResolvedValue([]);
    apiHoisted.listAlertsPaged.mockResolvedValue({ items: [sampleAlert], totalCount: 1 });
    apiHoisted.listApprovalRequests.mockResolvedValue([]);
    apiHoisted.listPromotions.mockResolvedValue([]);
    apiHoisted.listActivations.mockResolvedValue([]);
    apiHoisted.listAlertRoutingSubscriptions.mockResolvedValue([
      {
        routingSubscriptionId: "rs-layout-1",
        tenantId: "t",
        workspaceId: "w",
        projectId: "p",
        name: "Layout fixture subscription",
        channelType: "Email",
        destination: "ops@example.test",
        minimumSeverity: "High",
        isEnabled: true,
        createdUtc: new Date().toISOString(),
        metadataJson: "{}",
      },
    ]);
  });

  /**
   * Reader / non-mutating shell: submit card moves after inspect sections so "Load a run" is not buried under writes.
   * Regression: dropping `flex-col-reverse` would equal-weight submit vs inspect again.
   */
  it("Governance workflow: inspect-first column order when mutation capability is false", async () => {
    mutateCapability.current = false;
    const { container } = render(<GovernanceWorkflowPage />);

    await waitFor(() => {
      expect(container.querySelector(".flex.flex-col-reverse")).not.toBeNull();
    });
  });

  /** Same inspect-first contract as workflow: current packs + JSON before lifecycle when reads cannot mutate. */
  it("Policy packs: inspect-first column order when mutation capability is false", async () => {
    mutateCapability.current = false;
    const { container } = render(<PolicyPacksPage />);

    await waitFor(() => {
      expect(container.querySelector(".flex.flex-col-reverse")).not.toBeNull();
    });
  });

  /**
   * Triage strip is slightly deemphasized when Confirm/write is off — keeps triage visible without implying parity with
   * operator write affordances. Regression: removing `opacity-90` loses the hierarchy cue.
   */
  it("Alerts inbox: triage section deemphasized when mutation capability is false", async () => {
    mutateCapability.current = false;
    render(<AlertsPage />);

    await waitFor(() => {
      expect(screen.getByRole("article")).toBeInTheDocument();
    });

    const triage = screen.getByRole("region", { name: "Triage actions" });

    expect(triage).toHaveClass("opacity-90");
  });

  /**
   * Inspect (delivery history) before configure-adjacent toggle — read-tier users see the safe action first.
   * Ordering is structural (button labels), not tooltip prose.
   */
  it("Alert routing: delivery inspect button precedes enable/disable on a subscription row", async () => {
    mutateCapability.current = true;
    render(<AlertRoutingPage />);

    await waitFor(() => {
      expect(screen.getByText("Layout fixture subscription")).toBeInTheDocument();
    });

    const card = screen.getByText("Layout fixture subscription").closest("div");

    expect(card).not.toBeNull();

    const buttons = within(card as HTMLElement).getAllByRole("button");
    const labels = buttons.map((b) => b.textContent?.trim() ?? "");

    const inspectIdx = labels.findIndex((t) => t.includes("Show delivery attempts"));
    const toggleIdx = labels.findIndex((t) => t === "Disable" || t === "Enable");

    expect(inspectIdx).toBeGreaterThanOrEqual(0);
    expect(toggleIdx).toBeGreaterThanOrEqual(0);
    expect(inspectIdx).toBeLessThan(toggleIdx);
  });
});
