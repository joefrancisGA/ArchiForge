/**
 * Page-level regression: **`useEnterpriseMutationCapability()`** must actually gate Enterprise write affordances.
 * Lib-level parity lives in **`authority-seam-regression.test.ts`** / **`current-principal.test.ts`**; this file catches
 * inverted `disabled` props, dropped hooks, or pages that stop calling the hook while nav still filters by rank.
 */
import { render, screen, waitFor } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";

const mutateCapability = vi.hoisted(() => ({ current: false }));

vi.mock("@/hooks/use-enterprise-mutation-capability", () => ({
  useEnterpriseMutationCapability: (): boolean => mutateCapability.current,
}));

const apiHoisted = vi.hoisted(() => ({
  listPolicyPacks: vi.fn(),
  getEffectivePolicyPacks: vi.fn(),
  getEffectivePolicyContent: vi.fn(),
  listPolicyPackVersions: vi.fn(),
  listAlertsPaged: vi.fn(),
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

import AlertsPage from "./alerts/page";
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

  it("Alerts inbox: triage Acknowledge stays disabled when mutation capability is false", async () => {
    mutateCapability.current = false;
    render(<AlertsPage />);

    await waitFor(() => {
      expect(screen.getByRole("button", { name: "Acknowledge" })).toBeDisabled();
    });
  });

  it("Alerts inbox: triage Acknowledge enables after load when mutation capability is true", async () => {
    mutateCapability.current = true;
    render(<AlertsPage />);

    await waitFor(() => {
      expect(screen.getByRole("button", { name: "Acknowledge" })).not.toBeDisabled();
    });
  });
});
