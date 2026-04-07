/**
 * Mutable policy-pack graph for E2E mock API (assign + effective-content without a live ArchiForge API).
 */
import { randomUUID } from "node:crypto";

export type MockPolicyPack = {
  policyPackId: string;
  name: string;
  packType: string;
  status: string;
  currentVersion: string;
  description: string;
};

export type MockAssignment = {
  assignmentId: string;
  policyPackId: string;
  version: string;
  scopeLevel: string;
  isPinned: boolean;
  archivedUtc: string | null;
};

const defaultEffectiveContent = {
  complianceRuleIds: [] as string[],
  complianceRuleKeys: [] as string[],
  alertRuleIds: [] as string[],
  compositeAlertRuleIds: [] as string[],
  advisoryDefaults: {} as Record<string, string>,
  metadata: {} as Record<string, string>,
};

let packs: MockPolicyPack[] = [];
let assignments: MockAssignment[] = [];

export function resetPolicyPacksMockState(): void {
  packs = [];
  assignments = [];
}

export function listMockPacks(): MockPolicyPack[] {
  return [...packs];
}

export function createMockPack(body: {
  name: string;
  description?: string;
  packType: string;
  initialContentJson?: string;
}): MockPolicyPack {
  const policyPackId = randomUUID();
  const pack: MockPolicyPack = {
    policyPackId,
    name: body.name?.trim() || "Pack",
    packType: body.packType || "ProjectCustom",
    status: "Draft",
    currentVersion: "1.0.0",
    description: body.description?.trim() ?? "",
  };
  packs.push(pack);
  return pack;
}

export function publishMockVersion(policyPackId: string, version: string, contentJson: string): void {
  const pack = packs.find((p) => p.policyPackId === policyPackId);
  if (!pack) return;
  pack.status = "Active";
  pack.currentVersion = version;
  void contentJson;
}

export function listMockVersions(policyPackId: string): { version: string; contentJson: string; isPublished: boolean }[] {
  const pack = packs.find((p) => p.policyPackId === policyPackId);
  if (!pack) return [];
  return [{ version: pack.currentVersion, contentJson: "{}", isPublished: pack.status === "Active" }];
}

export function assignMockPack(
  policyPackId: string,
  version: string,
  scopeLevel: string,
  isPinned: boolean,
): MockAssignment | null {
  const pack = packs.find((p) => p.policyPackId === policyPackId);
  if (!pack || pack.currentVersion !== version) return null;
  const row: MockAssignment = {
    assignmentId: randomUUID(),
    policyPackId,
    version,
    scopeLevel: scopeLevel || "Project",
    isPinned,
    archivedUtc: null,
  };
  assignments.push(row);
  return row;
}

export function archiveMockAssignment(assignmentId: string): boolean {
  const row = assignments.find((a) => a.assignmentId === assignmentId && !a.archivedUtc);
  if (!row) return false;
  row.archivedUtc = new Date().toISOString();
  return true;
}

export function getMockEffectivePacks(): { packs: { policyPackId: string; version: string }[] } {
  const active = assignments.filter((a) => !a.archivedUtc);
  return {
    packs: active.map((a) => ({ policyPackId: a.policyPackId, version: a.version })),
  };
}

export function getMockEffectiveContent(): typeof defaultEffectiveContent {
  const active = assignments.filter((a) => !a.archivedUtc);
  if (active.length === 0) return { ...defaultEffectiveContent, complianceRuleKeys: [], metadata: {} };

  return {
    ...defaultEffectiveContent,
    complianceRuleKeys: ["e2e-mock-rule"],
    metadata: { e2eMock: "true" },
  };
}
