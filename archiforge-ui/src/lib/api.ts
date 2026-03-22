import { ApiV1Routes } from "@/lib/api-v1-routes";
import { getServerApiBaseUrl } from "@/lib/config";
import { AUTH_MODE } from "@/lib/auth-config";
import { getScopeHeaders } from "@/lib/scope";
import type { GoldenManifestComparison } from "@/types/comparison";
import type { ComparisonExplanation, RunExplanation } from "@/types/explanation";
import type {
  ArtifactDescriptor,
  ManifestSummary,
  ReplayResponse,
  RunComparison,
  RunDetail,
  RunSummary,
} from "@/types/authority";
import type {
  AskResponse,
  ConversationMessage,
  ConversationThread,
} from "@/types/conversation";
import type { ImprovementPlan } from "@/types/advisory";
import type { LearningProfile } from "@/types/recommendation-learning";
import type {
  AdvisoryScanExecution,
  AdvisoryScanSchedule,
  ArchitectureDigest,
} from "@/types/advisory-scheduling";
import type { DigestDeliveryAttempt, DigestSubscription } from "@/types/digest-subscriptions";
import type { AlertRecord, AlertRule } from "@/types/alerts";
import type { AlertRoutingDeliveryAttempt, AlertRoutingSubscription } from "@/types/alert-routing";
import type { CompositeAlertRule } from "@/types/composite-alert-rules";
import type {
  RuleCandidateComparisonResult,
  RuleSimulationResult,
} from "@/types/alert-simulation";
import type { ThresholdRecommendationResult } from "@/types/alert-tuning";
import type {
  EffectivePolicyPackSet,
  PolicyPack,
  PolicyPackAssignment,
  PolicyPackContentDocument,
  PolicyPackVersion,
} from "@/types/policy-packs";

function isBrowser(): boolean {
  return typeof window !== "undefined";
}

/**
 * Resolve request URL and headers.
 * - Server (RSC): direct backend + optional ARCHIFORGE_API_KEY.
 * - Browser: same-origin `/api/proxy` (adds X-Api-Key on the server).
 */
/**
 * Future: return an access token from secure storage / session when using JWT against the API.
 * The proxy can also forward an Authorization header from the browser when you set it on fetch.
 */
function getBearerToken(): string | undefined {
  if (typeof window === "undefined") return undefined;
  if (AUTH_MODE !== "jwt" && AUTH_MODE !== "jwt-bearer") return undefined;
  return undefined;
}

function resolveRequest(path: string): { url: string; headers: HeadersInit } {
  if (isBrowser()) {
    const url = `/api/proxy${path.startsWith("/") ? path : `/${path}`}`;
    const headers: Record<string, string> = {
      Accept: "application/json",
    };
    const bearer = getBearerToken();
    if (bearer) headers.Authorization = `Bearer ${bearer}`;
    return { url, headers };
  }

  const base = getServerApiBaseUrl().replace(/\/$/, "");
  const url = `${base}${path.startsWith("/") ? path : `/${path}`}`;
  const headers: Record<string, string> = {
    Accept: "application/json",
    ...getScopeHeaders(),
  };
  const key = process.env.ARCHIFORGE_API_KEY;
  if (key) headers["X-Api-Key"] = key;
  return { url, headers };
}

export async function apiGet<T>(path: string): Promise<T> {
  const { url, headers } = resolveRequest(path);
  const response = await fetch(url, {
    cache: "no-store",
    headers,
  });

  if (!response.ok) {
    throw new Error(`Request failed: ${response.status} ${response.statusText}`);
  }

  return response.json() as Promise<T>;
}

export async function apiPostJson<T>(path: string, body: unknown): Promise<T> {
  const { url, headers } = resolveRequest(path);
  const h = new Headers(headers);
  h.set("Content-Type", "application/json");
  const response = await fetch(url, {
    method: "POST",
    headers: h,
    cache: "no-store",
    body: JSON.stringify(body),
  });

  if (!response.ok) {
    let detail = "";
    try {
      const err = (await response.json()) as { error?: string };
      if (err?.error) detail = `: ${err.error}`;
    } catch {
      /* ignore */
    }
    throw new Error(`Request failed: ${response.status} ${response.statusText}${detail}`);
  }

  return response.json() as Promise<T>;
}

/** Same proxy/scope/API-key behavior as other UI API calls; for graph modules, etc. */
export async function fetchArchiForgeJson<T>(path: string): Promise<T> {
  return apiGet<T>(path);
}

export async function listRunsByProject(projectId: string, take = 20): Promise<RunSummary[]> {
  return apiGet<RunSummary[]>(
    `/api/authority/projects/${encodeURIComponent(projectId)}/runs?take=${take}`,
  );
}

export async function getRunSummary(runId: string): Promise<RunSummary> {
  return apiGet<RunSummary>(`/api/authority/runs/${runId}/summary`);
}

export async function getRunDetail(runId: string): Promise<RunDetail> {
  return apiGet<RunDetail>(`/api/authority/runs/${runId}`);
}

export async function getManifestSummary(manifestId: string): Promise<ManifestSummary> {
  return apiGet<ManifestSummary>(`/api/authority/manifests/${manifestId}/summary`);
}

export async function listArtifacts(manifestId: string): Promise<ArtifactDescriptor[]> {
  return apiGet<ArtifactDescriptor[]>(`/api/artifacts/manifests/${manifestId}`);
}

export async function compareRuns(leftRunId: string, rightRunId: string): Promise<RunComparison> {
  return apiGet<RunComparison>(
    `/api/authority/compare/runs?leftRunId=${encodeURIComponent(leftRunId)}&rightRunId=${encodeURIComponent(rightRunId)}`,
  );
}

export async function compareGoldenManifestRuns(
  baseRunId: string,
  targetRunId: string,
): Promise<GoldenManifestComparison> {
  return apiGet<GoldenManifestComparison>(
    `/api/compare?baseRunId=${encodeURIComponent(baseRunId)}&targetRunId=${encodeURIComponent(targetRunId)}`,
  );
}

export async function explainComparisonRuns(
  baseRunId: string,
  targetRunId: string,
): Promise<ComparisonExplanation> {
  return apiGet<ComparisonExplanation>(
    `/api/explain/compare/explain?baseRunId=${encodeURIComponent(baseRunId)}&targetRunId=${encodeURIComponent(targetRunId)}`,
  );
}

export async function explainRun(runId: string): Promise<RunExplanation> {
  return apiGet<RunExplanation>(`/api/explain/runs/${encodeURIComponent(runId)}/explain`);
}

export async function askArchiForge(payload: {
  threadId?: string;
  runId?: string;
  question: string;
  baseRunId?: string;
  targetRunId?: string;
}): Promise<AskResponse> {
  const body: Record<string, unknown> = {
    question: payload.question,
  };
  if (payload.threadId?.trim()) body.threadId = payload.threadId.trim();
  if (payload.runId?.trim()) body.runId = payload.runId.trim();
  if (payload.baseRunId?.trim()) body.baseRunId = payload.baseRunId.trim();
  if (payload.targetRunId?.trim()) body.targetRunId = payload.targetRunId.trim();

  return apiPostJson<AskResponse>("/api/ask", body);
}

export async function listConversationThreads(take = 50): Promise<ConversationThread[]> {
  return apiGet(`/api/conversations?take=${take}`);
}

export async function getConversationMessages(threadId: string, take = 200): Promise<ConversationMessage[]> {
  return apiGet(`/api/conversations/${encodeURIComponent(threadId)}/messages?take=${take}`);
}

export async function getImprovementPlan(runId: string, compareToRunId?: string): Promise<ImprovementPlan> {
  const params = new URLSearchParams();
  if (compareToRunId?.trim()) params.set("compareToRunId", compareToRunId.trim());
  const q = params.toString();
  return apiGet<ImprovementPlan>(
    `/api/advisory/runs/${encodeURIComponent(runId)}/improvements${q ? `?${q}` : ""}`,
  );
}

export async function getLatestLearningProfile(): Promise<LearningProfile | null> {
  const { url, headers } = resolveRequest("/api/recommendation-learning/latest");
  const response = await fetch(url, { cache: "no-store", headers });
  if (response.status === 404) return null;
  if (!response.ok) {
    throw new Error(`Request failed: ${response.status} ${response.statusText}`);
  }
  return response.json() as Promise<LearningProfile>;
}

export async function listAdvisorySchedules(): Promise<AdvisoryScanSchedule[]> {
  return apiGet<AdvisoryScanSchedule[]>("/api/advisory-scheduling/schedules");
}

export async function createAdvisorySchedule(body: {
  name: string;
  cronExpression: string;
  runProjectSlug?: string;
  isEnabled?: boolean;
}): Promise<AdvisoryScanSchedule> {
  return apiPostJson<AdvisoryScanSchedule>("/api/advisory-scheduling/schedules", {
    name: body.name,
    cronExpression: body.cronExpression,
    runProjectSlug: body.runProjectSlug?.trim() || "default",
    isEnabled: body.isEnabled ?? true,
  });
}

export async function runAdvisoryScheduleNow(scheduleId: string): Promise<void> {
  const { url, headers } = resolveRequest(
    `/api/advisory-scheduling/schedules/${encodeURIComponent(scheduleId)}/run`,
  );
  const h = new Headers(headers);
  h.set("Content-Type", "application/json");
  const response = await fetch(url, { method: "POST", headers: h, cache: "no-store" });
  if (!response.ok) {
    throw new Error(`Run now failed: ${response.status} ${response.statusText}`);
  }
}

export async function listScheduleExecutions(
  scheduleId: string,
  take = 30,
): Promise<AdvisoryScanExecution[]> {
  return apiGet<AdvisoryScanExecution[]>(
    `/api/advisory-scheduling/schedules/${encodeURIComponent(scheduleId)}/executions?take=${take}`,
  );
}

export async function listArchitectureDigests(take = 20): Promise<ArchitectureDigest[]> {
  return apiGet<ArchitectureDigest[]>(`/api/advisory-scheduling/digests?take=${take}`);
}

export async function listDigestSubscriptions(): Promise<DigestSubscription[]> {
  return apiGet<DigestSubscription[]>(`/${ApiV1Routes.digestSubscriptions}`);
}

export async function createDigestSubscription(body: {
  name: string;
  channelType: string;
  destination: string;
  isEnabled?: boolean;
  metadataJson?: string;
}): Promise<DigestSubscription> {
  return apiPostJson<DigestSubscription>(`/${ApiV1Routes.digestSubscriptions}`, {
    name: body.name,
    channelType: body.channelType,
    destination: body.destination,
    isEnabled: body.isEnabled ?? true,
    metadataJson: body.metadataJson ?? "{}",
  });
}

export async function toggleDigestSubscription(subscriptionId: string): Promise<DigestSubscription> {
  return apiPostJson<DigestSubscription>(
    `/api/digest-subscriptions/${encodeURIComponent(subscriptionId)}/toggle`,
    {},
  );
}

export async function listSubscriptionDeliveryAttempts(
  subscriptionId: string,
  take = 50,
): Promise<DigestDeliveryAttempt[]> {
  return apiGet<DigestDeliveryAttempt[]>(
    `/${ApiV1Routes.digestSubscriptions}/${encodeURIComponent(subscriptionId)}/attempts?take=${take}`,
  );
}

export async function listDigestDeliveryAttempts(digestId: string): Promise<DigestDeliveryAttempt[]> {
  return apiGet<DigestDeliveryAttempt[]>(
    `/${ApiV1Routes.digestSubscriptions}/digests/${encodeURIComponent(digestId)}/attempts`,
  );
}

export async function getArchitectureDigest(digestId: string): Promise<ArchitectureDigest> {
  return apiGet<ArchitectureDigest>(
    `/api/advisory-scheduling/digests/${encodeURIComponent(digestId)}`,
  );
}

export async function listAlertRules(): Promise<AlertRule[]> {
  return apiGet<AlertRule[]>(`/${ApiV1Routes.alertRules}`);
}

export async function createAlertRule(body: {
  name: string;
  ruleType: string;
  severity: string;
  thresholdValue: number;
  isEnabled?: boolean;
  targetChannelType?: string;
  metadataJson?: string;
}): Promise<AlertRule> {
  return apiPostJson<AlertRule>(`/${ApiV1Routes.alertRules}`, {
    name: body.name,
    ruleType: body.ruleType,
    severity: body.severity,
    thresholdValue: body.thresholdValue,
    isEnabled: body.isEnabled ?? true,
    targetChannelType: body.targetChannelType ?? "DigestOnly",
    metadataJson: body.metadataJson ?? "{}",
  });
}

export async function listAlerts(status: string | null, take = 100): Promise<AlertRecord[]> {
  const q = new URLSearchParams();
  if (status) q.set("status", status);
  q.set("take", String(take));
  const suffix = q.toString();
  return apiGet<AlertRecord[]>(`/v1/alerts${suffix ? `?${suffix}` : ""}`);
}

export async function applyAlertAction(
  alertId: string,
  action: "Acknowledge" | "Resolve" | "Suppress",
  comment?: string,
): Promise<AlertRecord> {
  return apiPostJson<AlertRecord>(`/${ApiV1Routes.alerts}/${encodeURIComponent(alertId)}/action`, {
    action,
    comment: comment ?? "",
  });
}

export async function listAlertRoutingSubscriptions(): Promise<AlertRoutingSubscription[]> {
  return apiGet<AlertRoutingSubscription[]>("/v1/alert-routing-subscriptions");
}

export async function createAlertRoutingSubscription(body: {
  name: string;
  channelType: string;
  destination: string;
  minimumSeverity: string;
  isEnabled?: boolean;
  metadataJson?: string;
}): Promise<AlertRoutingSubscription> {
  return apiPostJson<AlertRoutingSubscription>(`/${ApiV1Routes.alertRoutingSubscriptions}`, {
    name: body.name,
    channelType: body.channelType,
    destination: body.destination,
    minimumSeverity: body.minimumSeverity,
    isEnabled: body.isEnabled ?? true,
    metadataJson: body.metadataJson ?? "{}",
  });
}

export async function toggleAlertRoutingSubscription(
  routingSubscriptionId: string,
): Promise<AlertRoutingSubscription> {
  return apiPostJson<AlertRoutingSubscription>(
    `/v1/alert-routing-subscriptions/${encodeURIComponent(routingSubscriptionId)}/toggle`,
    {},
  );
}

export async function listAlertRoutingDeliveryAttempts(
  routingSubscriptionId: string,
  take = 30,
): Promise<AlertRoutingDeliveryAttempt[]> {
  return apiGet<AlertRoutingDeliveryAttempt[]>(
    `/${ApiV1Routes.alertRoutingSubscriptions}/${encodeURIComponent(routingSubscriptionId)}/attempts?take=${take}`,
  );
}

export async function listCompositeAlertRules(): Promise<CompositeAlertRule[]> {
  return apiGet<CompositeAlertRule[]>(`/${ApiV1Routes.compositeAlertRules}`);
}

export async function simulateAlertRule(body: {
  ruleKind: string;
  simpleRule?: Record<string, unknown> | null;
  compositeRule?: Record<string, unknown> | null;
  runId?: string | null;
  comparedToRunId?: string | null;
  recentRunCount?: number;
  useHistoricalWindow?: boolean;
  runProjectSlug?: string;
}): Promise<RuleSimulationResult> {
  return apiPostJson<RuleSimulationResult>("/v1/alert-simulation/simulate", body);
}

export async function listPolicyPacks(): Promise<PolicyPack[]> {
  return apiGet<PolicyPack[]>(`/${ApiV1Routes.policyPacks}`);
}

export async function listPolicyPackVersions(policyPackId: string): Promise<PolicyPackVersion[]> {
  return apiGet<PolicyPackVersion[]>(
    `/${ApiV1Routes.policyPacks}/${encodeURIComponent(policyPackId)}/versions`,
  );
}

export async function getEffectivePolicyPacks(): Promise<EffectivePolicyPackSet> {
  return apiGet<EffectivePolicyPackSet>(`/${ApiV1Routes.policyPacks}/effective`);
}

export async function getEffectivePolicyContent(): Promise<PolicyPackContentDocument> {
  return apiGet<PolicyPackContentDocument>(`/${ApiV1Routes.policyPacks}/effective-content`);
}

export async function createPolicyPack(body: {
  name: string;
  description?: string;
  packType: string;
  initialContentJson?: string;
}): Promise<PolicyPack> {
  return apiPostJson<PolicyPack>(`/${ApiV1Routes.policyPacks}`, body);
}

export async function publishPolicyPackVersion(
  policyPackId: string,
  body: { version: string; contentJson?: string },
): Promise<PolicyPackVersion> {
  return apiPostJson<PolicyPackVersion>(
    `/${ApiV1Routes.policyPacks}/${encodeURIComponent(policyPackId)}/publish`,
    body,
  );
}

export async function assignPolicyPack(
  policyPackId: string,
  body: { version: string },
): Promise<PolicyPackAssignment> {
  return apiPostJson<PolicyPackAssignment>(
    `/${ApiV1Routes.policyPacks}/${encodeURIComponent(policyPackId)}/assign`,
    body,
  );
}

export async function recommendAlertThreshold(body: {
  ruleKind: string;
  tunedMetricType: string;
  candidateThresholds: number[];
  recentRunCount?: number;
  targetCreatedAlertCountMin?: number;
  targetCreatedAlertCountMax?: number;
  runProjectSlug?: string;
  baseSimpleRule?: Record<string, unknown> | null;
  baseCompositeRule?: Record<string, unknown> | null;
}): Promise<ThresholdRecommendationResult> {
  return apiPostJson<ThresholdRecommendationResult>("/v1/alert-tuning/recommend-threshold", body);
}

export async function compareAlertRuleCandidates(body: {
  ruleKind: string;
  candidateA_SimpleRule?: Record<string, unknown> | null;
  candidateB_SimpleRule?: Record<string, unknown> | null;
  candidateA_CompositeRule?: Record<string, unknown> | null;
  candidateB_CompositeRule?: Record<string, unknown> | null;
  recentRunCount?: number;
  runProjectSlug?: string;
}): Promise<RuleCandidateComparisonResult> {
  return apiPostJson<RuleCandidateComparisonResult>(
    `/${ApiV1Routes.alertSimulation}/compare-candidates`,
    body,
  );
}

export async function createCompositeAlertRule(body: {
  name: string;
  severity: string;
  operator: string;
  suppressionWindowMinutes: number;
  cooldownMinutes: number;
  reopenDeltaThreshold: number;
  dedupeScope: string;
  isEnabled?: boolean;
  targetChannelType?: string;
  conditions: { metricType: string; operator: string; thresholdValue: number }[];
}): Promise<CompositeAlertRule> {
  return apiPostJson<CompositeAlertRule>(`/${ApiV1Routes.compositeAlertRules}`, {
    name: body.name,
    severity: body.severity,
    operator: body.operator,
    isEnabled: body.isEnabled ?? true,
    suppressionWindowMinutes: body.suppressionWindowMinutes,
    cooldownMinutes: body.cooldownMinutes,
    reopenDeltaThreshold: body.reopenDeltaThreshold,
    dedupeScope: body.dedupeScope,
    targetChannelType: body.targetChannelType ?? "AlertRouting",
    conditions: body.conditions.map((c) => ({
      metricType: c.metricType,
      operator: c.operator,
      thresholdValue: c.thresholdValue,
    })),
  });
}

export async function rebuildLearningProfile(): Promise<LearningProfile> {
  const { url, headers } = resolveRequest("/api/recommendation-learning/rebuild");
  const h = new Headers(headers);
  h.set("Content-Type", "application/json");
  const response = await fetch(url, {
    method: "POST",
    headers: h,
    cache: "no-store",
  });
  if (!response.ok) {
    throw new Error(`Request failed: ${response.status} ${response.statusText}`);
  }
  return response.json() as Promise<LearningProfile>;
}

export async function replayRun(runId: string, mode: string): Promise<ReplayResponse> {
  const { url, headers } = resolveRequest("/api/authority/replay");
  const h = new Headers(headers);
  h.set("Content-Type", "application/json");
  const response = await fetch(url, {
    method: "POST",
    headers: h,
    cache: "no-store",
    body: JSON.stringify({ runId, mode }),
  });

  if (!response.ok) {
    throw new Error(`Replay failed: ${response.status} ${response.statusText}`);
  }

  return response.json() as Promise<ReplayResponse>;
}

/** Use same-origin proxy so browser downloads work with API key auth. */
export function getArtifactDownloadUrl(manifestId: string, artifactId: string): string {
  return `/api/proxy/api/artifacts/manifests/${manifestId}/artifact/${artifactId}`;
}

export function getBundleDownloadUrl(manifestId: string): string {
  return `/api/proxy/api/artifacts/manifests/${manifestId}/bundle`;
}

export function getRunExportDownloadUrl(runId: string): string {
  return `/api/proxy/api/artifacts/runs/${runId}/export`;
}

/** DOCX package; optional compare + AI narrative flags. */
export function getArchitecturePackageDocxUrl(
  runId: string,
  compareWithRunId?: string,
  opts?: { explainRun?: boolean; includeComparisonExplanation?: boolean },
): string {
  const params = new URLSearchParams();
  if (compareWithRunId?.trim())
    params.set("compareWithRunId", compareWithRunId.trim());
  if (opts?.explainRun) params.set("explainRun", "true");
  if (opts?.includeComparisonExplanation === false)
    params.set("includeComparisonExplanation", "false");
  const q = params.toString();
  return `/api/proxy/api/docx/runs/${runId}/architecture-package${q ? `?${q}` : ""}`;
}
