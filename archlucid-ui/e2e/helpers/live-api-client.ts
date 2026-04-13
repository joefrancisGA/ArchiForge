/**
 * Typed helpers for Playwright live-API E2E against ArchLucid.Api (see `live-api-journey.spec.ts`).
 */
import type { APIRequestContext, APIResponse } from "@playwright/test";

/** Base URL for ArchLucid.Api (e.g. http://127.0.0.1:5128). */
export const liveApiBase = process.env.LIVE_API_URL ?? "http://127.0.0.1:5128";

const jsonHeaders = {
  Accept: "application/json",
  "Content-Type": "application/json",
} as const;

async function throwIfNotOk(res: APIResponse, label: string): Promise<void> {
  if (res.ok()) {
    return;
  }

  const text = await res.text();
  const snippet = text.slice(0, 500);

  throw new Error(`${label} failed ${res.status()}: ${snippet}`);
}

/** POST `/v1/architecture/request` — create a new architecture run. */
export async function createRun(
  request: APIRequestContext,
  body: Record<string, unknown>,
): Promise<{ runId: string }> {
  const res = await request.post(`${liveApiBase}/v1/architecture/request`, {
    data: body,
    headers: jsonHeaders,
  });

  await throwIfNotOk(res, "POST /v1/architecture/request");

  const created = (await res.json()) as { run?: { runId?: string } };
  const runId = created.run?.runId;

  if (!runId) {
    throw new Error("Create run response missing run.runId");
  }

  return { runId };
}

/** POST `/v1/architecture/run/{runId}/execute` — run agents (Simulator in CI). */
export async function executeRun(request: APIRequestContext, runId: string): Promise<unknown> {
  const res = await request.post(`${liveApiBase}/v1/architecture/run/${runId}/execute`, {
    headers: { Accept: "application/json" },
  });

  await throwIfNotOk(res, "POST /v1/architecture/run/.../execute");

  return res.json();
}

/** POST `/v1/architecture/run/{runId}/commit` — merge and persist golden manifest. */
export async function commitRun(request: APIRequestContext, runId: string): Promise<CommitRunResponseJson> {
  const res = await request.post(`${liveApiBase}/v1/architecture/run/${runId}/commit`, {
    headers: { Accept: "application/json" },
  });

  await throwIfNotOk(res, "POST /v1/architecture/run/.../commit");

  return res.json() as Promise<CommitRunResponseJson>;
}

/** Minimal commit response shape for E2E (camelCase JSON). */
export type CommitRunResponseJson = {
  manifest?: {
    metadata?: { manifestVersion?: string };
  };
};

/** GET `/v1/architecture/run/{runId}` — run aggregate including golden manifest id after commit. */
export async function getRunDetails(request: APIRequestContext, runId: string): Promise<RunDetailsJson> {
  const res = await request.get(`${liveApiBase}/v1/architecture/run/${runId}`, {
    headers: { Accept: "application/json" },
  });

  await throwIfNotOk(res, "GET /v1/architecture/run/...");

  return res.json() as Promise<RunDetailsJson>;
}

const maxTransientRetriesPerPoll = 3;

/**
 * Same as {@link getRunDetails} but retries a few times on HTTP 5xx (transient API/SQL blips during polling).
 */
export async function getRunDetailsWithTransientRetries(
  request: APIRequestContext,
  runId: string,
): Promise<RunDetailsJson> {
  for (let attempt = 0; attempt <= maxTransientRetriesPerPoll; attempt++) {
    const res = await request.get(`${liveApiBase}/v1/architecture/run/${runId}`, {
      headers: { Accept: "application/json" },
    });
    const code = res.status();

    if (code >= 500 && code < 600 && attempt < maxTransientRetriesPerPoll) {
      await new Promise((r) => setTimeout(r, 500));

      continue;
    }

    await throwIfNotOk(res, "GET /v1/architecture/run/...");

    return res.json() as Promise<RunDetailsJson>;
  }

  throw new Error("getRunDetailsWithTransientRetries: retry loop exhausted");
}

/** Row from `GET /v1/architecture/runs` (coordinator list). */
export type ArchitectureRunListItemJson = {
  runId?: string;
  status?: string;
  requestId?: string;
};

/** GET `/v1/architecture/runs` — recent runs in scope (dashboard / picker). */
export async function listArchitectureRuns(request: APIRequestContext): Promise<ArchitectureRunListItemJson[]> {
  const res = await request.get(`${liveApiBase}/v1/architecture/runs`, {
    headers: { Accept: "application/json" },
  });

  await throwIfNotOk(res, "GET /v1/architecture/runs");

  return res.json() as Promise<ArchitectureRunListItemJson[]>;
}

/** POST approve without throwing — use for negative-path assertions (`expect.soft` + status/body). */
export async function postGovernanceApproveRaw(
  request: APIRequestContext,
  approvalRequestId: string,
  body: { reviewedBy: string; reviewComment?: string | null },
): Promise<APIResponse> {
  return request.post(`${liveApiBase}/v1/governance/approval-requests/${approvalRequestId}/approve`, {
    data: {
      reviewedBy: body.reviewedBy,
      reviewComment: body.reviewComment ?? null,
    },
    headers: jsonHeaders,
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
    headers: jsonHeaders,
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
): Promise<GovernanceApprovalRequestJson> {
  const res = await request.post(
    `${liveApiBase}/v1/governance/approval-requests/${approvalRequestId}/approve`,
    {
      data: {
        reviewedBy: body.reviewedBy,
        reviewComment: body.reviewComment ?? null,
      },
      headers: jsonHeaders,
    },
  );

  await throwIfNotOk(res, "POST /v1/governance/approval-requests/.../approve");

  return res.json() as Promise<GovernanceApprovalRequestJson>;
}

/** GET `/v1/audit/search` — filtered audit events (`runId` and/or `correlationId` query params). */
export async function searchAudit(
  request: APIRequestContext,
  params: { runId?: string; correlationId?: string; take?: string },
): Promise<AuditEventJson[]> {
  if (!params.runId && !params.correlationId) {
    throw new Error("searchAudit: provide runId and/or correlationId");
  }

  const query: Record<string, string> = { take: params.take ?? "100" };

  if (params.runId) {
    query.runId = params.runId;
  }

  if (params.correlationId) {
    query.correlationId = params.correlationId;
  }

  const res = await request.get(`${liveApiBase}/v1/audit/search`, {
    params: query,
    headers: { Accept: "application/json" },
  });

  await throwIfNotOk(res, "GET /v1/audit/search");

  return res.json() as Promise<AuditEventJson[]>;
}

export type AuditEventJson = {
  eventType?: string;
  correlationId?: string | null;
};

/** GET `/v1/artifacts/runs/{runId}/export` — ZIP of committed run (binary). */
export async function getRunExportZip(request: APIRequestContext, runId: string): Promise<APIResponse> {
  return request.get(`${liveApiBase}/v1/artifacts/runs/${runId}/export`, {
    headers: { Accept: "application/zip, application/octet-stream, */*" },
  });
}
