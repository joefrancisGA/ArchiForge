import { vi } from "vitest";

import type { RecentPilotRunDeltaRow, RecentPilotRunDeltasPayload } from "../types";

/**
 * Shared "MSW-style" fetch handler for the three new BeforeAfterDeltaPanel variant
 * specs. Vitest doesn't pull in MSW in this repo (kept the dep set narrow), so this
 * file gives the variants the same shared-handler ergonomic that MSW handlers would:
 *
 *  - One canonical builder for the recent-deltas payload.
 *  - One canonical install function each spec calls in `beforeEach`.
 *  - URL-keyed dispatch that mirrors how `setupServer` routes requests by path.
 *
 * Keeping it dep-free means the spec surface mirrors prod data shape (the payload
 * matches `ArchLucid.Contracts.Pilots.RecentPilotRunDeltasResponse` 1:1) without
 * adding MSW + its peer deps to the operator-shell bundle path.
 */

const RECENT_DELTAS_URL_FRAGMENT = "/v1/pilots/runs/recent-deltas";

export type FakeRow = Partial<RecentPilotRunDeltaRow> & Pick<RecentPilotRunDeltaRow, "runId">;

export function makeRow(overrides: FakeRow): RecentPilotRunDeltaRow {
  return {
    runId: overrides.runId,
    requestId: overrides.requestId ?? `req-${overrides.runId}`,
    runCreatedUtc: overrides.runCreatedUtc ?? "2026-04-23T10:00:00Z",
    manifestCommittedUtc: overrides.manifestCommittedUtc ?? "2026-04-23T10:30:00Z",
    timeToCommittedManifestTotalSeconds:
      overrides.timeToCommittedManifestTotalSeconds === undefined
        ? 30 * 60
        : overrides.timeToCommittedManifestTotalSeconds,
    totalFindings: overrides.totalFindings ?? 3,
    topFindingSeverity: overrides.topFindingSeverity ?? "High",
    isDemoTenant: overrides.isDemoTenant ?? false,
  };
}

export function makePayload(rows: RecentPilotRunDeltaRow[]): RecentPilotRunDeltasPayload {
  const findings = rows.map((r) => r.totalFindings);
  const seconds = rows
    .map((r) => r.timeToCommittedManifestTotalSeconds)
    .filter((s): s is number => s !== null && Number.isFinite(s));

  return {
    items: rows,
    requestedCount: rows.length,
    returnedCount: rows.length,
    medianTotalFindings: rows.length === 0 ? null : median(findings),
    medianTimeToCommittedManifestTotalSeconds: seconds.length === 0 ? null : median(seconds),
  };
}

function median(values: number[]): number {
  const sorted = [...values].sort((a, b) => a - b);
  const mid = Math.floor(sorted.length / 2);

  return sorted.length % 2 === 1 ? sorted[mid] : (sorted[mid - 1] + sorted[mid]) / 2;
}

export function urlOf(input: RequestInfo | URL): string {
  if (typeof input === "string") return input;
  if (input instanceof URL) return input.toString();

  return (input as Request).url;
}

export function jsonResponse(body: unknown, ok = true): Response {
  return {
    ok,
    json: async () => body,
  } as unknown as Response;
}

/**
 * Installs a `vi.stubGlobal("fetch", ...)` handler that returns
 * `payload` for every request whose URL contains `/v1/pilots/runs/recent-deltas`,
 * and a 404 for anything else. Returns the underlying mock so specs can assert calls.
 */
export function installRecentDeltasFetch(payload: RecentPilotRunDeltasPayload): ReturnType<typeof vi.fn> {
  const handler = vi.fn(async (input: RequestInfo | URL): Promise<Response> => {
    const url = urlOf(input);

    if (url.includes(RECENT_DELTAS_URL_FRAGMENT)) {
      return jsonResponse(payload);
    }

    return jsonResponse({}, false);
  });

  vi.stubGlobal("fetch", handler);

  return handler;
}

/** Same as `installRecentDeltasFetch` but always replies non-ok so variant specs can verify graceful hiding. */
export function installFailingRecentDeltasFetch(): void {
  vi.stubGlobal(
    "fetch",
    vi.fn(async () => jsonResponse({}, false)),
  );
}
