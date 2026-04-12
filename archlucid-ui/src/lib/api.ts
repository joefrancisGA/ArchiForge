import { buildApiRequestErrorFromParts } from "@/lib/api-error";
import { ApiV1Routes } from "@/lib/api-v1-routes";
import { CORRELATION_ID_HEADER, generateCorrelationId } from "@/lib/correlation";
import { getServerApiBaseUrl } from "@/lib/config";
import { readServerSideApiKey } from "@/lib/legacy-arch-env";
import { isJwtAuthMode } from "@/lib/oidc/config";
import { ensureAccessTokenFresh, getAccessTokenForApi } from "@/lib/oidc/session";
import { getScopeHeaders } from "@/lib/scope";
import type { GoldenManifestComparison } from "@/types/comparison";
import type {
  ComparisonExplanation,
  RunExplanation,
  RunExplanationSummary,
} from "@/types/explanation";
import type {
  ArtifactDescriptor,
  DecisionProvenanceGraph,
  ManifestSummary,
  ReplayResponse,
  RunComparison,
  RunDetail,
  RunSummary,
} from "@/types/authority";
import type { PagedResponse } from "@/types/pagination";
import type {
  AskResponse,
  ConversationMessage,
  ConversationThread,
} from "@/types/conversation";
import type { ArchitectureRunProvenanceGraph } from "@/types/architecture-provenance";
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
import type { EffectiveGovernanceResolutionResult } from "@/types/governance-resolution";
import type { GovernanceDashboardSummary } from "@/types/governance-dashboard";
import type {
  GovernanceApprovalRequest,
  GovernanceEnvironmentActivation,
  GovernancePromotionRecord,
} from "@/types/governance-workflow";
import type { ProductLearningDashboardBundle } from "@/types/product-learning";
import type {
  LearningPlanDetailResponse,
  LearningPlansListResponse,
  LearningSummaryResponse,
  LearningThemesListResponse,
} from "@/types/learning";
import type {
  EvolutionCandidateChangeSetListResponse,
  EvolutionResultsResponse,
  EvolutionSimulateResponse,
} from "@/types/evolution";

/** JSON GET result plus optional distributed trace id from the upstream `X-Trace-Id` response header. */
export interface ApiResponseWithTrace<T> {
  data: T;
  traceId: string | null;
}

/** Returns the trace id from the `X-Trace-Id` response header, or null if absent. */
export function extractTraceId(response: Response): string | null {
  return response.headers.get("X-Trace-Id") ?? null;
}

/** Returns true when executing in the browser (client component), false on the Node.js server (RSC). */
function isBrowser(): boolean {
  return typeof window !== "undefined";
}

async function ensureOidcBearerReady(): Promise<void> {
  if (isBrowser() && isJwtAuthMode()) {
    await ensureAccessTokenFresh();
  }
}

/**
 * Returns a bearer token for JWT-based API auth when running in the browser (OIDC session).
 */
function getBearerToken(): string | undefined {
  if (typeof window === "undefined") {
    return undefined;
  }

  if (!isJwtAuthMode()) {
    return undefined;
  }

  return getAccessTokenForApi();
}

/**
 * Same routing as JSON calls, but Accept allows binary artifact bodies (UTF-8 text from synthesis).
 */
function resolveBinaryGetRequest(path: string): { url: string; headers: HeadersInit } {
  if (isBrowser()) {
    const url = `/api/proxy${path.startsWith("/") ? path : `/${path}`}`;
    const headers: Record<string, string> = {
      Accept: "*/*",
    };
    const bearer = getBearerToken();

    if (bearer) {
      headers.Authorization = `Bearer ${bearer}`;
    }

    return { url, headers };
  }

  const base = getServerApiBaseUrl().replace(/\/$/, "");
  const url = `${base}${path.startsWith("/") ? path : `/${path}`}`;
  const headers: Record<string, string> = {
    Accept: "*/*",
    ...getScopeHeaders(),
  };
  const key = readServerSideApiKey();

  if (key) {
    headers["X-Api-Key"] = key;
  }

  return { url, headers };
}

/**
 * Builds URL + headers for a JSON GET/POST.
 * Server (RSC): direct to backend with API key + scope headers.
 * Browser: same-origin `/api/proxy` so secrets stay server-side.
 */
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
  const key = readServerSideApiKey();
  if (key) headers["X-Api-Key"] = key;
  return { url, headers };
}

function withCorrelationHeaders(headers: HeadersInit): Headers {
  const h = new Headers(headers);
  h.set(CORRELATION_ID_HEADER, generateCorrelationId());

  return h;
}

async function apiGetJsonWithTrace<T>(path: string): Promise<ApiResponseWithTrace<T>> {
  await ensureOidcBearerReady();
  const { url, headers } = resolveRequest(path);
  const h = withCorrelationHeaders(headers);
  const response = await fetch(url, {
    cache: "no-store",
    headers: h,
  });
  const text = await response.text();
  const traceId = extractTraceId(response);

  if (!response.ok) {
    throw buildApiRequestErrorFromParts(response, text);
  }

  return { data: JSON.parse(text) as T, traceId };
}

/** GETs JSON from the ArchLucid API. Throws {@link ApiRequestError} on HTTP errors. */
export async function apiGet<T>(path: string): Promise<T> {
  const { data } = await apiGetJsonWithTrace<T>(path);

  return data;
}

/** POSTs a JSON body to the ArchLucid API and returns the parsed response. Throws on HTTP errors. */
export async function apiPostJson<T>(path: string, body: unknown): Promise<T> {
  await ensureOidcBearerReady();
  const { url, headers } = resolveRequest(path);
  const h = withCorrelationHeaders(headers);
  h.set("Content-Type", "application/json");
  const response = await fetch(url, {
    method: "POST",
    headers: h,
    cache: "no-store",
    body: JSON.stringify(body),
  });
  const text = await response.text();

  if (!response.ok) {
    throw buildApiRequestErrorFromParts(response, text);
  }

  return JSON.parse(text) as T;
}

/** Same proxy/scope/API-key behavior as other UI API calls; for graph modules, etc. */
export async function fetchArchLucidJson<T>(path: string): Promise<T> {
  return apiGet<T>(path);
}

/** Attached context document (camelCase JSON — matches API `ContextDocumentRequest`). */
export type CreateArchitectureRunDocumentPayload = {
  name: string;
  contentType: string;
  content: string;
};

/** IaC / declaration blob (camelCase JSON — matches API `InfrastructureDeclarationRequest`). */
export type CreateArchitectureRunInfrastructureDeclarationPayload = {
  name: string;
  format: string;
  content: string;
};

/** Body shape for POST /v1/architecture/request (operator wizard + full `ArchitectureRequest` surface). */
export type CreateArchitectureRunRequestPayload = {
  requestId: string;
  description: string;
  systemName: string;
  environment: string;
  cloudProvider: "Azure" | "Aws" | "Gcp";
  constraints: string[];
  requiredCapabilities: string[];
  assumptions: string[];
  priorManifestVersion?: string;
  inlineRequirements?: string[];
  documents?: CreateArchitectureRunDocumentPayload[];
  policyReferences?: string[];
  topologyHints?: string[];
  securityBaselineHints?: string[];
  infrastructureDeclarations?: CreateArchitectureRunInfrastructureDeclarationPayload[];
};

/** Response envelope for create run (subset used by the UI). */
export type CreateArchitectureRunResponsePayload = {
  run?: { runId?: string; status?: string; requestId?: string };
  tasks?: { taskId?: string; agentType?: string }[];
};

/** Submits a new architecture run (POST /v1/architecture/request). */
export async function createArchitectureRun(
  body: CreateArchitectureRunRequestPayload,
): Promise<CreateArchitectureRunResponsePayload> {
  return apiPostJson<CreateArchitectureRunResponsePayload>("/v1/architecture/request", body);
}

/** Linkage graph + trace timeline for a coordinator architecture run. */
export async function getArchitectureRunProvenance(
  runId: string,
): Promise<ApiResponseWithTrace<ArchitectureRunProvenanceGraph>> {
  return apiGetJsonWithTrace<ArchitectureRunProvenanceGraph>(
    `/v1/architecture/runs/${encodeURIComponent(runId)}/provenance`,
  );
}

/** Lists recent runs for a project (GET /v1/authority/projects/{id}/runs). */
export async function listRunsByProject(projectId: string, take = 20): Promise<RunSummary[]> {
  return apiGet<RunSummary[]>(
    `/v1/authority/projects/${encodeURIComponent(projectId)}/runs?take=${take}`,
  );
}

/** Paged runs for a project (GET with `page` + `pageSize` — returns PagedResponse). */
export async function listRunsByProjectPaged(
  projectId: string,
  page: number,
  pageSize: number,
): Promise<PagedResponse<RunSummary>> {
  const q = new URLSearchParams();
  q.set("page", String(page));
  q.set("pageSize", String(pageSize));

  return apiGet<PagedResponse<RunSummary>>(
    `/v1/authority/projects/${encodeURIComponent(projectId)}/runs?${q}`,
  );
}

/** Fetches the lightweight summary for a single run. */
export async function getRunSummary(runId: string): Promise<RunSummary> {
  return apiGet<RunSummary>(`/v1/authority/runs/${runId}/summary`);
}

/** Fetches the full run detail envelope (run metadata, snapshots, manifest, trace, bundle). */
export async function getRunDetail(runId: string): Promise<ApiResponseWithTrace<RunDetail>> {
  return apiGetJsonWithTrace<RunDetail>(`/v1/authority/runs/${runId}`);
}

/** Structural provenance graph for a completed authority run (422 if snapshots incomplete). */
export async function getRunProvenance(runId: string): Promise<DecisionProvenanceGraph> {
  return apiGet<DecisionProvenanceGraph>(`/v1/authority/runs/${runId}/provenance`);
}

/** Fetches golden manifest summary (decision count, warnings, status, etc.). */
export async function getManifestSummary(manifestId: string): Promise<ManifestSummary> {
  return apiGet<ManifestSummary>(`/v1/authority/manifests/${manifestId}/summary`);
}

/** Lists all synthesized artifacts for a manifest (metadata only, no binary content). */
export async function listArtifacts(manifestId: string): Promise<ArtifactDescriptor[]> {
  return apiGet<ArtifactDescriptor[]>(`/v1/artifacts/manifests/${manifestId}`);
}

/** JSON metadata for one artifact (no binary download). */
export async function getArtifactDescriptor(
  manifestId: string,
  artifactId: string,
): Promise<ArtifactDescriptor> {
  return apiGet<ArtifactDescriptor>(
    `/v1/artifacts/manifests/${manifestId}/artifact/${artifactId}/descriptor`,
  );
}

/** In-shell preview cap; artifacts larger than this are truncated for the review panel. */
const DEFAULT_ARTIFACT_PREVIEW_MAX_BYTES = 2 * 1024 * 1024;

/** Result of fetching artifact binary content and decoding it as UTF-8 for in-shell preview. */
export type ArtifactContentFetchResult = {
  text: string;
  contentType: string;
  byteLength: number;
  truncated: boolean;
};

/**
 * Fetches artifact bytes from the download endpoint and decodes as UTF-8 for in-shell review.
 * Large artifacts are truncated deterministically for the preview panel (download remains full file).
 */
export async function fetchArtifactContentUtf8(
  manifestId: string,
  artifactId: string,
  maxBytes: number = DEFAULT_ARTIFACT_PREVIEW_MAX_BYTES,
): Promise<ArtifactContentFetchResult> {
  await ensureOidcBearerReady();
  const path = `/v1/artifacts/manifests/${encodeURIComponent(manifestId)}/artifact/${encodeURIComponent(artifactId)}`;
  const { url, headers } = resolveBinaryGetRequest(path);
  const h = withCorrelationHeaders(headers);
  const response = await fetch(url, {
    cache: "no-store",
    headers: h,
  });

  if (!response.ok) {
    const text = await response.text();
    throw buildApiRequestErrorFromParts(response, text);
  }

  const contentType = response.headers.get("content-type") ?? "application/octet-stream";
  const buffer = await response.arrayBuffer();
  const byteLength = buffer.byteLength;
  let truncated = false;
  let slice = buffer;

  if (byteLength > maxBytes) {
    truncated = true;
    slice = buffer.slice(0, maxBytes);
  }

  const text = new TextDecoder("utf-8", { fatal: false }).decode(slice);

  return {
    text,
    contentType,
    byteLength,
    truncated,
  };
}

/** Legacy flat-diff comparison between two runs (run-level + optional manifest diffs). */
export async function compareRuns(leftRunId: string, rightRunId: string): Promise<RunComparison> {
  return apiGet<RunComparison>(
    `/v1/authority/compare/runs?leftRunId=${encodeURIComponent(leftRunId)}&rightRunId=${encodeURIComponent(rightRunId)}`,
  );
}

/** Structured golden manifest comparison (decision/requirement/security/topology/cost deltas). */
export async function compareGoldenManifestRuns(
  baseRunId: string,
  targetRunId: string,
): Promise<GoldenManifestComparison> {
  return apiGet<GoldenManifestComparison>(
    `/v1/compare?baseRunId=${encodeURIComponent(baseRunId)}&targetRunId=${encodeURIComponent(targetRunId)}`,
  );
}

/** Requests an AI-generated narrative explanation of the differences between two runs. */
export async function explainComparisonRuns(
  baseRunId: string,
  targetRunId: string,
): Promise<ComparisonExplanation> {
  return apiGet<ComparisonExplanation>(
    `/v1/explain/compare/explain?baseRunId=${encodeURIComponent(baseRunId)}&targetRunId=${encodeURIComponent(targetRunId)}`,
  );
}

/** Requests an AI-generated explanation of a single run's decisions and implications. */
export async function explainRun(runId: string): Promise<RunExplanation> {
  return apiGet<RunExplanation>(`/v1/explain/runs/${encodeURIComponent(runId)}/explain`);
}

/** Aggregate executive explanation (themes, posture, counts) with nested full explanation payload. */
export async function getRunExplanationSummary(runId: string): Promise<RunExplanationSummary> {
  return apiGet<RunExplanationSummary>(`/v1/explain/runs/${encodeURIComponent(runId)}/aggregate`);
}

/** Sends a natural-language question to the ArchLucid conversational AI endpoint. */
export async function askArchLucid(payload: {
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

  return apiPostJson<AskResponse>("/v1/ask", body);
}

/** Lists recent conversation threads for the current scope. */
export async function listConversationThreads(take = 50): Promise<ConversationThread[]> {
  return apiGet(`/v1/conversations?take=${take}`);
}

/** Fetches messages for a conversation thread (most recent first). */
export async function getConversationMessages(threadId: string, take = 200): Promise<ConversationMessage[]> {
  return apiGet(`/v1/conversations/${encodeURIComponent(threadId)}/messages?take=${take}`);
}

/** Generates an AI-driven improvement plan for a run, optionally compared to another run. */
export async function getImprovementPlan(runId: string, compareToRunId?: string): Promise<ImprovementPlan> {
  const params = new URLSearchParams();
  if (compareToRunId?.trim()) params.set("compareToRunId", compareToRunId.trim());
  const q = params.toString();
  return apiGet<ImprovementPlan>(
    `/v1/advisory/runs/${encodeURIComponent(runId)}/improvements${q ? `?${q}` : ""}`,
  );
}

/** Fetches the most recent recommendation learning profile, or null if none exists (404). */
export async function getLatestLearningProfile(): Promise<LearningProfile | null> {
  await ensureOidcBearerReady();
  const { url, headers } = resolveRequest("/v1/recommendation-learning/latest");
  const h = withCorrelationHeaders(headers);
  const response = await fetch(url, { cache: "no-store", headers: h });
  const text = await response.text();

  if (response.status === 404) {
    return null;
  }

  if (!response.ok) {
    throw buildApiRequestErrorFromParts(response, text);
  }

  return JSON.parse(text) as LearningProfile;
}

/** Optional `since` filter (ISO 8601) appended to product-learning GETs; omit for all-time scope. */
function productLearningSinceQuery(since: string | null | undefined): string {
  const trimmed = since?.trim();
  if (!trimmed) {
    return "";
  }

  return `?since=${encodeURIComponent(trimmed)}`;
}

/**
 * Loads summary, improvement opportunities, artifact outcome trends, and triage queue for the current scope.
 * Each upstream call recomputes its slice; use one refresh action to keep the four panels consistent.
 */
export async function fetchProductLearningDashboard(options?: {
  since?: string | null;
}): Promise<ProductLearningDashboardBundle> {
  const q = productLearningSinceQuery(options?.since);
  const base = `/${ApiV1Routes.productLearning}`;

  const [summary, opportunities, trends, triage] = await Promise.all([
    apiGet(`${base}/summary${q}`),
    apiGet(`${base}/improvement-opportunities${q}`),
    apiGet(`${base}/artifact-outcome-trends${q}`),
    apiGet(`${base}/triage-queue${q}`),
  ]);

  return {
    summary: summary as ProductLearningDashboardBundle["summary"],
    opportunities: opportunities as ProductLearningDashboardBundle["opportunities"],
    trends: trends as ProductLearningDashboardBundle["trends"],
    triage: triage as ProductLearningDashboardBundle["triage"],
  };
}

function learningMaxQuery(param: "maxThemes" | "maxPlans", value: number | undefined): string {
  if (value === undefined) {
    return "";
  }

  return `?${param}=${encodeURIComponent(String(value))}`;
}

/** Lists improvement themes for the current scope (newest first). */
export async function fetchLearningThemes(maxThemes?: number): Promise<LearningThemesListResponse> {
  const q = learningMaxQuery("maxThemes", maxThemes);
  return apiGet<LearningThemesListResponse>(`/${ApiV1Routes.learning}/themes${q}`);
}

/** Lists improvement plans for the current scope (newest first). */
export async function fetchLearningPlans(maxPlans?: number): Promise<LearningPlansListResponse> {
  const q = learningMaxQuery("maxPlans", maxPlans);
  return apiGet<LearningPlansListResponse>(`/${ApiV1Routes.learning}/plans${q}`);
}

/** Loads one improvement plan with steps, link counts, and optional parent theme. */
export async function fetchLearningPlanDetail(planId: string): Promise<LearningPlanDetailResponse> {
  const id = planId.trim();
  return apiGet<LearningPlanDetailResponse>(`/${ApiV1Routes.learning}/plans/${encodeURIComponent(id)}`);
}

/** Aggregated planning KPIs for the current scope. */
export async function fetchLearningSummary(options?: {
  maxThemes?: number;
  maxPlans?: number;
}): Promise<LearningSummaryResponse> {
  const params = new URLSearchParams();
  if (options?.maxThemes !== undefined) {
    params.set("maxThemes", String(options.maxThemes));
  }
  if (options?.maxPlans !== undefined) {
    params.set("maxPlans", String(options.maxPlans));
  }

  const q = params.toString();
  const suffix = q ? `?${q}` : "";

  return apiGet<LearningSummaryResponse>(`/${ApiV1Routes.learning}/summary${suffix}`);
}

/**
 * Loads summary, themes, and plans together (consistent refresh for the planning list view).
 */
export async function fetchLearningPlanningListBundle(options?: {
  maxThemes?: number;
  maxPlans?: number;
}): Promise<{
  summary: LearningSummaryResponse;
  themes: LearningThemesListResponse;
  plans: LearningPlansListResponse;
}> {
  const maxThemes = options?.maxThemes;
  const maxPlans = options?.maxPlans;

  const [summary, themes, plans] = await Promise.all([
    fetchLearningSummary({ maxThemes, maxPlans }),
    fetchLearningThemes(maxThemes),
    fetchLearningPlans(maxPlans),
  ]);

  return { summary, themes, plans };
}

/** Lists 60R evolution candidate change sets for the current scope (newest first server-side). */
export async function fetchEvolutionCandidates(max?: number): Promise<EvolutionCandidateChangeSetListResponse> {
  const q = max !== undefined ? `?max=${encodeURIComponent(String(max))}` : "";

  return apiGet<EvolutionCandidateChangeSetListResponse>(`/${ApiV1Routes.evolution}/candidates${q}`);
}

/** Loads candidate, plan snapshot JSON, and simulation runs with parsed evaluation fields. */
export async function fetchEvolutionResults(candidateId: string): Promise<EvolutionResultsResponse> {
  const id = candidateId.trim();

  return apiGet<EvolutionResultsResponse>(`/${ApiV1Routes.evolution}/results/${encodeURIComponent(id)}`);
}

/**
 * Re-runs simulation for the candidate (replaces prior rows). Requires execute authority; may return 403.
 */
export async function postEvolutionSimulate(candidateId: string): Promise<EvolutionSimulateResponse> {
  const id = candidateId.trim();

  return apiPostJson<EvolutionSimulateResponse>(`/${ApiV1Routes.evolution}/simulate/${encodeURIComponent(id)}`, {});
}

/** Lists all advisory scan schedules for the current scope. */
export async function listAdvisorySchedules(): Promise<AdvisoryScanSchedule[]> {
  return apiGet<AdvisoryScanSchedule[]>("/v1/advisory-scheduling/schedules");
}

/** Creates a new advisory scan schedule with a cron expression. */
export async function createAdvisorySchedule(body: {
  name: string;
  cronExpression: string;
  runProjectSlug?: string;
  isEnabled?: boolean;
}): Promise<AdvisoryScanSchedule> {
  return apiPostJson<AdvisoryScanSchedule>("/v1/advisory-scheduling/schedules", {
    name: body.name,
    cronExpression: body.cronExpression,
    runProjectSlug: body.runProjectSlug?.trim() || "default",
    isEnabled: body.isEnabled ?? true,
  });
}

/** Triggers an immediate execution of an advisory scan schedule. */
export async function runAdvisoryScheduleNow(scheduleId: string): Promise<void> {
  await ensureOidcBearerReady();
  const { url, headers } = resolveRequest(
    `/v1/advisory-scheduling/schedules/${encodeURIComponent(scheduleId)}/run`,
  );
  const h = withCorrelationHeaders(headers);
  h.set("Content-Type", "application/json");
  const response = await fetch(url, { method: "POST", headers: h, cache: "no-store" });
  const text = await response.text();

  if (!response.ok) {
    throw buildApiRequestErrorFromParts(response, text);
  }
}

/** Lists recent executions for an advisory scan schedule. */
export async function listScheduleExecutions(
  scheduleId: string,
  take = 30,
): Promise<AdvisoryScanExecution[]> {
  return apiGet<AdvisoryScanExecution[]>(
    `/v1/advisory-scheduling/schedules/${encodeURIComponent(scheduleId)}/executions?take=${take}`,
  );
}

/** Lists recent architecture digests (periodic summary reports). */
export async function listArchitectureDigests(take = 20): Promise<ArchitectureDigest[]> {
  return apiGet<ArchitectureDigest[]>(`/v1/advisory-scheduling/digests?take=${take}`);
}

/** Lists all digest delivery subscriptions (email, webhook, etc.). */
export async function listDigestSubscriptions(): Promise<DigestSubscription[]> {
  return apiGet<DigestSubscription[]>(`/${ApiV1Routes.digestSubscriptions}`);
}

/** Creates a new digest delivery subscription. */
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

/** Toggles a digest subscription between enabled and disabled. */
export async function toggleDigestSubscription(subscriptionId: string): Promise<DigestSubscription> {
  return apiPostJson<DigestSubscription>(
    `/v1/digest-subscriptions/${encodeURIComponent(subscriptionId)}/toggle`,
    {},
  );
}

/** Lists delivery attempts for a specific digest subscription. */
export async function listSubscriptionDeliveryAttempts(
  subscriptionId: string,
  take = 50,
): Promise<DigestDeliveryAttempt[]> {
  return apiGet<DigestDeliveryAttempt[]>(
    `/${ApiV1Routes.digestSubscriptions}/${encodeURIComponent(subscriptionId)}/attempts?take=${take}`,
  );
}

/** Lists all delivery attempts for a specific digest. */
export async function listDigestDeliveryAttempts(digestId: string): Promise<DigestDeliveryAttempt[]> {
  return apiGet<DigestDeliveryAttempt[]>(
    `/${ApiV1Routes.digestSubscriptions}/digests/${encodeURIComponent(digestId)}/attempts`,
  );
}

/** Fetches a single architecture digest by ID. */
export async function getArchitectureDigest(digestId: string): Promise<ArchitectureDigest> {
  return apiGet<ArchitectureDigest>(
    `/v1/advisory-scheduling/digests/${encodeURIComponent(digestId)}`,
  );
}

/** Lists all alert rules for the current scope. */
export async function listAlertRules(): Promise<AlertRule[]> {
  return apiGet<AlertRule[]>(`/${ApiV1Routes.alertRules}`);
}

/** Creates a new simple alert rule with a severity and threshold. */
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

/** Lists alert records, optionally filtered by status (Active, Acknowledged, Resolved, Suppressed). */
export async function listAlerts(status: string | null, take = 100): Promise<AlertRecord[]> {
  const q = new URLSearchParams();
  if (status) q.set("status", status);
  q.set("take", String(take));
  const suffix = q.toString();
  return apiGet<AlertRecord[]>(`/v1/alerts${suffix ? `?${suffix}` : ""}`);
}

/** Paged alerts (GET with `page` + `pageSize` — returns PagedResponse). */
export async function listAlertsPaged(
  status: string | null,
  page: number,
  pageSize: number,
): Promise<PagedResponse<AlertRecord>> {
  const q = new URLSearchParams();
  if (status) q.set("status", status);
  q.set("page", String(page));
  q.set("pageSize", String(pageSize));

  return apiGet<PagedResponse<AlertRecord>>(`/v1/alerts?${q}`);
}

/** Row from `GET /v1/audit` / `GET /v1/audit/search` (camelCase JSON). */
export interface AuditEvent {
  eventId: string;
  occurredUtc: string;
  eventType: string;
  actorUserId: string;
  actorUserName: string;
  tenantId: string;
  workspaceId: string;
  projectId: string;
  runId: string | null;
  manifestId: string | null;
  artifactId: string | null;
  dataJson: string;
  correlationId: string | null;
}

/** Filtered audit query for the operator UI. */
export async function searchAuditEvents(params: {
  eventType?: string;
  fromUtc?: string;
  toUtc?: string;
  /** Keyset cursor: events strictly older than this ISO-8601 instant (matches API `beforeUtc`). */
  beforeUtc?: string;
  correlationId?: string;
  actorUserId?: string;
  runId?: string;
  take?: number;
}): Promise<AuditEvent[]> {
  const query = new URLSearchParams();
  if (params.eventType) query.set("eventType", params.eventType);
  if (params.fromUtc) query.set("fromUtc", params.fromUtc);
  if (params.toUtc) query.set("toUtc", params.toUtc);
  if (params.beforeUtc) query.set("beforeUtc", params.beforeUtc);
  if (params.correlationId) query.set("correlationId", params.correlationId);
  if (params.actorUserId) query.set("actorUserId", params.actorUserId);
  if (params.runId) query.set("runId", params.runId);
  if (params.take) query.set("take", String(params.take));
  const qs = query.toString();
  return apiGet<AuditEvent[]>(`/v1/audit/search${qs ? `?${qs}` : ""}`);
}

/** Core registry constants for event-type dropdowns (`GET /v1/audit/event-types`). */
export async function getAuditEventTypes(): Promise<string[]> {
  return apiGet<string[]>("/v1/audit/event-types");
}

/** Applies a lifecycle action (Acknowledge, Resolve, Suppress) to an alert record. */
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

/** Lists all alert routing subscriptions (delivery channels for fired alerts). */
export async function listAlertRoutingSubscriptions(): Promise<AlertRoutingSubscription[]> {
  return apiGet<AlertRoutingSubscription[]>("/v1/alert-routing-subscriptions");
}

/** Creates a new alert routing subscription (channel + severity filter). */
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

/** Toggles an alert routing subscription between enabled and disabled. */
export async function toggleAlertRoutingSubscription(
  routingSubscriptionId: string,
): Promise<AlertRoutingSubscription> {
  return apiPostJson<AlertRoutingSubscription>(
    `/v1/alert-routing-subscriptions/${encodeURIComponent(routingSubscriptionId)}/toggle`,
    {},
  );
}

/** Lists delivery attempts for an alert routing subscription. */
export async function listAlertRoutingDeliveryAttempts(
  routingSubscriptionId: string,
  take = 30,
): Promise<AlertRoutingDeliveryAttempt[]> {
  return apiGet<AlertRoutingDeliveryAttempt[]>(
    `/${ApiV1Routes.alertRoutingSubscriptions}/${encodeURIComponent(routingSubscriptionId)}/attempts?take=${take}`,
  );
}

/** Lists all composite alert rules (multi-condition rules with AND/OR logic). */
export async function listCompositeAlertRules(): Promise<CompositeAlertRule[]> {
  return apiGet<CompositeAlertRule[]>(`/${ApiV1Routes.compositeAlertRules}`);
}

/** Simulates an alert rule against recent runs to preview what alerts would fire. */
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

/** Lists all policy packs for the current scope. */
export async function listPolicyPacks(): Promise<PolicyPack[]> {
  return apiGet<PolicyPack[]>(`/${ApiV1Routes.policyPacks}`);
}

/** Lists published versions for a policy pack. */
export async function listPolicyPackVersions(policyPackId: string): Promise<PolicyPackVersion[]> {
  return apiGet<PolicyPackVersion[]>(
    `/${ApiV1Routes.policyPacks}/${encodeURIComponent(policyPackId)}/versions`,
  );
}

/** Fetches the effective (resolved) set of policy packs for the current scope. */
export async function getEffectivePolicyPacks(): Promise<EffectivePolicyPackSet> {
  return apiGet<EffectivePolicyPackSet>(`/${ApiV1Routes.policyPacks}/effective`);
}

/** Fetches the merged content document from all effective policy packs. */
export async function getEffectivePolicyContent(): Promise<PolicyPackContentDocument> {
  return apiGet<PolicyPackContentDocument>(`/${ApiV1Routes.policyPacks}/effective-content`);
}

/** Fetches the governance resolution result (merge decisions, conflicts, effective content). */
export async function getGovernanceResolution(): Promise<EffectiveGovernanceResolutionResult> {
  return apiGet<EffectiveGovernanceResolutionResult>(`/${ApiV1Routes.governanceResolution}`);
}

const governanceBase = (): string => `/${ApiV1Routes.governance}`;

/** Cross-run governance dashboard: pending approvals, recent decisions, tenant policy change log. */
export async function getGovernanceDashboard(
  maxPending = 20,
  maxDecisions = 20,
  maxChanges = 20,
): Promise<GovernanceDashboardSummary> {
  const query = new URLSearchParams({
    maxPending: String(maxPending),
    maxDecisions: String(maxDecisions),
    maxChanges: String(maxChanges),
  });

  return apiGet<GovernanceDashboardSummary>(`${governanceBase()}/dashboard?${query.toString()}`);
}

/** Lists approval requests for a run (governance workflow). */
export async function listApprovalRequests(runId: string): Promise<GovernanceApprovalRequest[]> {
  return apiGet<GovernanceApprovalRequest[]>(
    `${governanceBase()}/runs/${encodeURIComponent(runId)}/approval-requests`,
  );
}

/** Submits a new governance approval request for manifest promotion between environments. */
export async function submitApprovalRequest(body: {
  runId: string;
  manifestVersion: string;
  sourceEnvironment: string;
  targetEnvironment: string;
  requestComment?: string;
}): Promise<GovernanceApprovalRequest> {
  return apiPostJson<GovernanceApprovalRequest>(`${governanceBase()}/approval-requests`, body);
}

/** Approves a pending governance approval request. */
export async function approveRequest(
  approvalRequestId: string,
  body: { reviewedBy: string; reviewComment?: string },
): Promise<GovernanceApprovalRequest> {
  return apiPostJson<GovernanceApprovalRequest>(
    `${governanceBase()}/approval-requests/${encodeURIComponent(approvalRequestId)}/approve`,
    body,
  );
}

/** Rejects a pending governance approval request. */
export async function rejectRequest(
  approvalRequestId: string,
  body: { reviewedBy: string; reviewComment?: string },
): Promise<GovernanceApprovalRequest> {
  return apiPostJson<GovernanceApprovalRequest>(
    `${governanceBase()}/approval-requests/${encodeURIComponent(approvalRequestId)}/reject`,
    body,
  );
}

/** Records promotion of a manifest from source to target environment (after approval when required). */
export async function promoteManifest(body: {
  runId: string;
  manifestVersion: string;
  sourceEnvironment: string;
  targetEnvironment: string;
  promotedBy: string;
  approvalRequestId?: string;
  notes?: string;
}): Promise<GovernancePromotionRecord> {
  return apiPostJson<GovernancePromotionRecord>(`${governanceBase()}/promotions`, body);
}

/**
 * Activates a run/manifest as the baseline for an environment.
 * `activatedBy` is part of the UI contract for operator context; the API derives the actor from auth and only reads runId, manifestVersion, environment from the JSON body.
 */
export async function activateEnvironment(body: {
  runId: string;
  manifestVersion: string;
  environment: string;
  activatedBy: string;
}): Promise<GovernanceEnvironmentActivation> {
  void body.activatedBy;

  return apiPostJson<GovernanceEnvironmentActivation>(`${governanceBase()}/activations`, {
    runId: body.runId,
    manifestVersion: body.manifestVersion,
    environment: body.environment,
  });
}

/** Lists promotion audit rows for a run. */
export async function listPromotions(runId: string): Promise<GovernancePromotionRecord[]> {
  return apiGet<GovernancePromotionRecord[]>(
    `${governanceBase()}/runs/${encodeURIComponent(runId)}/promotions`,
  );
}

/** Lists environment activation rows for a run. */
export async function listActivations(runId: string): Promise<GovernanceEnvironmentActivation[]> {
  return apiGet<GovernanceEnvironmentActivation[]>(
    `${governanceBase()}/runs/${encodeURIComponent(runId)}/activations`,
  );
}

/** Creates a new policy pack with an initial content document. */
export async function createPolicyPack(body: {
  name: string;
  description?: string;
  packType: string;
  initialContentJson?: string;
}): Promise<PolicyPack> {
  return apiPostJson<PolicyPack>(`/${ApiV1Routes.policyPacks}`, body);
}

/** Publishes a new version of a policy pack with optional updated content. */
export async function publishPolicyPackVersion(
  policyPackId: string,
  body: { version: string; contentJson?: string },
): Promise<PolicyPackVersion> {
  return apiPostJson<PolicyPackVersion>(
    `/${ApiV1Routes.policyPacks}/${encodeURIComponent(policyPackId)}/publish`,
    body,
  );
}

/** Assigns a specific policy pack version to the current scope (project/workspace/tenant). */
export async function assignPolicyPack(
  policyPackId: string,
  body: { version: string; scopeLevel?: string; isPinned?: boolean },
): Promise<PolicyPackAssignment> {
  return apiPostJson<PolicyPackAssignment>(
    `/${ApiV1Routes.policyPacks}/${encodeURIComponent(policyPackId)}/assign`,
    body,
  );
}

/** Evaluates candidate thresholds and recommends the best one based on noise scoring. */
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

/** Compares two alert rule candidates side-by-side using simulation. */
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

/** Creates a composite alert rule with multiple metric conditions and suppression/cooldown settings. */
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

/** Triggers a full rebuild of the recommendation learning profile from historical outcomes. */
export async function rebuildLearningProfile(): Promise<LearningProfile> {
  await ensureOidcBearerReady();
  const { url, headers } = resolveRequest("/v1/recommendation-learning/rebuild");
  const h = withCorrelationHeaders(headers);
  h.set("Content-Type", "application/json");
  const response = await fetch(url, {
    method: "POST",
    headers: h,
    cache: "no-store",
  });
  const text = await response.text();

  if (!response.ok) {
    throw buildApiRequestErrorFromParts(response, text);
  }

  return JSON.parse(text) as LearningProfile;
}

/** Replays an authority chain for a run using the specified mode (ReconstructOnly, RebuildManifest, RebuildArtifacts). */
export async function replayRun(runId: string, mode: string): Promise<ReplayResponse> {
  await ensureOidcBearerReady();
  const { url, headers } = resolveRequest("/v1/authority/replay");
  const h = withCorrelationHeaders(headers);
  h.set("Content-Type", "application/json");
  const response = await fetch(url, {
    method: "POST",
    headers: h,
    cache: "no-store",
    body: JSON.stringify({ runId, mode }),
  });
  const text = await response.text();

  if (!response.ok) {
    throw buildApiRequestErrorFromParts(response, text);
  }

  return JSON.parse(text) as ReplayResponse;
}

/** Use same-origin proxy so browser downloads work with API key auth. */
export function getArtifactDownloadUrl(manifestId: string, artifactId: string): string {
  return `/api/proxy/v1/artifacts/manifests/${manifestId}/artifact/${artifactId}`;
}

/** Returns the proxy URL for downloading the full artifact bundle ZIP for a manifest. */
export function getBundleDownloadUrl(manifestId: string): string {
  return `/api/proxy/v1/artifacts/manifests/${manifestId}/bundle`;
}

/** Returns the proxy URL for downloading the full run export ZIP. */
export function getRunExportDownloadUrl(runId: string): string {
  return `/api/proxy/v1/artifacts/runs/${runId}/export`;
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
  return `/api/proxy/v1/docx/runs/${runId}/architecture-package${q ? `?${q}` : ""}`;
}
