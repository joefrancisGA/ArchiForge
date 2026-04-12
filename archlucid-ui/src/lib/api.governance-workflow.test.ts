import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

import {
  activateEnvironment,
  approveRequest,
  getGovernanceDashboard,
  listActivations,
  listApprovalRequests,
  listPromotions,
  promoteManifest,
  rejectRequest,
  submitApprovalRequest,
} from "./api";

function jsonResponse(data: unknown): Response {
  return new Response(JSON.stringify(data), {
    status: 200,
    headers: { "content-type": "application/json" },
  });
}

describe("API governance workflow (v1/governance)", () => {
  beforeEach(() => {
    vi.stubGlobal("fetch", vi.fn(() => Promise.resolve(jsonResponse([]))));
  });

  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it("listApprovalRequests GETs run-scoped approval-requests", async () => {
    const fetchMock = vi.mocked(fetch);
    fetchMock.mockResolvedValueOnce(jsonResponse([]));

    await listApprovalRequests("run-1&x");

    expect(fetchMock).toHaveBeenCalledTimes(1);
    const url = String(fetchMock.mock.calls[0][0]);
    const init = fetchMock.mock.calls[0][1] as RequestInit | undefined;

    expect(url).toContain("/v1/governance/runs/");
    expect(url).toContain(encodeURIComponent("run-1&x"));
    expect(url).toContain("/approval-requests");
    expect(init?.method).toBeUndefined();
  });

  it("submitApprovalRequest POSTs approval-requests with body", async () => {
    const fetchMock = vi.mocked(fetch);
    fetchMock.mockResolvedValueOnce(
      jsonResponse({
        approvalRequestId: "a1",
        runId: "r1",
        manifestVersion: "v1",
        sourceEnvironment: "dev",
        targetEnvironment: "test",
        status: "Submitted",
        requestedBy: "u1",
        reviewedBy: null,
        requestComment: null,
        reviewComment: null,
        requestedUtc: "2026-01-01T00:00:00Z",
        reviewedUtc: null,
      }),
    );

    await submitApprovalRequest({
      runId: "r1",
      manifestVersion: "v1",
      sourceEnvironment: "dev",
      targetEnvironment: "test",
      requestComment: "please",
    });

    const url = String(fetchMock.mock.calls[0][0]);
    const init = fetchMock.mock.calls[0][1] as RequestInit;

    expect(url).toContain("/v1/governance/approval-requests");
    expect(init.method).toBe("POST");
    expect(JSON.parse(String(init.body))).toEqual({
      runId: "r1",
      manifestVersion: "v1",
      sourceEnvironment: "dev",
      targetEnvironment: "test",
      requestComment: "please",
    });
  });

  it("approveRequest POSTs approve subresource", async () => {
    const fetchMock = vi.mocked(fetch);
    fetchMock.mockResolvedValueOnce(jsonResponse({ approvalRequestId: "aid" }));

    await approveRequest("req/id", { reviewedBy: "alice", reviewComment: "ok" });

    const url = String(fetchMock.mock.calls[0][0]);
    const init = fetchMock.mock.calls[0][1] as RequestInit;

    expect(url).toContain("/v1/governance/approval-requests/");
    expect(url).toContain(encodeURIComponent("req/id"));
    expect(url).toContain("/approve");
    expect(init.method).toBe("POST");
    expect(JSON.parse(String(init.body))).toEqual({
      reviewedBy: "alice",
      reviewComment: "ok",
    });
  });

  it("rejectRequest POSTs reject subresource", async () => {
    const fetchMock = vi.mocked(fetch);
    fetchMock.mockResolvedValueOnce(jsonResponse({ approvalRequestId: "aid" }));

    await rejectRequest("req%2Fid", { reviewedBy: "bob" });

    const url = String(fetchMock.mock.calls[0][0]);
    const init = fetchMock.mock.calls[0][1] as RequestInit;

    expect(url).toContain("/v1/governance/approval-requests/");
    expect(url).toContain(encodeURIComponent("req%2Fid"));
    expect(url).toContain("/reject");
    expect(init.method).toBe("POST");
    expect(JSON.parse(String(init.body))).toEqual({ reviewedBy: "bob" });
  });

  it("promoteManifest POSTs promotions with body", async () => {
    const fetchMock = vi.mocked(fetch);
    fetchMock.mockResolvedValueOnce(
      jsonResponse({
        promotionRecordId: "p1",
        runId: "r1",
        manifestVersion: "v2",
        sourceEnvironment: "dev",
        targetEnvironment: "staging",
        promotedBy: "carol",
        approvalRequestId: "a9",
        notes: "n",
        promotedUtc: "2026-01-02T00:00:00Z",
      }),
    );

    await promoteManifest({
      runId: "r1",
      manifestVersion: "v2",
      sourceEnvironment: "dev",
      targetEnvironment: "staging",
      promotedBy: "carol",
      approvalRequestId: "a9",
      notes: "n",
    });

    const url = String(fetchMock.mock.calls[0][0]);
    const init = fetchMock.mock.calls[0][1] as RequestInit;

    expect(url).toContain("/v1/governance/promotions");
    expect(init.method).toBe("POST");
    expect(JSON.parse(String(init.body))).toEqual({
      runId: "r1",
      manifestVersion: "v2",
      sourceEnvironment: "dev",
      targetEnvironment: "staging",
      promotedBy: "carol",
      approvalRequestId: "a9",
      notes: "n",
    });
  });

  it("activateEnvironment POSTs activations without activatedBy in JSON body", async () => {
    const fetchMock = vi.mocked(fetch);
    fetchMock.mockResolvedValueOnce(
      jsonResponse({
        activationId: "x1",
        runId: "r1",
        manifestVersion: "v1",
        environment: "test",
        isActive: true,
        activatedUtc: "2026-01-03T00:00:00Z",
      }),
    );

    await activateEnvironment({
      runId: "r1",
      manifestVersion: "v1",
      environment: "test",
      activatedBy: "dan",
    });

    const url = String(fetchMock.mock.calls[0][0]);
    const init = fetchMock.mock.calls[0][1] as RequestInit;

    expect(url).toContain("/v1/governance/activations");
    expect(init.method).toBe("POST");
    expect(JSON.parse(String(init.body))).toEqual({
      runId: "r1",
      manifestVersion: "v1",
      environment: "test",
    });
  });

  it("listPromotions GETs run-scoped promotions", async () => {
    const fetchMock = vi.mocked(fetch);
    fetchMock.mockResolvedValueOnce(jsonResponse([]));

    await listPromotions("r99");

    const url = String(fetchMock.mock.calls[0][0]);
    expect(url).toContain("/v1/governance/runs/r99/promotions");
  });

  it("listActivations GETs run-scoped activations", async () => {
    const fetchMock = vi.mocked(fetch);
    fetchMock.mockResolvedValueOnce(jsonResponse([]));

    await listActivations("r88");

    const url = String(fetchMock.mock.calls[0][0]);
    expect(url).toContain("/v1/governance/runs/r88/activations");
  });

  it("getGovernanceDashboard GETs dashboard with query caps", async () => {
    const fetchMock = vi.mocked(fetch);
    fetchMock.mockResolvedValueOnce(
      jsonResponse({
        pendingApprovals: [],
        recentDecisions: [],
        recentChanges: [],
        pendingCount: 0,
      }),
    );

    await getGovernanceDashboard(5, 10, 15);

    const url = String(fetchMock.mock.calls[0][0]);
    expect(url).toContain("/v1/governance/dashboard?");
    expect(url).toContain("maxPending=5");
    expect(url).toContain("maxDecisions=10");
    expect(url).toContain("maxChanges=15");
  });
});
