/**
 * Page-level regression: **`useEnterpriseMutationCapability()`** (deprecated; prefer **`useOperateCapability()`**) must
 * actually gate Operate write affordances. Lib-level parity lives in **`authority-seam-regression.test.ts`** /
 * **`current-principal.test.ts`**; this file catches inverted `disabled` props, dropped hooks, or pages that stop calling
 * the hook while nav still filters by rank.
 *
 * Governance workflow: submit card uses the same hook for read-only fields (`readOnly` / disabled selects) — asserted
 * via DOM attributes, not tooltip copy strings.
 *
 * Governance resolution: **`Change related controls`** reader supplement is driven only by the mutation capability hook
 * (GET **Refresh** stays enabled); rank cues on the same page use **`useNavCallerAuthorityRank`** in production — here the
 * mocked hook isolates the write-boundary copy from **`GovernanceResolutionRankCue`**.
 */
import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";

const mutateCapability = vi.hoisted(() => ({ current: false }));

vi.mock("@/hooks/use-enterprise-mutation-capability", () => ({
  useEnterpriseMutationCapability: (): boolean => mutateCapability.current,
}));

vi.mock("@/components/OperatorNavAuthorityProvider", async (importOriginal) => {
  const mod = await importOriginal<typeof import("@/components/OperatorNavAuthorityProvider")>();
  const { AUTHORITY_RANK } = await import("@/lib/nav-authority");

  return {
    ...mod,
    useNavCallerAuthorityRank: (): number =>
      mutateCapability.current ? AUTHORITY_RANK.ExecuteAuthority : AUTHORITY_RANK.ReadAuthority,
  };
});

// Pages that have migrated to `useNavSurface()` (Prompt 7 / `use-nav-surface.ts`)
// resolve `mutationCapability` through the composed hook. Mock it here so the
// same `mutateCapability.current` ref still drives every page in this suite.
vi.mock("@/lib/use-nav-surface", async (importOriginal) => {
  const mod = await importOriginal<typeof import("@/lib/use-nav-surface")>();
  const { AUTHORITY_RANK } = await import("@/lib/nav-authority");

  return {
    ...mod,
    useNavSurface: (routeKey: import("@/lib/layer-guidance").LayerGuidancePageKey) => {
      const callerRank = mutateCapability.current ? AUTHORITY_RANK.ExecuteAuthority : AUTHORITY_RANK.ReadAuthority;
      const real = mod.composeNavSurface(routeKey, callerRank, false, false, true);

      return { ...real, mutationCapability: mutateCapability.current };
    },
  };
});

vi.mock("next/navigation", () => ({
  usePathname: (): string => "/alerts",
  useRouter: (): { push: () => void; replace: () => void } => ({ push: vi.fn(), replace: vi.fn() }),
  useSearchParams: (): URLSearchParams => new URLSearchParams(),
}));

const apiHoisted = vi.hoisted(() => ({
  listPolicyPacks: vi.fn(),
  getEffectivePolicyPacks: vi.fn(),
  getEffectivePolicyContent: vi.fn(),
  listPolicyPackVersions: vi.fn(),
  listAlertsPaged: vi.fn(),
  listAlertRules: vi.fn(),
  listCompositeAlertRules: vi.fn(),
  listApprovalRequests: vi.fn(),
  listPromotions: vi.fn(),
  listActivations: vi.fn(),
  getGovernanceResolution: vi.fn(),
  listDigestSubscriptions: vi.fn(),
  listAdvisorySchedules: vi.fn(),
  listRunsByProjectPaged: vi.fn(),
}));

/**
 * Policy packs hides the lifecycle / create panel when demo static payloads are enabled
 * ({@link isStaticDemoPayloadFallbackEnabled}); CI sometimes sets demo env vars globally.
 * This suite asserts mutation gates on real controls, so keep demo-style suppression off here.
 */
vi.mock("@/lib/operator-static-demo", async (importOriginal) => {
  const mod = await importOriginal<typeof import("@/lib/operator-static-demo")>();

  return {
    ...mod,
    isStaticDemoPayloadFallbackEnabled: (): boolean => false,
  };
});

vi.mock("@/lib/api", async (importOriginal) => {
  const mod = await importOriginal<typeof import("@/lib/api")>();

  return {
    ...mod,
    listPolicyPacks: apiHoisted.listPolicyPacks,
    getEffectivePolicyPacks: apiHoisted.getEffectivePolicyPacks,
    getEffectivePolicyContent: apiHoisted.getEffectivePolicyContent,
    listPolicyPackVersions: apiHoisted.listPolicyPackVersions,
    listAlertsPaged: apiHoisted.listAlertsPaged,
    listAlertRules: apiHoisted.listAlertRules,
    listCompositeAlertRules: apiHoisted.listCompositeAlertRules,
    listApprovalRequests: apiHoisted.listApprovalRequests,
    listPromotions: apiHoisted.listPromotions,
    listActivations: apiHoisted.listActivations,
    getGovernanceResolution: apiHoisted.getGovernanceResolution,
    listDigestSubscriptions: apiHoisted.listDigestSubscriptions,
    listAdvisorySchedules: apiHoisted.listAdvisorySchedules,
    listRunsByProjectPaged: apiHoisted.listRunsByProjectPaged,
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

import {
  alertToolingListRefreshButtonTitleReader,
  alertSimulationCurrentBehaviorHeadingReader,
  alertTuningCurrentTuningHeadingReader,
  alertsInboxRefreshButtonTitleReader,
  alertsInboxRankReaderLine,
  alertsTriageDialogConfirmButtonLabelReaderRank,
  governanceResolutionChangeRelatedControlsReaderSupplement,
  governanceResolutionEffectivePolicyHeadingReader,
  governanceResolutionResolutionDetailsHeadingReader,
  advisorySchedulesCreateScheduleButtonLabelReaderRank,
  compositeRulesCreateButtonLabelReaderRank,
  digestSubscriptionsCreateSubscriptionButtonLabelReaderRank,
  governanceWorkflowApprovalRequestsCardTitleReader,
  governanceWorkflowPromotionsActivationsHeadingReader,
  governanceWorkflowSubmitCardTitleReader,
  policyPacksCreatePackButtonLabelReaderRank,
  policyPacksCurrentPacksHeadingOperator,
  policyPacksCurrentPacksHeadingReader,
  policyPacksPackContentHeadingReader,
} from "@/lib/enterprise-controls-context-copy";

import { AlertRulesContent } from "@/components/alerts/AlertRulesContent";
import { AlertSimulationContent } from "@/components/alerts/AlertSimulationContent";
import { AlertTuningContent } from "@/components/alerts/AlertTuningContent";
import { AlertsInboxContent } from "@/components/alerts/AlertsInboxContent";
import { CompositeAlertRulesContent } from "@/components/alerts/CompositeAlertRulesContent";

import { AdvisorySchedulesContent } from "@/components/advisory/AdvisorySchedulesContent";
import { DigestSubscriptionsContent } from "@/components/digests/DigestSubscriptionsContent";
import GovernanceResolutionPage from "./governance-resolution/page";
import GovernanceWorkflowPage from "./governance/page";
import PolicyPacksPage from "./policy-packs/page";

const emptyGovernanceResolutionPayload = {
  tenantId: "t-ui-shape",
  workspaceId: "w-ui-shape",
  projectId: "p-ui-shape",
  effectiveContent: {
    complianceRuleIds: [] as string[],
    complianceRuleKeys: [] as string[],
    alertRuleIds: [] as string[],
    compositeAlertRuleIds: [] as string[],
    advisoryDefaults: {} as Record<string, string>,
    metadata: {} as Record<string, string>,
  },
  decisions: [] as { itemType: string; itemKey: string }[],
  conflicts: [] as { itemType: string; itemKey: string }[],
  notes: [] as string[],
};

const sampleAlert = {
  alertId: "alert-ui-shape-1",
  ruleId: "rule-1",
  title: "Sample signal",
  category: "Test",
  severity: "High",
  status: "Open",
  triggerValue: "n/a",
  description: "Synthetic row for mutation gate tests.",
  createdUtc: new Date().toISOString(),
};

describe("Enterprise authority UI shaping (mutation hook → controls)", () => {
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
    apiHoisted.listAlertRules.mockResolvedValue([]);
    apiHoisted.listCompositeAlertRules.mockResolvedValue([]);
    apiHoisted.listApprovalRequests.mockResolvedValue([]);
    apiHoisted.listPromotions.mockResolvedValue([]);
    apiHoisted.listActivations.mockResolvedValue([]);
    apiHoisted.getGovernanceResolution.mockResolvedValue(emptyGovernanceResolutionPayload);
    apiHoisted.listDigestSubscriptions.mockResolvedValue([]);
    apiHoisted.listAdvisorySchedules.mockResolvedValue([]);
    apiHoisted.listRunsByProjectPaged.mockResolvedValue({
      items: [{ runId: "gov-ui-shape-run", projectId: "default", description: "UI shape fixture", createdUtc: "" }],
      totalCount: 1,
      page: 1,
      pageSize: 50,
      hasMore: false,
    });
  });

  /**
   * Pack content / lifecycle sits inside {@link AdvancedOptionsAccordion}; Radix keeps closed panel content out of the
   * accessibility tree, so gates on Create pack etc. must open the accordion first (same pattern as governance tests).
   */
  async function expandPolicyPacksAdvancedOptions(): Promise<void> {
    const toggle = screen.getByRole("button", { name: /^Advanced Options$/ });

    fireEvent.click(toggle);

    await waitFor(
      () => {
        expect(toggle).toHaveAttribute("aria-expanded", "true");
      },
      { timeout: 8000 },
    );
  }

  it(
    "Policy packs: Create pack stays disabled when mutation capability is false",
    async () => {
      mutateCapability.current = false;
      render(<PolicyPacksPage />);

      await waitFor(() => {
        expect(screen.getByRole("heading", { name: policyPacksCurrentPacksHeadingReader })).toBeInTheDocument();
      });

      await expandPolicyPacksAdvancedOptions();

      await waitFor(() => {
        expect(screen.getByRole("button", { name: policyPacksCreatePackButtonLabelReaderRank })).toBeDisabled();
      });
    },
    15_000,
  );

  it(
    "Policy packs: inventory headings show inspect framing when mutation capability is false",
    async () => {
      mutateCapability.current = false;
      render(<PolicyPacksPage />);

      await waitFor(() => {
        expect(screen.getByRole("heading", { name: policyPacksCurrentPacksHeadingReader })).toBeInTheDocument();
      });

      await expandPolicyPacksAdvancedOptions();

      expect(screen.getByRole("heading", { name: policyPacksPackContentHeadingReader })).toBeInTheDocument();
    },
    15_000,
  );

  it(
    "Policy packs: Create pack enables after load when mutation capability is true",
    async () => {
      mutateCapability.current = true;
      render(<PolicyPacksPage />);

      await waitFor(() => {
        expect(screen.getByRole("heading", { name: policyPacksCurrentPacksHeadingOperator })).toBeInTheDocument();
      });

      await expandPolicyPacksAdvancedOptions();

      await waitFor(() => {
        expect(screen.getByRole("button", { name: /create pack/i })).not.toBeDisabled();
      });
    },
    15_000,
  );

  it("Alerts inbox: triage preview opens but Confirm stays disabled when mutation capability is false", async () => {
    mutateCapability.current = false;
    render(<AlertsInboxContent />);

    await waitFor(() => {
      expect(screen.getByRole("button", { name: /Acknowledge/ })).not.toBeDisabled();
    });

    expect(screen.getByRole("button", { name: /^Refresh$/ })).toHaveAttribute("title", alertsInboxRefreshButtonTitleReader);

    screen.getByRole("button", { name: /Acknowledge/ }).click();

    await waitFor(() => {
      expect(screen.getByRole("button", { name: alertsTriageDialogConfirmButtonLabelReaderRank })).toBeDisabled();
    });
  });

  it("Alerts inbox: triage Acknowledge enables after load when mutation capability is true", async () => {
    mutateCapability.current = true;
    render(<AlertsInboxContent />);

    await waitFor(() => {
      expect(screen.getByRole("button", { name: /^Acknowledge$/ })).not.toBeDisabled();
    });
  });

  /**
   * **Visibility** vs **Capability:** the Execute+ rank line on **`LayerHeader`** only renders when mutation is on;
   * **`AlertsInboxRankCue`** renders at read tier so the inbox keeps a single `role="note"` write-boundary strip.
   */
  it("Alerts inbox: shows inbox rank cue note when mutation capability is false", async () => {
    mutateCapability.current = false;
    render(<AlertsInboxContent />);

    await waitFor(() => {
      expect(screen.getByRole("button", { name: /Acknowledge/ })).toBeInTheDocument();
    });

    expect(screen.getByText(alertsInboxRankReaderLine)).toBeInTheDocument();
    expect(screen.queryByTestId("layer-header-operate-execute-rank-cue")).toBeNull();
  });

  it("Alerts inbox: shows LayerHeader Execute rank cue when mutation capability is true (inbox cue omitted)", async () => {
    mutateCapability.current = true;
    render(<AlertsInboxContent />);

    await waitFor(() => {
      expect(screen.getByRole("button", { name: /^Acknowledge$/ })).toBeInTheDocument();
    });

    expect(screen.getByTestId("layer-header-operate-execute-rank-cue")).toBeInTheDocument();
    expect(screen.queryByText(alertsInboxRankReaderLine)).toBeNull();
  });

  it("Digest subscriptions: Create subscription stays disabled when mutation capability is false", async () => {
    mutateCapability.current = false;
    render(<DigestSubscriptionsContent />);

    await waitFor(() => {
      expect(apiHoisted.listDigestSubscriptions).toHaveBeenCalled();
    });

    expect(
      screen.getByRole("button", { name: digestSubscriptionsCreateSubscriptionButtonLabelReaderRank }),
    ).toBeDisabled();
  });

  it("Advisory schedules: Create schedule submit stays disabled when mutation capability is false", async () => {
    mutateCapability.current = false;
    render(<AdvisorySchedulesContent />);

    await waitFor(() => {
      expect(apiHoisted.listAdvisorySchedules).toHaveBeenCalled();
    });

    expect(
      screen.getByRole("button", { name: advisorySchedulesCreateScheduleButtonLabelReaderRank }),
    ).toBeDisabled();
  });

  it("Alert tuning: Current tuning heading uses inspect framing when mutation capability is false", () => {
    mutateCapability.current = false;
    render(<AlertTuningContent />);

    expect(screen.getByRole("heading", { name: alertTuningCurrentTuningHeadingReader })).toBeInTheDocument();
  });

  it("Alert simulation: Current behavior heading uses inspect framing when mutation capability is false", () => {
    mutateCapability.current = false;
    render(<AlertSimulationContent />);

    expect(
      screen.getAllByRole("heading", { name: alertSimulationCurrentBehaviorHeadingReader }).length,
    ).toBeGreaterThanOrEqual(1);
  });

  it("Alert rules: Create rule stays disabled when mutation capability is false", async () => {
    mutateCapability.current = false;
    render(<AlertRulesContent />);

    await waitFor(() => {
      expect(screen.getByRole("button", { name: /Create rule \(Execute\+\)/ })).toBeDisabled();
    });

    expect(screen.getByRole("button", { name: /^Refresh$/ })).toHaveAttribute(
      "title",
      alertToolingListRefreshButtonTitleReader,
    );
  });

  it("Alert rules: Create rule enables after load when mutation capability is true", async () => {
    mutateCapability.current = true;
    render(<AlertRulesContent />);

    await waitFor(() => {
      expect(screen.getByRole("button", { name: "Create rule" })).not.toBeDisabled();
    });
  });

  it("Composite alert rules: Create composite rule stays disabled when mutation capability is false", async () => {
    mutateCapability.current = false;
    render(<CompositeAlertRulesContent />);

    await waitFor(() => {
      expect(screen.getByRole("button", { name: compositeRulesCreateButtonLabelReaderRank })).toBeDisabled();
    });
  });

  it("Composite alert rules: Create composite rule enables after load when mutation capability is true", async () => {
    mutateCapability.current = true;
    render(<CompositeAlertRulesContent />);

    await waitFor(() => {
      expect(screen.getByRole("button", { name: "Create composite rule" })).not.toBeDisabled();
    });
  });

  it(
    "Governance workflow: submit Run ID and manifest inputs stay read-only when mutation capability is false",
    async () => {
      mutateCapability.current = false;
      render(<GovernanceWorkflowPage />);

      await waitFor(() => {
        const submitRunTrigger = document.getElementById("gov-submit-run-select") as HTMLButtonElement | null;

        expect(submitRunTrigger).not.toBeNull();
        expect(submitRunTrigger!.disabled).toBe(true);
      });

      expect(screen.getAllByText(governanceWorkflowSubmitCardTitleReader).length).toBeGreaterThanOrEqual(1);
      expect(screen.getByText(governanceWorkflowApprovalRequestsCardTitleReader)).toBeInTheDocument();

      // Promotions / activations `h3` is inside the default-closed `AdvancedOptionsAccordion`; Radix unmounts
      // closed panel content, so open it before querying the heading.
      const advancedToggle = screen.getByRole("button", { name: /^Advanced Options$/ });

      fireEvent.click(advancedToggle);

      await waitFor(
        () => {
          expect(advancedToggle).toHaveAttribute("aria-expanded", "true");
        },
        { timeout: 5000 },
      );

      expect(
        screen.getByRole("heading", { level: 3, name: governanceWorkflowPromotionsActivationsHeadingReader }),
      ).toBeInTheDocument();

      const submitVersion = document.getElementById("gov-submit-version") as HTMLInputElement | null;

      expect(submitVersion).not.toBeNull();
      expect(submitVersion!.readOnly).toBe(true);
      expect(screen.getByTestId("governance-submit-approval-button")).toBeDisabled();
    },
    15_000,
  );

  it("Governance workflow: submit Run ID is editable when mutation capability is true", async () => {
    mutateCapability.current = true;
    render(<GovernanceWorkflowPage />);

    await waitFor(() => {
      const submitRunTrigger = document.getElementById("gov-submit-run-select") as HTMLButtonElement | null;

      expect(submitRunTrigger).not.toBeNull();
      expect(submitRunTrigger!.disabled).toBe(false);
    });

    expect(screen.getByTestId("governance-submit-approval-button")).not.toBeDisabled();
  });

  /**
   * Rank cues (`GovernanceResolutionRankCue`) and this supplement are different seams: outside **`OperatorNavAuthorityProvider`**
   * tests default to Admin rank, but the mutation hook mock can still be **false** — we assert the page wires **soft-disable**
   * copy to **`useEnterpriseMutationCapability`**, not nav rank alone.
   */
  it("Governance resolution: Change related controls shows reader supplement when mutation capability is false", async () => {
    mutateCapability.current = false;
    render(<GovernanceResolutionPage />);

    await waitFor(() => {
      expect(apiHoisted.getGovernanceResolution).toHaveBeenCalled();
    });

    expect(screen.getByText(governanceResolutionChangeRelatedControlsReaderSupplement)).toBeInTheDocument();
    expect(screen.getByRole("heading", { name: governanceResolutionEffectivePolicyHeadingReader })).toBeInTheDocument();

    // Resolution details `h3` lives inside the second default-closed `AdvancedOptionsAccordion` (progressive disclosure).
    const advancedToggles = screen.getAllByRole("button", { name: /^Advanced Options$/ });

    expect(advancedToggles.length).toBeGreaterThanOrEqual(2);
    fireEvent.click(advancedToggles[1]!);

    await waitFor(() => {
      expect(
        screen.getByRole("heading", { name: governanceResolutionResolutionDetailsHeadingReader }),
      ).toBeInTheDocument();
    });
  });

  it("Governance resolution: Change related controls omits reader supplement when mutation capability is true", async () => {
    mutateCapability.current = true;
    render(<GovernanceResolutionPage />);

    await waitFor(() => {
      expect(apiHoisted.getGovernanceResolution).toHaveBeenCalled();
    });

    expect(screen.queryByText(governanceResolutionChangeRelatedControlsReaderSupplement)).toBeNull();
  });

  /** Readers refresh effective policy via GET; **`disabled`** must stay tied to **`loading`**, not mutation rank. */
  it("Governance resolution: Refresh stays enabled when mutation capability is false", async () => {
    mutateCapability.current = false;
    render(<GovernanceResolutionPage />);

    await waitFor(() => {
      expect(screen.getByRole("button", { name: "Refresh" })).toBeInTheDocument();
    });

    expect(screen.getByRole("button", { name: "Refresh" })).not.toBeDisabled();
  });
});
