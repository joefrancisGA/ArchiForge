/**
 * Typed helpers for Playwright live-API E2E against ArchLucid.Api
 * (`live-api-journey.spec.ts`, `live-api-conflict-journey.spec.ts`, `live-api-governance-rejection.spec.ts`, …).
 *
 * Auth lanes (see `docs/LIVE_E2E_AUTH_ASSUMPTIONS.md`):
 * - **JWT:** `LIVE_JWT_TOKEN` → `Authorization: Bearer …` (takes precedence over API key when both are set).
 * - **ApiKey:** `LIVE_API_KEY` → `X-Api-Key`.
 * - **DevelopmentBypass:** no auth headers.
 */
import type { APIRequestContext, APIResponse } from "@playwright/test";

import { getLiveJwtTokenFromEnvSync, isLiveJwtTokenConfigured } from "./jwt-token-provider";

/** Base URL for ArchLucid.Api (e.g. http://127.0.0.1:5128). */
export const liveApiBase = process.env.LIVE_API_URL ?? "http://127.0.0.1:5128";

const liveApiKeyEnv = process.env.LIVE_API_KEY?.trim() ?? "";

/** True when `LIVE_JWT_TOKEN` is set — helpers send `Authorization: Bearer`. */
export const isJwtMode = isLiveJwtTokenConfigured();

/** True when `LIVE_API_KEY` is set and JWT is not configured. */
export const isApiKeyMode = liveApiKeyEnv.length > 0 && !isJwtMode;

/** Detected primary auth lane for logging / assertions in specs. */
export type LiveAuthMode = "bypass" | "apikey" | "jwt";

export const liveAuthMode: LiveAuthMode = isJwtMode ? "jwt" : isApiKeyMode ? "apikey" : "bypass";

/** Optional second key for readonly / least-privilege tests (`live-api-apikey-auth.spec.ts`). */
export const liveApiKeyReadOnly = process.env.LIVE_API_KEY_READONLY?.trim() ?? "";

/**
 * Governance submitter identity for segregation: DevelopmentBypass **Developer**, ApiKey **ApiKeyAdmin**,
 * JWT **LIVE_JWT_ACTOR_NAME** (default **JwtE2eAdmin**) — must match JWT `name` claim.
 */
export const liveAuthActorName = isJwtMode
  ? (process.env.LIVE_JWT_ACTOR_NAME?.trim() || "JwtE2eAdmin")
  : isApiKeyMode
    ? "ApiKeyAdmin"
    : "Developer";

/** Distinct `reviewedBy` body value vs {@link liveAuthActorName} for approve/reject paths. */
export const livePeerReviewerActorName = "e2e-peer-reviewer";

/** Scope headers for mutating/reading architecture + tenant routes in a specific tenant (self-service registration E2E). */
export type LiveTenantScopeHeaders = {
  tenantId: string;
  workspaceId: string;
  projectId: string;
};

/** Builds `x-tenant-id` / `x-workspace-id` / `x-project-id` headers for {@link LiveTenantScopeHeaders}. */
export function liveTenantScopeHeaders(scope: LiveTenantScopeHeaders): Record<string, string> {
  return {
    "x-tenant-id": scope.tenantId.trim(),
    "x-workspace-id": scope.workspaceId.trim(),
    "x-project-id": scope.projectId.trim(),
  };
}

function mergeTenantScope(
  headers: Record<string, string>,
  tenantScope?: LiveTenantScopeHeaders | null,
): Record<string, string> {
  if (
    tenantScope === undefined ||
    tenantScope === null ||
    tenantScope.tenantId.trim().length === 0 ||
    tenantScope.workspaceId.trim().length === 0 ||
    tenantScope.projectId.trim().length === 0
  ) {
    return headers;
  }

  return { ...headers, ...liveTenantScopeHeaders(tenantScope) };
}

/**
 * Compares run ids across API surfaces: architecture routes use 32-char hex (`Guid.ToString("N")`),
 * while authority run detail JSON serializes `Guid` with hyphens. The operator UI shows the authority value.
 */
export function normalizeRunIdForCompare(value: string): string {
  return value.replace(/-/g, "").trim().toLowerCase();
}

function pickApiKey(explicitApiKey?: string | null): string | undefined {
  if (explicitApiKey !== undefined && explicitApiKey !== null) {
    const t = explicitApiKey.trim();

    return t.length > 0 ? t : undefined;
  }

  return liveApiKeyEnv.length > 0 ? liveApiKeyEnv : undefined;
}

/**
 * Builds auth headers. Pass explicit `""` to force **no** `Authorization` / `X-Api-Key` (negative tests).
 * For JWT, optional `explicitBearerToken` overrides env token when non-empty (e.g. invalid token tests).
 */
function pickAuthHeaders(
  explicitApiKey?: string | null,
  explicitBearerToken?: string | null,
): Record<string, string> {
  if (explicitApiKey !== undefined && explicitApiKey !== null && explicitApiKey.trim().length === 0) {
    return {};
  }

  if (explicitBearerToken !== undefined && explicitBearerToken !== null && explicitBearerToken.trim().length === 0) {
    return {};
  }

  if (isJwtMode) {
    const token =
      explicitBearerToken !== undefined && explicitBearerToken !== null && explicitBearerToken.trim().length > 0
        ? explicitBearerToken.trim()
        : getLiveJwtTokenFromEnvSync();

    if (token.length === 0) {
      return {};
    }

    return { Authorization: `Bearer ${token}` };
  }

  const key = pickApiKey(explicitApiKey);

  if (key === undefined) {
    return {};
  }

  return { "X-Api-Key": key };
}

/** JSON request headers. Pass `""` to force **no** auth (negative tests). Omit argument for default credentials. */
export function liveJsonHeaders(explicitApiKey?: string | null): Record<string, string> {
  return {
    ...pickAuthHeaders(explicitApiKey),
    Accept: "application/json",
    "Content-Type": "application/json",
  };
}

/** GET JSON headers. Pass `""` to omit auth. */
export function liveAcceptHeaders(explicitApiKey?: string | null): Record<string, string> {
  return {
    ...pickAuthHeaders(explicitApiKey),
    Accept: "application/json",
  };
}

/**
 * GET JSON headers with explicit Bearer token (JWT mode). Pass `""` for no `Authorization` header.
 * Use for invalid-token negative tests; ApiKey mode ignores `token` and uses {@link pickAuthHeaders} key path.
 */
export function liveBearerAcceptHeaders(token?: string | null): Record<string, string> {
  return {
    ...pickAuthHeaders(undefined, token),
    Accept: "application/json",
  };
}

function liveBinaryAcceptHeaders(accept: string, explicitApiKey?: string | null): Record<string, string> {
  return {
    ...pickAuthHeaders(explicitApiKey),
    Accept: accept,
  };
}

async function throwIfNotOk(res: APIResponse, label: string): Promise<void> {
  if (res.ok()) {
    return;
  }

  const text = await res.text();
  const snippet = text.slice(0, 500);

  throw new Error(`${label} failed ${res.status()}: ${snippet}`);
}

/** Waits after HTTP 429 using `Retry-After` when present (capped), else a short default. */
async function delayAfterRateLimitedResponse(res: APIResponse): Promise<void> {
  const headers = res.headers();
  const retryAfterRaw = headers["retry-after"] ?? headers["Retry-After"];
  const seconds = retryAfterRaw ? Number.parseInt(String(retryAfterRaw).trim(), 10) : Number.NaN;
  const ms =
    Number.isFinite(seconds) && seconds > 0 ? Math.min(seconds * 1000, 60_000) : 2500;

  await new Promise((r) => setTimeout(r, ms));
}

/** POST `/v1/architecture/request` — raw response for negative-path tests (400/422). */
export async function postArchitectureRequestRaw(
  request: APIRequestContext,
  body: unknown,
  tenantScope?: LiveTenantScopeHeaders | null,
): Promise<APIResponse> {
  return request.post(`${liveApiBase}/v1/architecture/request`, {
    data: body,
    headers: mergeTenantScope(liveJsonHeaders(), tenantScope),
  });
}

/** Mutating architecture POSTs share one API with many live specs — retry fixed-window 429 and brief 5xx. */
const maxArchitectureMutationAttempts = 8;

/** POST `/v1/architecture/request` — create a new architecture run. */
export async function createRun(
  request: APIRequestContext,
  body: Record<string, unknown>,
  tenantScope?: LiveTenantScopeHeaders | null,
): Promise<{ runId: string }> {
  for (let attempt = 0; attempt < maxArchitectureMutationAttempts; attempt++) {
    const res = await postArchitectureRequestRaw(request, body, tenantScope);

    if (res.status() === 429 && attempt < maxArchitectureMutationAttempts - 1) {
      await delayAfterRateLimitedResponse(res);

      continue;
    }

    if (res.status() >= 500 && res.status() < 600 && attempt < maxArchitectureMutationAttempts - 1) {
      await new Promise((r) => setTimeout(r, 500));

      continue;
    }

    await throwIfNotOk(res, "POST /v1/architecture/request");

    const created = (await res.json()) as { run?: { runId?: string } };
    const runId = created.run?.runId;

    if (!runId) {
      throw new Error("Create run response missing run.runId");
    }

    return { runId };
  }

  throw new Error("createRun: retry loop exhausted");
}

/** POST `/v1/architecture/run/{runId}/execute` — run agents (Simulator in CI). */
export async function executeRun(
  request: APIRequestContext,
  runId: string,
  tenantScope?: LiveTenantScopeHeaders | null,
): Promise<unknown> {
  for (let attempt = 0; attempt < maxArchitectureMutationAttempts; attempt++) {
    const res = await request.post(`${liveApiBase}/v1/architecture/run/${runId}/execute`, {
      headers: mergeTenantScope(liveAcceptHeaders(), tenantScope),
    });

    if (res.status() === 429 && attempt < maxArchitectureMutationAttempts - 1) {
      await delayAfterRateLimitedResponse(res);

      continue;
    }

    if (res.status() >= 500 && res.status() < 600 && attempt < maxArchitectureMutationAttempts - 1) {
      await new Promise((r) => setTimeout(r, 500));

      continue;
    }

    await throwIfNotOk(res, "POST /v1/architecture/run/.../execute");

    return res.json();
  }

  throw new Error("executeRun: retry loop exhausted");
}

/** POST `/v1/architecture/run/{runId}/commit` — merge and persist golden manifest. */
export async function commitRun(
  request: APIRequestContext,
  runId: string,
  tenantScope?: LiveTenantScopeHeaders | null,
): Promise<CommitRunResponseJson> {
  for (let attempt = 0; attempt < maxArchitectureMutationAttempts; attempt++) {
    const res = await request.post(`${liveApiBase}/v1/architecture/run/${runId}/commit`, {
      headers: mergeTenantScope(liveAcceptHeaders(), tenantScope),
    });

    if (res.status() === 429 && attempt < maxArchitectureMutationAttempts - 1) {
      await delayAfterRateLimitedResponse(res);

      continue;
    }

    if (res.status() >= 500 && res.status() < 600 && attempt < maxArchitectureMutationAttempts - 1) {
      await new Promise((r) => setTimeout(r, 500));

      continue;
    }

    await throwIfNotOk(res, "POST /v1/architecture/run/.../commit");

    return res.json() as Promise<CommitRunResponseJson>;
  }

  throw new Error("commitRun: retry loop exhausted");
}

/**
 * Same as {@link commitRun} but returns the raw response for negative-path assertions (409, 404, …).
 * Retries **429** / transient **5xx** only so callers still see the first definitive 4xx (e.g. 404) body.
 */
export async function commitRunRaw(
  request: APIRequestContext,
  runId: string,
  tenantScope?: LiveTenantScopeHeaders | null,
): Promise<APIResponse> {
  for (let attempt = 0; attempt < maxArchitectureMutationAttempts; attempt++) {
    const res = await request.post(`${liveApiBase}/v1/architecture/run/${runId}/commit`, {
      headers: mergeTenantScope(liveAcceptHeaders(), tenantScope),
    });

    if (res.status() === 429 && attempt < maxArchitectureMutationAttempts - 1) {
      await delayAfterRateLimitedResponse(res);

      continue;
    }

    if (res.status() >= 500 && res.status() < 600 && attempt < maxArchitectureMutationAttempts - 1) {
      await new Promise((r) => setTimeout(r, 500));

      continue;
    }

    return res;
  }

  throw new Error("commitRunRaw: retry loop exhausted");
}

/** Minimal commit response shape for E2E (camelCase JSON). */
export type CommitRunResponseJson = {
  manifest?: {
    metadata?: { manifestVersion?: string };
  };
};

/** GET `/v1/architecture/run/{runId}` — raw response (404/409 negative paths). */
export async function getRunDetailsRaw(
  request: APIRequestContext,
  runId: string,
  tenantScope?: LiveTenantScopeHeaders | null,
): Promise<APIResponse> {
  return request.get(`${liveApiBase}/v1/architecture/run/${runId}`, {
    headers: mergeTenantScope(liveAcceptHeaders(), tenantScope),
  });
}

/** GET `/v1/architecture/run/{runId}` — run aggregate including golden manifest id after commit. */
export async function getRunDetails(
  request: APIRequestContext,
  runId: string,
  tenantScope?: LiveTenantScopeHeaders | null,
): Promise<RunDetailsJson> {
  const res = await getRunDetailsRaw(request, runId, tenantScope);

  await throwIfNotOk(res, "GET /v1/architecture/run/...");

  return res.json() as Promise<RunDetailsJson>;
}

/** Polling must survive transient 5xx and fixed-window 429 when many live specs share one API process. */
const maxRunDetailPollAttempts = 16;

/**
 * Same as {@link getRunDetails} but retries on HTTP 5xx (transient API/SQL) and 429 (rate limit) during polling.
 */
export async function getRunDetailsWithTransientRetries(
  request: APIRequestContext,
  runId: string,
  tenantScope?: LiveTenantScopeHeaders | null,
): Promise<RunDetailsJson> {
  for (let attempt = 0; attempt < maxRunDetailPollAttempts; attempt++) {
    const res = await request.get(`${liveApiBase}/v1/architecture/run/${runId}`, {
      headers: mergeTenantScope(liveAcceptHeaders(), tenantScope),
    });
    const code = res.status();

    if (code === 429 && attempt < maxRunDetailPollAttempts - 1) {
      await delayAfterRateLimitedResponse(res);

      continue;
    }

    if (code >= 500 && code < 600 && attempt < maxRunDetailPollAttempts - 1) {
      await new Promise((r) => setTimeout(r, 500));

      continue;
    }

    await throwIfNotOk(res, "GET /v1/architecture/run/...");

    return res.json() as Promise<RunDetailsJson>;
  }

  throw new Error("getRunDetailsWithTransientRetries: retry loop exhausted");
}

/**
 * Polls GET run detail until status is ReadyForCommit (4), Committed (5), or timeout.
 * Throws if the run reaches Failed (6) first.
 */
export async function waitForReadyForCommit(
  request: APIRequestContext,
  runId: string,
  timeoutMs: number,
  tenantScope?: LiveTenantScopeHeaders | null,
): Promise<void> {
  const deadline = Date.now() + timeoutMs;

  while (Date.now() < deadline) {
    const detail = await getRunDetailsWithTransientRetries(request, runId, tenantScope);
    const status = detail.run?.status;

    if (status === 4 || status === "ReadyForCommit") {
      return;
    }

    if (status === 5 || status === "Committed") {
      return;
    }

    if (status === 6 || status === "Failed") {
      throw new Error(`Run ${runId} reached Failed before ReadyForCommit`);
    }

    await new Promise((r) => setTimeout(r, 2000));
  }

  throw new Error(`Run ${runId} did not reach ReadyForCommit within ${timeoutMs}ms`);
}

/** Row from `GET /v1/architecture/runs` (coordinator list). */
export type ArchitectureRunListItemJson = {
  runId?: string;
  status?: string;
  requestId?: string;
  currentManifestVersion?: string | null;
  systemName?: string | null;
};

/** Converts a 32-char hex run id to hyphenated GUID for API routes that use `{runId:guid}`. */
export function toRunGuidPathSegment(runId: string): string {
  const n = runId.trim();

  if (n.includes("-")) {
    return n;
  }

  if (n.length === 32 && /^[0-9a-fA-F]+$/.test(n)) {
    return `${n.slice(0, 8)}-${n.slice(8, 12)}-${n.slice(12, 16)}-${n.slice(16, 20)}-${n.slice(20)}`;
  }

  return n;
}

/** True when API status is {@link ArchitectureRunStatus.Committed} (numeric 5 or string "Committed"). */
export function isArchitectureRunStatusCommitted(status: number | string | undefined): boolean {
  if (status === undefined || status === null) {
    return false;
  }

  if (typeof status === "number") {
    return status === 5;
  }

  return /^committed$/i.test(String(status).trim());
}

/** Polls GET run detail until status is Committed or timeout (post-commit / read-your-writes). */
export async function waitForRunDetailCommitted(
  request: APIRequestContext,
  runId: string,
  timeoutMs: number,
  tenantScope?: LiveTenantScopeHeaders | null,
): Promise<void> {
  const deadline = Date.now() + timeoutMs;

  while (Date.now() < deadline) {
    const detail = await getRunDetailsWithTransientRetries(request, runId, tenantScope);

    if (isArchitectureRunStatusCommitted(detail.run?.status)) {
      return;
    }

    await new Promise((r) => setTimeout(r, 1000));
  }

  throw new Error(`Run ${runId} did not reach Committed (GET /v1/architecture/run/{id}) within ${timeoutMs}ms`);
}

/** Polls GET /v1/architecture/runs until the row shows Committed or timeout (dashboard list consistency). */
export async function waitForArchitectureRunListCommitted(
  request: APIRequestContext,
  runId: string,
  timeoutMs: number,
): Promise<void> {
  const deadline = Date.now() + timeoutMs;

  while (Date.now() < deadline) {
    const rows = await listArchitectureRuns(request);
    const row = rows.find((r) => r.runId === runId);

    if (row !== undefined && isArchitectureRunStatusCommitted(row.status)) {
      return;
    }

    await new Promise((r) => setTimeout(r, 1500));
  }

  throw new Error(`Run ${runId} did not show Committed on GET /v1/architecture/runs within ${timeoutMs}ms`);
}

/** GET `/v1/architecture/runs` — recent runs in scope (dashboard / picker). */
export async function listArchitectureRuns(request: APIRequestContext): Promise<ArchitectureRunListItemJson[]> {
  const res = await request.get(`${liveApiBase}/v1/architecture/runs`, {
    headers: liveAcceptHeaders(),
  });

  await throwIfNotOk(res, "GET /v1/architecture/runs");

  return res.json() as Promise<ArchitectureRunListItemJson[]>;
}

/** POST approve without throwing — use for negative-path assertions (`expect.soft` + status/body). */
export async function postGovernanceApproveRaw(
  request: APIRequestContext,
  approvalRequestId: string,
  body: { reviewedBy: string; reviewComment?: string | null },
  options?: { apiKey?: string | null },
): Promise<APIResponse> {
  return request.post(`${liveApiBase}/v1/governance/approval-requests/${approvalRequestId}/approve`, {
    data: {
      reviewedBy: body.reviewedBy,
      reviewComment: body.reviewComment ?? null,
    },
    headers: liveJsonHeaders(options?.apiKey),
  });
}

export type RunDetailsJson = {
  run?: {
    goldenManifestId?: string | null;
    /** Numeric enum from API JSON, or string name when serialized as string. */
    status?: number | string;
  };
};

/** POST `/v1/governance/approval-requests` — submit promotion approval request. */
export async function createApprovalRequest(
  request: APIRequestContext,
  body: CreateGovernanceApprovalRequestBody,
): Promise<GovernanceApprovalRequestJson> {
  const res = await request.post(`${liveApiBase}/v1/governance/approval-requests`, {
    data: {
      runId: body.runId,
      manifestVersion: body.manifestVersion,
      sourceEnvironment: body.sourceEnvironment,
      targetEnvironment: body.targetEnvironment,
      requestComment: body.requestComment ?? null,
    },
    headers: liveJsonHeaders(),
  });

  await throwIfNotOk(res, "POST /v1/governance/approval-requests");

  return res.json() as Promise<GovernanceApprovalRequestJson>;
}

export type CreateGovernanceApprovalRequestBody = {
  runId: string;
  manifestVersion: string;
  sourceEnvironment: string;
  targetEnvironment: string;
  requestComment?: string;
};

export type GovernanceApprovalRequestJson = {
  approvalRequestId?: string;
  status?: string;
  runId?: string;
};

/** POST `/v1/governance/approval-requests/{id}/approve`. Use a different `reviewedBy` than the submitter to satisfy segregation of duties. */
export async function approveGovernanceRequest(
  request: APIRequestContext,
  approvalRequestId: string,
  body: { reviewedBy: string; reviewComment?: string },
  options?: { apiKey?: string | null },
): Promise<GovernanceApprovalRequestJson> {
  const res = await request.post(
    `${liveApiBase}/v1/governance/approval-requests/${approvalRequestId}/approve`,
    {
      data: {
        reviewedBy: body.reviewedBy,
        reviewComment: body.reviewComment ?? null,
      },
      headers: liveJsonHeaders(options?.apiKey),
    },
  );

  await throwIfNotOk(res, "POST /v1/governance/approval-requests/.../approve");

  return res.json() as Promise<GovernanceApprovalRequestJson>;
}

/** POST `/v1/governance/approval-requests/{id}/reject`. */
export async function rejectGovernanceRequest(
  request: APIRequestContext,
  approvalRequestId: string,
  body: { reviewedBy: string; reviewComment?: string },
  options?: { apiKey?: string | null },
): Promise<GovernanceApprovalRequestJson> {
  const res = await request.post(
    `${liveApiBase}/v1/governance/approval-requests/${approvalRequestId}/reject`,
    {
      data: {
        reviewedBy: body.reviewedBy,
        reviewComment: body.reviewComment ?? null,
      },
      headers: liveJsonHeaders(options?.apiKey),
    },
  );

  await throwIfNotOk(res, "POST /v1/governance/approval-requests/.../reject");

  return res.json() as Promise<GovernanceApprovalRequestJson>;
}

/** POST reject without throwing — for negative-path assertions. */
export async function postGovernanceRejectRaw(
  request: APIRequestContext,
  approvalRequestId: string,
  body: { reviewedBy: string; reviewComment?: string | null },
  options?: { apiKey?: string | null },
): Promise<APIResponse> {
  return request.post(`${liveApiBase}/v1/governance/approval-requests/${approvalRequestId}/reject`, {
    data: {
      reviewedBy: body.reviewedBy,
      reviewComment: body.reviewComment ?? null,
    },
    headers: liveJsonHeaders(options?.apiKey),
  });
}

/** GET `/v1/audit/search` — filtered audit events (optional `runId`, `correlationId`, `eventType`). */
export async function searchAudit(
  request: APIRequestContext,
  params: {
    runId?: string;
    correlationId?: string;
    eventType?: string;
    take?: string;
    tenantId?: string;
    workspaceId?: string;
    projectId?: string;
  },
): Promise<AuditEventJson[]> {
  if (!params.runId && !params.correlationId && !params.eventType) {
    throw new Error("searchAudit: provide runId, correlationId, and/or eventType");
  }

  const query: Record<string, string> = { take: params.take ?? "100" };

  if (params.runId) {
    query.runId = params.runId;
  }

  if (params.correlationId) {
    query.correlationId = params.correlationId;
  }

  if (params.eventType) {
    query.eventType = params.eventType;
  }

  const scopeHeaders: Record<string, string> = {};

  if (
    params.tenantId !== undefined &&
    params.tenantId.trim().length > 0 &&
    params.workspaceId !== undefined &&
    params.workspaceId.trim().length > 0 &&
    params.projectId !== undefined &&
    params.projectId.trim().length > 0
  ) {
    scopeHeaders["x-tenant-id"] = params.tenantId.trim();
    scopeHeaders["x-workspace-id"] = params.workspaceId.trim();
    scopeHeaders["x-project-id"] = params.projectId.trim();
  }

  const maxAuditSearchAttempts = 8;

  for (let attempt = 0; attempt < maxAuditSearchAttempts; attempt++) {
    const res = await request.get(`${liveApiBase}/v1/audit/search`, {
      params: query,
      headers: { ...liveAcceptHeaders(), ...scopeHeaders },
    });

    if (res.status() === 429 && attempt < maxAuditSearchAttempts - 1) {
      await delayAfterRateLimitedResponse(res);

      continue;
    }

    await throwIfNotOk(res, "GET /v1/audit/search");

    return res.json() as Promise<AuditEventJson[]>;
  }

  throw new Error("searchAudit: retry loop exhausted");
}

/** GET `/v1/audit` — recent audit events for scope (newest first). */
export async function listRecentAudit(
  request: APIRequestContext,
  take = 200,
): Promise<AuditEventJson[]> {
  const res = await request.get(`${liveApiBase}/v1/audit`, {
    params: { take: String(Math.min(500, Math.max(1, take))) },
    headers: liveAcceptHeaders(),
  });

  await throwIfNotOk(res, "GET /v1/audit");

  return res.json() as Promise<AuditEventJson[]>;
}

export type AuditEventJson = {
  eventType?: string;
  correlationId?: string | null;
};

/** GET `/v1/artifacts/runs/{runId}/export` — ZIP of committed run (binary). */
export async function getRunExportZip(request: APIRequestContext, runId: string): Promise<APIResponse> {
  return request.get(`${liveApiBase}/v1/artifacts/runs/${runId}/export`, {
    headers: liveBinaryAcceptHeaders("application/zip, application/octet-stream, */*"),
  });
}

/** Minimal policy pack content JSON (matches `PolicyPackContentDocument` shape used in API tests). */
export function minimalPolicyPackContentJson(complianceKey: string): string {
  return JSON.stringify({
    complianceRuleIds: [],
    complianceRuleKeys: [complianceKey],
    alertRuleIds: [],
    compositeAlertRuleIds: [],
    advisoryDefaults: {},
    metadata: { liveE2e: "true" },
  });
}

/** POST `/v1/policy-packs` — create pack + initial draft version `1.0.0`. */
export async function createPolicyPack(
  request: APIRequestContext,
  body: {
    name: string;
    description?: string;
    packType: string;
    initialContentJson: string;
  },
): Promise<{ policyPackId: string }> {
  const res = await request.post(`${liveApiBase}/v1/policy-packs`, {
    data: {
      name: body.name,
      description: body.description ?? "",
      packType: body.packType,
      initialContentJson: body.initialContentJson,
    },
    headers: liveJsonHeaders(),
  });

  await throwIfNotOk(res, "POST /v1/policy-packs");

  const created = (await res.json()) as { policyPackId?: string };
  const policyPackId = created.policyPackId;

  if (!policyPackId) {
    throw new Error("Create policy pack response missing policyPackId");
  }

  return { policyPackId };
}

/** POST `/v1/policy-packs/{id}/publish` — publish or upsert version. */
export async function publishPolicyPackVersion(
  request: APIRequestContext,
  policyPackId: string,
  body: { version: string; contentJson: string },
): Promise<unknown> {
  const res = await request.post(`${liveApiBase}/v1/policy-packs/${policyPackId}/publish`, {
    data: { version: body.version, contentJson: body.contentJson },
    headers: liveJsonHeaders(),
  });

  await throwIfNotOk(res, "POST /v1/policy-packs/.../publish");

  return res.json();
}

/** POST `/v1/policy-packs/{id}/assign` — assign published version to scope tier. */
export async function assignPolicyPack(
  request: APIRequestContext,
  policyPackId: string,
  body: { version: string; scopeLevel?: string; isPinned?: boolean },
): Promise<unknown> {
  const res = await request.post(`${liveApiBase}/v1/policy-packs/${policyPackId}/assign`, {
    data: {
      version: body.version,
      scopeLevel: body.scopeLevel ?? "Project",
      isPinned: body.isPinned ?? false,
    },
    headers: liveJsonHeaders(),
  });

  await throwIfNotOk(res, "POST /v1/policy-packs/.../assign");

  return res.json();
}

/** GET `/v1/policy-packs/effective` — resolved packs for current scope. */
export async function getEffectivePolicyPacks(request: APIRequestContext): Promise<{
  packs?: { policyPackId?: string; version?: string }[];
}> {
  const res = await request.get(`${liveApiBase}/v1/policy-packs/effective`, {
    headers: liveAcceptHeaders(),
  });

  await throwIfNotOk(res, "GET /v1/policy-packs/effective");

  return res.json() as Promise<{ packs?: { policyPackId?: string; version?: string }[] }>;
}

/** GET `/v1/authority/compare/runs` — compare two authority runs by id (Guid string, with or without dashes). */
export async function compareAuthorityRuns(
  request: APIRequestContext,
  leftRunId: string,
  rightRunId: string,
): Promise<APIResponse> {
  return request.get(`${liveApiBase}/v1/authority/compare/runs`, {
    params: { leftRunId, rightRunId },
    headers: liveAcceptHeaders(),
  });
}

/** POST `/v1/advisory/scans` — schedule advisory scan for a run (2xx/409/404 for capability gaps). */
export async function postAdvisoryScanRaw(
  request: APIRequestContext,
  body: { runId: string; description?: string },
): Promise<APIResponse> {
  return request.post(`${liveApiBase}/v1/advisory/scans`, {
    data: { runId: body.runId, description: body.description ?? "" },
    headers: liveJsonHeaders(),
  });
}

/** POST `/v1/replay/run/{runId}` — authority replay (raw for 404 skip in live E2E). */
export async function postReplayRunRaw(request: APIRequestContext, runId: string): Promise<APIResponse> {
  return request.post(`${liveApiBase}/v1/replay/run/${runId}`, {
    headers: liveAcceptHeaders(),
  });
}

/** POST `/v1/reports/analysis` — analysis report for a run. */
export async function postAnalysisReportRaw(
  request: APIRequestContext,
  body: { runId: string },
): Promise<APIResponse> {
  return request.post(`${liveApiBase}/v1/reports/analysis`, {
    data: { runId: body.runId },
    headers: liveJsonHeaders(),
  });
}

/** GET DOCX consulting export for a run (raw for optional 404). */
export async function getDocxArchitecturePackageExportRaw(
  request: APIRequestContext,
  runId: string,
): Promise<APIResponse> {
  return request.get(`${liveApiBase}/v1/exports/docx/runs/${runId}/architecture-package`, {
    headers: liveBinaryAcceptHeaders("application/vnd.openxmlformats-officedocument.wordprocessingml.document, */*"),
  });
}

/** POST `/v1/alert-rules` — create alert rule (raw for status assertions). */
export async function postAlertRuleRaw(
  request: APIRequestContext,
  body: {
    name: string;
    ruleType: string;
    severity: string;
    thresholdValue: number;
    isEnabled: boolean;
    targetChannelType: string;
    metadataJson: string;
  },
): Promise<APIResponse> {
  return request.post(`${liveApiBase}/v1/alert-rules`, {
    data: body,
    headers: liveJsonHeaders(),
  });
}

/** GET `/v1/alert-rules` — list rules. */
export async function getAlertRulesRaw(request: APIRequestContext): Promise<APIResponse> {
  return request.get(`${liveApiBase}/v1/alert-rules`, {
    headers: liveAcceptHeaders(),
  });
}

/** GET `/v1/graph/runs/{runGuid}` — knowledge graph for run. */
export async function getGraphForRunRaw(request: APIRequestContext, runGuidPathSegment: string): Promise<APIResponse> {
  return request.get(`${liveApiBase}/v1/graph/runs/${runGuidPathSegment}`, {
    headers: liveAcceptHeaders(),
  });
}

/** POST `/v1/ask` — RAG-style question (raw; may be 503 when LLM unavailable). */
export async function postAskRaw(
  request: APIRequestContext,
  body: { runId: string; question: string },
): Promise<APIResponse> {
  return request.post(`${liveApiBase}/v1/ask`, {
    data: { runId: body.runId, question: body.question },
    headers: liveJsonHeaders(),
  });
}

export type DigestSubscriptionJson = {
  subscriptionId?: string;
  name?: string;
  channelType?: string;
  destination?: string;
  isEnabled?: boolean;
};

/** POST `/v1/digest-subscriptions` — create digest route (ExecuteAuthority). */
export async function createDigestSubscription(
  request: APIRequestContext,
  body: { name: string; channelType: string; destination: string; isEnabled?: boolean; metadataJson?: string },
): Promise<DigestSubscriptionJson> {
  const res = await request.post(`${liveApiBase}/v1/digest-subscriptions`, {
    data: {
      name: body.name,
      channelType: body.channelType,
      destination: body.destination,
      isEnabled: body.isEnabled ?? true,
      metadataJson: body.metadataJson ?? "{}",
    },
    headers: liveJsonHeaders(),
  });

  await throwIfNotOk(res, "POST /v1/digest-subscriptions");

  return res.json() as Promise<DigestSubscriptionJson>;
}

/** GET `/v1/digest-subscriptions` — list subscriptions in scope. */
export async function listDigestSubscriptions(request: APIRequestContext): Promise<DigestSubscriptionJson[]> {
  const res = await request.get(`${liveApiBase}/v1/digest-subscriptions`, {
    headers: liveAcceptHeaders(),
  });

  await throwIfNotOk(res, "GET /v1/digest-subscriptions");

  return res.json() as Promise<DigestSubscriptionJson[]>;
}

/** POST `/v1/digest-subscriptions/{id}/toggle` — flip enabled flag. */
export async function toggleDigestSubscription(
  request: APIRequestContext,
  subscriptionId: string,
): Promise<DigestSubscriptionJson> {
  const res = await request.post(`${liveApiBase}/v1/digest-subscriptions/${subscriptionId}/toggle`, {
    headers: liveAcceptHeaders(),
  });

  await throwIfNotOk(res, "POST /v1/digest-subscriptions/.../toggle");

  return res.json() as Promise<DigestSubscriptionJson>;
}

/** Headers for non-production `POST /v1/e2e/*` harness routes (must match `ArchLucid:E2eHarness:SharedSecret` on the API). */
export function liveE2eHarnessHeaders(): Record<string, string> {
  const s = process.env.LIVE_E2E_HARNESS_SECRET?.trim() ?? "";

  if (s.length < 16) {
    throw new Error("LIVE_E2E_HARNESS_SECRET must be set to >= 16 chars for harness calls.");
  }

  return {
    "X-ArchLucid-E2e-Harness-Secret": s,
    Accept: "application/json",
    "Content-Type": "application/json",
  };
}

/** POST `/v1/e2e/trial/set-expires` — clock harness (SQL updates `TrialExpiresUtc`). */
export async function postHarnessTrialSetExpires(
  request: APIRequestContext,
  tenantId: string,
  expiresUtcIso: string,
): Promise<APIResponse> {
  return request.post(`${liveApiBase}/v1/e2e/trial/set-expires`, {
    headers: liveE2eHarnessHeaders(),
    data: { tenantId, expiresUtc: expiresUtcIso },
  });
}

/** POST `/v1/e2e/billing/simulate-subscription-activated` — invokes billing activator (Stripe-style outcome). */
export async function postHarnessBillingSimulateActivated(
  request: APIRequestContext,
  body: Record<string, unknown>,
): Promise<APIResponse> {
  return request.post(`${liveApiBase}/v1/e2e/billing/simulate-subscription-activated`, {
    headers: liveE2eHarnessHeaders(),
    data: body,
  });
}

/** GET `/v1/tenant/trial-status` for the given tenant scope. */
export async function getTenantTrialStatus(
  request: APIRequestContext,
  scope: LiveTenantScopeHeaders,
): Promise<{
  status?: string;
  daysRemaining?: number | null;
  trialRunsUsed?: number;
  trialRunsLimit?: number | null;
  trialSeatsUsed?: number;
  trialSeatsLimit?: number | null;
  trialSampleRunId?: string | null;
  trialWelcomeRunId?: string | null;
  trialExpiresUtc?: string | null;
  firstCommitUtc?: string | null;
  baselineReviewCycleHours?: number | null;
  baselineReviewCycleSource?: string | null;
  baselineReviewCycleCapturedUtc?: string | null;
}> {
  const res = await request.get(`${liveApiBase}/v1/tenant/trial-status`, {
    headers: mergeTenantScope(liveAcceptHeaders(), scope),
  });

  await throwIfNotOk(res, "GET /v1/tenant/trial-status");

  return res.json() as Promise<{
    status?: string;
    daysRemaining?: number | null;
    trialRunsUsed?: number;
    trialRunsLimit?: number | null;
    trialSeatsUsed?: number;
    trialSeatsLimit?: number | null;
    trialSampleRunId?: string | null;
    trialWelcomeRunId?: string | null;
    trialExpiresUtc?: string | null;
    firstCommitUtc?: string | null;
    baselineReviewCycleHours?: number | null;
    baselineReviewCycleSource?: string | null;
    baselineReviewCycleCapturedUtc?: string | null;
  }>;
}
