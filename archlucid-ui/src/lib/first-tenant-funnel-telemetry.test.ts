import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

import { recordFirstTenantFunnelEvent } from "./first-tenant-funnel-telemetry";

describe("first-tenant-funnel-telemetry", () => {
  let fetchMock: ReturnType<typeof vi.fn>;

  beforeEach(() => {
    window.localStorage.clear();
    fetchMock = vi.fn().mockResolvedValue({ ok: true });
    vi.stubGlobal("fetch", fetchMock);
  });

  afterEach(() => {
    vi.unstubAllGlobals();
    vi.useRealTimers();
  });

  it("never sends a tenant id in the request body (server infers from scope)", () => {
    recordFirstTenantFunnelEvent("signup");

    expect(fetchMock).toHaveBeenCalledWith(
      "/api/proxy/v1/diagnostics/first-tenant-funnel",
      expect.objectContaining({
        method: "POST",
        body: JSON.stringify({ event: "signup" }),
      }),
    );

    const callArgs = fetchMock.mock.calls[0]?.[1] as RequestInit | undefined;
    const body = String(callArgs?.body ?? "");
    expect(body).not.toContain("tenantId");
    expect(body).not.toContain("tenant_id");
    expect(body).not.toContain("userId");
  });

  it("posts every supported event name", () => {
    const events = [
      "tour_opt_in",
      "first_run_started",
      "first_run_committed",
    ] as const;

    for (const event of events) {
      recordFirstTenantFunnelEvent(event);
    }

    expect(fetchMock).toHaveBeenCalledTimes(events.length);

    for (const event of events) {
      expect(fetchMock).toHaveBeenCalledWith(
        "/api/proxy/v1/diagnostics/first-tenant-funnel",
        expect.objectContaining({ body: JSON.stringify({ event }) }),
      );
    }
  });

  it("fires the 30-minute milestone when first_finding_viewed lands within 30 minutes of signup", () => {
    vi.useFakeTimers();
    vi.setSystemTime(new Date("2026-04-24T10:00:00Z"));
    recordFirstTenantFunnelEvent("signup");

    vi.setSystemTime(new Date("2026-04-24T10:25:00Z"));
    fetchMock.mockClear();
    recordFirstTenantFunnelEvent("first_finding_viewed");

    expect(fetchMock).toHaveBeenCalledTimes(2);
    const milestoneCall = fetchMock.mock.calls.find(
      (c) => String((c[1] as RequestInit).body).includes("thirty_minute_milestone"),
    );
    expect(milestoneCall).toBeDefined();
  });

  it("does NOT fire the milestone when first_finding_viewed lands more than 30 minutes after signup", () => {
    vi.useFakeTimers();
    vi.setSystemTime(new Date("2026-04-24T10:00:00Z"));
    recordFirstTenantFunnelEvent("signup");

    vi.setSystemTime(new Date("2026-04-24T11:00:00Z"));
    fetchMock.mockClear();
    recordFirstTenantFunnelEvent("first_finding_viewed");

    expect(fetchMock).toHaveBeenCalledTimes(1);
    const onlyCall = fetchMock.mock.calls[0]?.[1] as RequestInit | undefined;
    expect(String(onlyCall?.body)).toContain("first_finding_viewed");
    expect(String(onlyCall?.body)).not.toContain("thirty_minute_milestone");
  });

  it("fires the milestone at most once per browser", () => {
    vi.useFakeTimers();
    vi.setSystemTime(new Date("2026-04-24T10:00:00Z"));
    recordFirstTenantFunnelEvent("signup");

    vi.setSystemTime(new Date("2026-04-24T10:10:00Z"));
    recordFirstTenantFunnelEvent("first_finding_viewed");
    recordFirstTenantFunnelEvent("first_finding_viewed");

    const milestoneCalls = fetchMock.mock.calls.filter(
      (c) => String((c[1] as RequestInit).body).includes("thirty_minute_milestone"),
    );
    expect(milestoneCalls).toHaveLength(1);
  });
});
