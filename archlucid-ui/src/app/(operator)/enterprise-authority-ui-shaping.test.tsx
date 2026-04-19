/**
 * Page-level regression: **`useEnterpriseMutationCapability()`** must actually gate Enterprise write affordances.
 * Lib-level parity lives in **`authority-seam-regression.test.ts`** / **`current-principal.test.ts`**; this file catches
 * inverted `disabled` props, dropped hooks, or pages that stop calling the hook while nav still filters by rank.
 *
 * Governance workflow: submit card uses the same hook for read-only fields (`readOnly` / disabled selects) — asserted
 * via DOM attributes, not tooltip copy strings.
 *
 * Governance resolution: **`Change related controls`** reader supplement is driven only by **`useEnterpriseMutationCapability`**
 * (GET **Refresh** stays enabled); rank cues on the same page use **`useNavCallerAuthorityRank`** in production — here the
 * mocked hook isolates the write-boundary copy from **`GovernanceResolutionRankCue`**.
 */
import { render, screen, waitFor } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";

const mutateCapability = vi.hoisted(() => ({ current: false }));

vi.mock("@/hooks/use-enterprise-mutation-capability", () => ({
  useEnterpriseMutationCapability: (): boolean => mutateCapability.current,
}));

vi.mock("next/navigation", () => ({
  useSearchParams: (): URLSearchParams => new URLSearchParams(),
}));

const apiHoisted = vi.hoisted(() => ({
  listPolicyPacks: vi.fn(),
  getEffectivePolicyPacks: vi.fn(),
  getEffectivePolicyContent: vi.fn(),
  listPolicyPackVersions: vi.fn(),
  listAlertsPaged: vi.fn(),
  listAlertRules: vi.fn(),
  listApprovalRequests: vi.fn(),
  listPromotions: vi.fn(),
  listActivations: vi.fn(),
  getGovernanceResolution: vi.fn(),
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
    listAlertRules: apiHoisted.listAlertRules,
    listApprovalRequests: apiHoisted.listApprovalRequests,
    listPromotions: apiHoisted.listPromotions,
    listActivations: apiHoisted.listActivations,
    getGovernanceResolution: apiHoisted.getGovernanceResolution,
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

import { governanceResolutionChangeRelatedControlsReaderSupplement } from "@/lib/enterprise-controls-context-copy";

import AlertRulesPage from "./alert-rules/page";
import AlertsPage from "./alerts/page";
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
    apiHoisted.listApprovalRequests.mockResolvedValue([]);
    apiHoisted.listPromotions.mockResolvedValue([]);
    apiHoisted.listActivations.mockResolvedValue([]);
    apiHoisted.getGovernanceResolution.mockResolvedValue(emptyGovernanceResolutionPayload);
  });

  it("Policy packs: Create pack stays disabled when mutation capability is false", async () => {
    mutateCapability.current = false;
    render(<PolicyPacksPage />);

    await waitFor(() => {
      expect(screen.getByRole("button", { name: /create pack/i })).toBeDisabled();
    });
  });

  it("Policy packs: Create pack enables after load when mutation capability is true", async () => {
    mutateCapability.current = true;
    render(<PolicyPacksPage />);

    await waitFor(() => {
      expect(screen.getByRole("button", { name: /create pack/i })).not.toBeDisabled();
    });
  });

  it("Alerts inbox: triage preview opens but Confirm stays disabled when mutation capability is false", async () => {
    mutateCapability.current = false;
    render(<AlertsPage />);

    await waitFor(() => {
      expect(screen.getByRole("button", { name: /Acknowledge/ })).not.toBeDisabled();
    });

    screen.getByRole("button", { name: /Acknowledge/ }).click();

    await waitFor(() => {
      expect(screen.getByRole("button", { name: "Confirm" })).toBeDisabled();
    });
  });

  it("Alerts inbox: triage Acknowledge enables after load when mutation capability is true", async () => {
    mutateCapability.current = true;
    render(<AlertsPage />);

    await waitFor(() => {
      expect(screen.getByRole("button", { name: /^Acknowledge$/ })).not.toBeDisabled();
    });
  });

  /**
   * Rank cue is the second `role="note"` strip (LayerHeader is always first). If mutation capability flips true but the
   * cue is not removed, Reader cognitive load regresses; if false without cue, write boundary copy disappears.
   */
  it("Alerts inbox: shows LayerHeader plus inbox rank cue notes when mutation capability is false", async () => {
    mutateCapability.current = false;
    render(<AlertsPage />);

    await waitFor(() => {
      expect(screen.getByRole("button", { name: /Acknowledge/ })).toBeInTheDocument();
    });

    expect(screen.getAllByRole("note")).toHaveLength(2);
  });

  it("Alerts inbox: omits inbox rank cue note when mutation capability is true (LayerHeader note only)", async () => {
    mutateCapability.current = true;
    render(<AlertsPage />);

    await waitFor(() => {
      expect(screen.getByRole("button", { name: /^Acknowledge$/ })).toBeInTheDocument();
    });

    expect(screen.getAllByRole("note")).toHaveLength(1);
  });

  it("Alert rules: Create rule stays disabled when mutation capability is false", async () => {
    mutateCapability.current = false;
    render(<AlertRulesPage />);

    await waitFor(() => {
      expect(screen.getByRole("button", { name: /Create rule \(Execute\+\)/ })).toBeDisabled();
    });
  });

  it("Alert rules: Create rule enables after load when mutation capability is true", async () => {
    mutateCapability.current = true;
    render(<AlertRulesPage />);

    await waitFor(() => {
      expect(screen.getByRole("button", { name: "Create rule" })).not.toBeDisabled();
    });
  });

  it("Governance workflow: submit Run ID and manifest inputs stay read-only when mutation capability is false", async () => {
    mutateCapability.current = false;
    render(<GovernanceWorkflowPage />);

    await waitFor(() => {
      const submitRun = document.getElementById("gov-submit-run") as HTMLInputElement | null;

      expect(submitRun).not.toBeNull();
      expect(submitRun!.readOnly).toBe(true);
    });

    const submitVersion = document.getElementById("gov-submit-version") as HTMLInputElement | null;

    expect(submitVersion).not.toBeNull();
    expect(submitVersion!.readOnly).toBe(true);
    expect(screen.getByRole("button", { name: /submit for approval/i })).toBeDisabled();
  });

  it("Governance workflow: submit Run ID is editable when mutation capability is true", async () => {
    mutateCapability.current = true;
    render(<GovernanceWorkflowPage />);

    await waitFor(() => {
      const submitRun = document.getElementById("gov-submit-run") as HTMLInputElement | null;

      expect(submitRun).not.toBeNull();
      expect(submitRun!.readOnly).toBe(false);
    });

    expect(screen.getByRole("button", { name: /submit for approval/i })).not.toBeDisabled();
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
