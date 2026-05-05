import { describe, expect, it, vi } from "vitest";

import { countAuditEventsInWindow } from "@/lib/workspace-health-audit-count";

vi.mock("@/lib/api", () => ({
  searchAuditEvents: vi.fn(),
}));

import { searchAuditEvents } from "@/lib/api";

describe("countAuditEventsInWindow", () => {
  it("returns exact when first page has no more", async () => {
    vi.mocked(searchAuditEvents).mockResolvedValueOnce({
      items: [{ eventId: "1" } as never, { eventId: "2" } as never],
      nextCursor: null,
      hasMore: false,
      requestedTake: 500,
    });

    const r = await countAuditEventsInWindow({
      eventType: "GovernancePreCommitBlocked",
      fromUtcIso: "2026-04-01T00:00:00.000Z",
      toUtcIso: "2026-05-01T00:00:00.000Z",
    });

    expect(r.count).toBe(2);
    expect(r.exact).toBe(true);
  });

  it("pages and marks inexact when cap hit", async () => {
    vi.mocked(searchAuditEvents).mockResolvedValue({
      items: new Array(500).fill({ eventId: "x" }) as never[],
      nextCursor: "more",
      hasMore: true,
      requestedTake: 500,
    });

    const r = await countAuditEventsInWindow({
      eventType: "GovernancePreCommitBlocked",
      fromUtcIso: "2026-04-01T00:00:00.000Z",
      toUtcIso: "2026-05-01T00:00:00.000Z",
    });

    expect(r.exact).toBe(false);
    expect(r.count).toBe(500 * 50);
  });
});
