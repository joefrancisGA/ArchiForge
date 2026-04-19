/**
 * Page-level regression: **`useEnterpriseMutationCapability()`** must actually gate Enterprise write affordances.
 * Lib-level parity lives in **`authority-seam-regression.test.ts`** / **`current-principal.test.ts`**; this file catches
 * inverted `disabled` props, dropped hooks, or pages that stop calling the hook while nav still filters by rank.
 *
 * Governance workflow: submit card uses the same hook for read-only fields (`readOnly` / disabled selects) — asserted
 * via DOM attributes, not tooltip copy strings.
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

import AlertRulesPage from "./alert-rules/page";
import AlertsPage from "./alerts/page";
import GovernanceWorkflowPage from "./governance/page";
import PolicyPacksPage from "./policy-packs/page";

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
});
