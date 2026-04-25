/**
 * Typed JSON access to `/v1/...` plus proxy/binary helpers.
 * For a compact **current principal** read-model (`GET /api/proxy/api/auth/me`), use `@/lib/current-principal`
 * instead of adding ad-hoc identity fetches here.
 */
import { buildApiRequestErrorFromParts } from "@/lib/api-error";
import { ApiV1Routes } from "@/lib/api-v1-routes";
import { CORRELATION_ID_HEADER, generateCorrelationId } from "@/lib/correlation";
import { getServerApiBaseUrl } from "@/lib/config";
import { getServerUpstreamAuthHeaders } from "@/lib/legacy-arch-env";
import { isJwtAuthMode } from "@/lib/oidc/config";
import { ensureAccessTokenFresh, getAccessTokenForApi } from "@/lib/oidc/session";
import { getEffectiveBrowserProxyScopeHeaders } from "@/lib/operator-scope-storage";
import { mergeRegistrationScopeForProxy } from "@/lib/proxy-fetch-registration-scope";
import { getScopeHeaders } from "@/lib/scope";
import type { GoldenManifestComparison } from "@/types/comparison";
import type { DemoExplainResponse } from "@/types/demo-explain";
import type {
  ComparisonExplanation,
  FindingEvidenceChain,
  FindingExplainability,
  FindingLlmAudit,
  RunExplanation,
  RunExplanationSummary,
} from "@/types/explanation";
import type { FindingInspectPayload } from "@/types/finding-inspect";
import type {
  ArtifactDescriptor,
  DecisionProvenanceGraph,
  ManifestSummary,
  PipelineTimelineItem,
  ReplayResponse,
  RunComparison,
  RunDetail,
  RunSummary,
} from "@/types/authority";
import type { PagedResponse } from "@/types/pagination";
import type { TenantCostEstimateResponse } from "@/types/tenant-cost-estimate";
import type {
  AskResponse,
  ConversationMessage,
  ConversationThread,
} from "@/types/conversation";
import type { ArchitectureRunProvenanceGraph } from "@/types/architecture-provenance";
import type {
  AgentExecutionTraceListPayload,
  AgentOutputEvaluationSummaryPayload,
} from "@/types/agent-forensics";
import type { ImprovementPlan } from "@/types/advisory";
import type { LearningProfile } from "@/types/recommendation-learning";
import type {
  AdvisoryScanExecution,
  AdvisoryScanSchedule,
  ArchitectureDigest,
} from "@/types/advisory-scheduling";
import type { DigestDeliveryAttempt, DigestSubscription } from "@/types/digest-subscriptions";
import type {
  ExecDigestPreferencesResponse,
  ExecDigestPreferencesUpsertRequest,
} from "@/types/exec-digest-preferences";
import type {
  TeamsIncomingWebhookConnectionResponse,
  TeamsIncomingWebhookConnectionUpsertRequest,
} from "@/types/teams-incoming-webhook-connection";
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
import {
  POLICY_PACK_DRY_RUN_DEFAULT_PAGE_SIZE,
  POLICY_PACK_DRY_RUN_MAX_PAGE_SIZE,
  type PolicyPackDryRunRequest,
  type PolicyPackDryRunResponse,
} from "@/types/policy-pack-dry-run";
import type { EffectiveGovernanceResolutionResult } from "@/types/governance-resolution";
import type {
  ComplianceDriftTrendPoint,
  GovernanceDashboardSummary,
  GovernanceLineageResult,
  GovernanceRationaleResult,
} from "@/types/governance-dashboard";
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
      ...getEffectiveBrowserProxyScopeHeaders(),
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
    ...getServerUpstreamAuthHeaders(),
  };

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
      ...getEffectiveBrowserProxyScopeHeaders(),
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
    ...getServerUpstreamAuthHeaders(),
  };

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

/** DELETEs a path; returns void on 2xx. Throws on HTTP errors. */
export async function apiDelete(path: string): Promise<void> {
  await ensureOidcBearerReady();
  const { url, headers } = resolveRequest(path);
  const h = withCorrelationHeaders(headers);
  const response = await fetch(url, {
    method: "DELETE",
    headers: h,
    cache: "no-store",
  });
  const text = await response.text();

  if (!response.ok) {
    throw buildApiRequestErrorFromParts(response, text);
  }
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

/** Commits agent results into a golden manifest (POST /v1/architecture/run/{runId}/commit). */
export async function commitArchitectureRun(runId: string): Promise<unknown> {
  return apiPostJson<unknown>(`/v1/architecture/run/${encodeURIComponent(runId)}/commit`, {});
}

/** Seeds deterministic fake agent results for a run (POST /v1/architecture/run/{runId}/seed-fake-results; non-Production only). */
export async function seedFakeArchitectureRunResults(runId: string): Promise<{ resultCount?: number }> {
  return apiPostJson<{ resultCount?: number }>(
    `/v1/architecture/run/${encodeURIComponent(runId)}/seed-fake-results`,
    {},
  );
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

/** Run-scoped audit events oldest-first (pipeline / lifecycle timeline for operators). */
export async function getRunPipelineTimeline(runId: string): Promise<PipelineTimelineItem[]> {
  return apiGet<PipelineTimelineItem[]>(`/v1/authority/runs/${runId}/pipeline-timeline`);
}

/** Paginated agent execution traces (LLM audit rows) for a coordinator architecture run. */
export async function getRunTraces(
  runId: string,
  pageNumber = 1,
  pageSize = 50,
): Promise<ApiResponseWithTrace<AgentExecutionTraceListPayload>> {
  const q = new URLSearchParams();
  q.set("pageNumber", String(pageNumber));
  q.set("pageSize", String(pageSize));

  return apiGetJsonWithTrace<AgentExecutionTraceListPayload>(
    `/v1/architecture/run/${encodeURIComponent(runId)}/traces?${q}`,
  );
}

/** On-demand structural evaluation of persisted `parsedResultJson` per trace (no OTel side effects in API). */
export async function getRunAgentEvaluation(
  runId: string,
): Promise<ApiResponseWithTrace<AgentOutputEvaluationSummaryPayload>> {
  return apiGetJsonWithTrace<AgentOutputEvaluationSummaryPayload>(
    `/v1/architecture/run/${encodeURIComponent(runId)}/agent-evaluation`,
  );
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

/**
 * Fetches the sponsor first-value report (Markdown body) for a run.
 * Returns `null` when the API responds 404 (run not found / not committed yet).
 */
export async function getFirstValueReportMarkdown(runId: string): Promise<string | null> {
  await ensureOidcBearerReady();
  const { url, headers } = resolveRequest(`/v1/pilots/runs/${encodeURIComponent(runId)}/first-value-report`);
  const h = withCorrelationHeaders(headers);
  h.set("Accept", "text/markdown");
  const response = await fetch(url, { cache: "no-store", headers: h });
  const text = await response.text();

  if (response.status === 404) return null;

  if (!response.ok) throw buildApiRequestErrorFromParts(response, text);

  return text;
}

/** Server-rendered telemetry snapshot for the operator-shell `/why-archlucid` proof page. */
export type WhyArchLucidSnapshot = {
  generatedUtc: string;
  demoRunId: string;
  runsCreatedTotal: number;
  findingsProducedBySeverity: Record<string, number>;
  auditRowCount: number;
  auditRowCountTruncated: boolean;
};

/** GETs the `/v1/pilots/why-archlucid-snapshot` JSON snapshot used by the proof page. */
export async function getWhyArchLucidSnapshot(): Promise<WhyArchLucidSnapshot> {
  return apiGet<WhyArchLucidSnapshot>("/v1/pilots/why-archlucid-snapshot");
}

/** Bundle for `/why-archlucid`: process counters + optional monthly cost band + disclaimers. */
export type TenantMeasuredRoiPayload = {
  snapshot: WhyArchLucidSnapshot;
  monthlyCostEstimate: TenantCostEstimateResponse | null;
  disclaimer: string;
};

/** GETs `/v1/tenant/measured-roi` (operator proof page — combines snapshot + cost context). */
export async function getTenantMeasuredRoi(): Promise<TenantMeasuredRoiPayload> {
  return apiGet<TenantMeasuredRoiPayload>(`/${ApiV1Routes.tenantMeasuredRoi}`);
}

/**
 * GETs the side-by-side provenance + explanation payload used by the operator-shell
 * `/demo/explain` proof route. Returns `null` when the API responds 404 — that covers both
 * "demo seed has not been applied yet" and "deployment is not `Demo:Enabled=true`" (the
 * `[FeatureGate(DemoEnabled)]` filter on the server returns 404 by design so production-like
 * hosts cannot leak the demo surface). Callers should render a friendly fallback in either case.
 */
export async function getDemoExplain(): Promise<DemoExplainResponse | null> {
  await ensureOidcBearerReady();
  const { url, headers } = resolveRequest("/v1/demo/explain");
  const h = withCorrelationHeaders(headers);
  const response = await fetch(url, { cache: "no-store", headers: h });
  const text = await response.text();

  if (response.status === 404) return null;

  if (!response.ok) throw buildApiRequestErrorFromParts(response, text);

  return JSON.parse(text) as DemoExplainResponse;
}

/** Read-model inspector: typed payload, rules, evidence citations, audit correlation (ReadAuthority). */
export async function getFindingInspect(findingId: string): Promise<FindingInspectPayload> {
  return apiGet<FindingInspectPayload>(`/v1/findings/${encodeURIComponent(findingId)}/inspect`);
}

/** Persisted explainability trace + narrative for a single finding (no LLM). */
export async function getFindingExplainability(runId: string, findingId: string): Promise<FindingExplainability> {
  const encodedFinding = encodeURIComponent(findingId);

  return apiGet<FindingExplainability>(
    `/v1/explain/runs/${encodeURIComponent(runId)}/findings/${encodedFinding}/explainability`,
  );
}

/** Evidence-chain pointers for one finding (ReadAuthority; architecture query surface). */
export async function getFindingEvidenceChain(runId: string, findingId: string): Promise<FindingEvidenceChain> {
  const encodedFinding = encodeURIComponent(findingId);

  return apiGet<FindingEvidenceChain>(
    `/v1/architecture/run/${encodeURIComponent(runId)}/findings/${encodedFinding}/evidence-chain`,
  );
}

/** Redacted LLM prompt/completion audit for one finding (ReadAuthority). */
export async function getFindingLlmAudit(runId: string, findingId: string): Promise<FindingLlmAudit> {
  const encodedFinding = encodeURIComponent(findingId);

  return apiGet<FindingLlmAudit>(
    `/v1/explain/runs/${encodeURIComponent(runId)}/findings/${encodedFinding}/llm-audit`,
  );
}

/** Records thumbs feedback for a finding (ExecuteAuthority). */
export async function postFindingFeedback(
  runId: string,
  findingId: string,
  score: -1 | 1,
): Promise<void> {
  const encodedFinding = encodeURIComponent(findingId);

  await apiPostJson(
    `/v1/explain/runs/${encodeURIComponent(runId)}/findings/${encodedFinding}/feedback`,
    { score },
  );
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
 * Re-runs simulation for the candidate (replaces prior rows). Requires operator access; may return 403.
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

/** Loads weekly executive digest email preferences for the current tenant. */
export async function getExecDigestPreferences(): Promise<ExecDigestPreferencesResponse> {
  return apiGet<ExecDigestPreferencesResponse>(`/${ApiV1Routes.tenantExecDigestPreferences}`);
}

/** Rough monthly spend band for Standard+ tenants (402 when below Standard). */
export async function getTenantCostEstimate(): Promise<TenantCostEstimateResponse> {
  return apiGet<TenantCostEstimateResponse>(`/${ApiV1Routes.tenantCostEstimate}`);
}

/** Saves weekly executive digest email preferences (Execute+). */
export async function saveExecDigestPreferences(
  body: ExecDigestPreferencesUpsertRequest,
): Promise<ExecDigestPreferencesResponse> {
  return apiPostJson<ExecDigestPreferencesResponse>(`/${ApiV1Routes.tenantExecDigestPreferences}`, body);
}

/** Loads Teams incoming-webhook Key Vault reference for the current tenant (secret value never returned). */
export async function getTeamsIncomingWebhookConnection(): Promise<TeamsIncomingWebhookConnectionResponse> {
  return apiGet<TeamsIncomingWebhookConnectionResponse>(`/${ApiV1Routes.teamsIncomingWebhookConnections}`);
}

/** Upserts Teams incoming-webhook Key Vault secret name reference (Execute+). */
export async function upsertTeamsIncomingWebhookConnection(
  body: TeamsIncomingWebhookConnectionUpsertRequest,
): Promise<TeamsIncomingWebhookConnectionResponse> {
  return apiPostJson<TeamsIncomingWebhookConnectionResponse>(`/${ApiV1Routes.teamsIncomingWebhookConnections}`, body);
}

/** Removes Teams Key Vault reference (Execute+). */
export async function deleteTeamsIncomingWebhookConnection(): Promise<void> {
  return apiDelete(`/${ApiV1Routes.teamsIncomingWebhookConnections}`);
}

/** Loads the canonical v1 Teams notification trigger catalog (canonical event-type strings). */
export async function getTeamsNotificationTriggerCatalog(): Promise<string[]> {
  return apiGet<string[]>(`/${ApiV1Routes.teamsNotificationTriggerCatalog}`);
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
  otelTraceId?: string | null;
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

/**
 * Downloads `GET /v1/audit/export` as CSV (browser only). Requires UTC instants acceptable to the API.
 */
export async function downloadAuditExportCsv(params: {
  fromUtcIso: string;
  toUtcIso: string;
  maxRows?: number;
}): Promise<void> {
  if (typeof window === "undefined") {
    throw new Error("downloadAuditExportCsv is only available in the browser.");
  }

  const query = new URLSearchParams();
  query.set("fromUtc", params.fromUtcIso);
  query.set("toUtc", params.toUtcIso);
  if (params.maxRows !== undefined) {
    query.set("maxRows", String(params.maxRows));
  }

  await ensureOidcBearerReady();
  const { url, headers } = resolveBinaryGetRequest(`/v1/audit/export?${query.toString()}`);
  const h = withCorrelationHeaders(new Headers(headers));
  h.set("Accept", "text/csv");
  const response = await fetch(url, { cache: "no-store", headers: h });
  const text = await response.text();

  if (!response.ok) {
    throw buildApiRequestErrorFromParts(response, text);
  }

  const blob = new Blob([text], { type: "text/csv;charset=utf-8" });
  const disposition = response.headers.get("Content-Disposition");
  let filename = "audit-export.csv";

  if (disposition) {
    const m = /filename="?([^";]+)"?/i.exec(disposition);

    if (m?.[1]) {
      filename = m[1].trim();
    }
  }

  const objectUrl = URL.createObjectURL(blob);
  const anchor = document.createElement("a");
  anchor.href = objectUrl;
  anchor.download = filename;
  anchor.rel = "noopener";
  document.body.appendChild(anchor);
  anchor.click();
  anchor.remove();
  URL.revokeObjectURL(objectUrl);
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

/** Policy pack change activity buckets for the governance dashboard trend chart. */
export async function getComplianceDriftTrend(
  fromUtc: string,
  toUtc: string,
  bucketMinutes = 1440,
): Promise<ComplianceDriftTrendPoint[]> {
  const query = new URLSearchParams({
    fromUtc,
    toUtc,
    bucketMinutes: String(bucketMinutes),
  });

  return apiGet<ComplianceDriftTrendPoint[]>(
    `${governanceBase()}/compliance-drift-trend?${query.toString()}`,
  );
}

/** Joins an approval request to run summary, authority manifest/findings (when linked), and promotions. */
export async function getApprovalRequestLineage(
  approvalRequestId: string,
): Promise<GovernanceLineageResult> {
  return apiGet<GovernanceLineageResult>(
    `${governanceBase()}/approval-requests/${encodeURIComponent(approvalRequestId)}/lineage`,
  );
}

/** Deterministic governance rationale (lineage-derived bullets; no LLM). */
export async function getGovernanceApprovalRationale(
  approvalRequestId: string,
): Promise<GovernanceRationaleResult> {
  return apiGet<GovernanceRationaleResult>(
    `${governanceBase()}/approval-requests/${encodeURIComponent(approvalRequestId)}/rationale`,
  );
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
  body: { reviewedBy?: string; reviewComment?: string },
): Promise<GovernanceApprovalRequest> {
  return apiPostJson<GovernanceApprovalRequest>(
    `${governanceBase()}/approval-requests/${encodeURIComponent(approvalRequestId)}/approve`,
    body,
  );
}

/** Rejects a pending governance approval request. */
export async function rejectRequest(
  approvalRequestId: string,
  body: { reviewedBy?: string; reviewComment?: string },
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

/**
 * Dry-runs proposed threshold changes for a policy pack against a list of historic runs without
 * committing anything (POST `/v1/governance/policy-packs/{id}/dry-run`). The default page size is
 * fixed by `POLICY_PACK_DRY_RUN_DEFAULT_PAGE_SIZE` and clamped client-side to
 * `POLICY_PACK_DRY_RUN_MAX_PAGE_SIZE` per owner Q38 (the API will also clamp). The response always
 * carries a `proposedThresholdsRedactedJson` value that has been through the LLM-prompt redaction
 * pipeline (PENDING_QUESTIONS Q37) before persistence in the audit log.
 */
export async function dryRunPolicyPack(
  policyPackId: string,
  body: PolicyPackDryRunRequest,
  options?: { page?: number; pageSize?: number },
): Promise<PolicyPackDryRunResponse> {
  const pageSize = clampDryRunPageSize(options?.pageSize);
  const page = clampDryRunPage(options?.page);
  const query = new URLSearchParams({ page: String(page), pageSize: String(pageSize) });

  return apiPostJson<PolicyPackDryRunResponse>(
    `/${ApiV1Routes.policyPacks}/${encodeURIComponent(policyPackId)}/dry-run?${query.toString()}`,
    body,
  );
}

function clampDryRunPageSize(input: number | undefined): number {
  if (input === undefined || !Number.isFinite(input)) {
    return POLICY_PACK_DRY_RUN_DEFAULT_PAGE_SIZE;
  }

  if (input < 1) {
    return POLICY_PACK_DRY_RUN_DEFAULT_PAGE_SIZE;
  }

  return Math.min(Math.floor(input), POLICY_PACK_DRY_RUN_MAX_PAGE_SIZE);
}

function clampDryRunPage(input: number | undefined): number {
  if (input === undefined || !Number.isFinite(input) || input < 1) {
    return 1;
  }

  return Math.floor(input);
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

/** Returns the proxy URL for the traceability ZIP (run summary + audit slice + decision traces, size-capped on API). */
export function getTraceabilityBundleDownloadUrl(runId: string): string {
  return `/api/proxy/v1/architecture/run/${encodeURIComponent(runId)}/traceability-bundle.zip`;
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

function parseFilenameFromContentDisposition(header: string | null): string | null {
  if (!header) return null;
  const m = /filename\*?=(?:UTF-8''|")?([^";]+)/i.exec(header);

  return m?.[1]?.replace(/"/g, "").trim() ?? null;
}

/**
 * POST `/v1/pilots/runs/{runId}/first-value-report.pdf` and trigger a browser download of the resulting PDF
 * (sponsor-shareable projection of the canonical first-value-report Markdown). Mirrors the auth surface of
 * the Markdown sibling (`ReadAuthority`, no Standard-tier gate) so the post-commit CTA stays one-click.
 * Throws {@link ApiRequestError}-shaped error on non-2xx responses.
 */
export async function downloadFirstValueReportPdf(runId: string): Promise<void> {
  if (!isBrowser()) {
    throw new Error("downloadFirstValueReportPdf is only supported in the browser.");
  }

  await ensureOidcBearerReady();
  const path = `/v1/pilots/runs/${encodeURIComponent(runId)}/first-value-report.pdf`;
  const url = `/api/proxy${path}`;
  const headers = new Headers();
  headers.set("Accept", "application/pdf, application/json");
  const bearer = getBearerToken();
  if (bearer) headers.set("Authorization", `Bearer ${bearer}`);
  const init = mergeRegistrationScopeForProxy({
    method: "POST",
    headers,
    credentials: "same-origin",
    cache: "no-store",
  });
  const h = new Headers(init.headers);
  h.set(CORRELATION_ID_HEADER, generateCorrelationId());
  const response = await fetch(url, { ...init, method: "POST", headers: h });

  if (!response.ok) {
    const errText = await response.text();
    throw buildApiRequestErrorFromParts(response, errText);
  }

  const fileName =
    parseFilenameFromContentDisposition(response.headers.get("Content-Disposition")) ??
    `ArchLucid-first-value-report-${runId}.pdf`;
  const blob = await response.blob();
  const objectUrl = URL.createObjectURL(blob);
  const a = document.createElement("a");
  a.href = objectUrl;
  a.download = fileName;
  a.click();
  URL.revokeObjectURL(objectUrl);
}

/**
 * POST `/v1/pilots/board-pack.pdf` — quarterly sponsor PDF (`ExecuteAuthority`, Standard+ tier on API).
 * Browser-only download.
 */
export async function downloadBoardPackPdf(year: number, quarter: number): Promise<void> {
  if (!isBrowser()) {
    throw new Error("downloadBoardPackPdf is only supported in the browser.");
  }

  await ensureOidcBearerReady();
  const path = "/v1/pilots/board-pack.pdf";
  const url = `/api/proxy${path}`;
  const headers = new Headers();
  headers.set("Accept", "application/pdf, application/json");
  headers.set("Content-Type", "application/json");
  const bearer = getBearerToken();
  if (bearer) headers.set("Authorization", `Bearer ${bearer}`);
  const init = mergeRegistrationScopeForProxy({
    method: "POST",
    headers,
    credentials: "same-origin",
    cache: "no-store",
    body: JSON.stringify({ year, quarter }),
  });
  const h = new Headers(init.headers);
  h.set(CORRELATION_ID_HEADER, generateCorrelationId());
  const response = await fetch(url, { ...init, method: "POST", headers: h });

  if (!response.ok) {
    const errText = await response.text();
    throw buildApiRequestErrorFromParts(response, errText);
  }

  const fileName =
    parseFilenameFromContentDisposition(response.headers.get("Content-Disposition")) ??
    `ArchLucid-board-pack-Q${quarter}-${year}.pdf`;
  const blob = await response.blob();
  const objectUrl = URL.createObjectURL(blob);
  const a = document.createElement("a");
  a.href = objectUrl;
  a.download = fileName;
  a.click();
  URL.revokeObjectURL(objectUrl);
}

/** POST sponsor value report DOCX (`ExecuteAuthority`, Standard+ tier on API). Browser-only download. */
export async function downloadValueReportDocx(
  tenantId: string,
  fromIso: string,
  toIso: string,
): Promise<void> {
  if (!isBrowser()) {
    throw new Error("downloadValueReportDocx is only supported in the browser.");
  }

  await ensureOidcBearerReady();
  const path = `/v1/value-report/${encodeURIComponent(tenantId)}/generate?from=${encodeURIComponent(fromIso)}&to=${encodeURIComponent(toIso)}`;
  const url = `/api/proxy${path}`;
  const headers = new Headers();
  headers.set(
    "Accept",
    "application/vnd.openxmlformats-officedocument.wordprocessingml.document, application/json",
  );
  const bearer = getBearerToken();
  if (bearer) headers.set("Authorization", `Bearer ${bearer}`);
  const init = mergeRegistrationScopeForProxy({
    method: "POST",
    headers,
    credentials: "same-origin",
    cache: "no-store",
  });
  const h = new Headers(init.headers);
  h.set(CORRELATION_ID_HEADER, generateCorrelationId());
  const response = await fetch(url, { ...init, method: "POST", headers: h });

  if (response.status === 202) {
    throw new Error(
      "Large reporting window: async generation started. Open Enterprise Controls → Value report to poll the job.",
    );
  }

  if (!response.ok) {
    const errText = await response.text();
    throw buildApiRequestErrorFromParts(response, errText);
  }

  const fileName =
    parseFilenameFromContentDisposition(response.headers.get("Content-Disposition")) ??
    `ArchLucid-value-report-${tenantId}.docx`;
  const blob = await response.blob();
  const objectUrl = URL.createObjectURL(blob);
  const a = document.createElement("a");
  a.href = objectUrl;
  a.download = fileName;
  a.click();
  URL.revokeObjectURL(objectUrl);
}
