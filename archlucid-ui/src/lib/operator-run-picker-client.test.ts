import { afterEach, describe, expect, it, vi } from "vitest";

vi.mock("@/lib/api", () => ({
  listRunsByProjectPaged: vi.fn(),
}));

import { listRunsByProjectPaged } from "@/lib/api";
import { loadProjectRunsMergedWithDemoFallback } from "@/lib/operator-run-picker-client";

const mockList = vi.mocked(listRunsByProjectPaged);

describe("loadProjectRunsMergedWithDemoFallback", () => {
  const prevDemo = process.env.NEXT_PUBLIC_DEMO_MODE;

  afterEach(() => {
    process.env.NEXT_PUBLIC_DEMO_MODE = prevDemo;
    vi.clearAllMocks();
  });

  it("returns API items when non-empty", async () => {
    mockList.mockResolvedValue({
      items: [
        {
          runId: "11111111-1111-1111-1111-111111111111",
          projectId: "default",
          createdUtc: "2026-01-01T00:00:00.000Z",
          description: "Alpha run",
        },
      ],
      totalCount: 1,
      page: 1,
      pageSize: 50,
      hasMore: false,
    });

    const { items, loadError } = await loadProjectRunsMergedWithDemoFallback("default");

    expect(loadError).toBe(false);
    expect(items).toHaveLength(1);
    expect(items[0]?.runId).toBe("11111111-1111-1111-1111-111111111111");
  });

  it("injects single showcase row when list is empty and demo mode is on", async () => {
    process.env.NEXT_PUBLIC_DEMO_MODE = "true";
    mockList.mockResolvedValue({
      items: [],
      totalCount: 0,
      page: 1,
      pageSize: 50,
      hasMore: false,
    });

    const { items, loadError } = await loadProjectRunsMergedWithDemoFallback("default");

    expect(loadError).toBe(false);
    expect(items).toHaveLength(1);
    expect(items[0]?.runId).toBe("claims-intake-modernization");
  });

  it("prefers compare pair when forCompare and list is empty and demo mode is on", async () => {
    process.env.NEXT_PUBLIC_DEMO_MODE = "true";
    mockList.mockResolvedValue({
      items: [],
      totalCount: 0,
      page: 1,
      pageSize: 50,
      hasMore: false,
    });

    const { items, loadError } = await loadProjectRunsMergedWithDemoFallback("default", { forCompare: true });

    expect(loadError).toBe(false);
    expect(items).toHaveLength(2);
    expect(items.map((r) => r.runId)).toEqual(["claims-intake-run-v1", "claims-intake-run-v2"]);
  });

  it("injects showcase row when API returns zero without curated demo env flags", async () => {
    delete process.env.NEXT_PUBLIC_DEMO_MODE;
    delete process.env.NEXT_PUBLIC_DEMO_STATIC_OPERATOR;
    mockList.mockResolvedValue({
      items: [],
      totalCount: 0,
      page: 1,
      pageSize: 50,
      hasMore: false,
    });

    const { items, loadError } = await loadProjectRunsMergedWithDemoFallback("default");

    expect(loadError).toBe(false);
    expect(items).toHaveLength(1);
    expect(items[0]?.runId).toBe("claims-intake-modernization");
  });

  it("injects single showcase row when list throws and demo mode is off", async () => {
    delete process.env.NEXT_PUBLIC_DEMO_MODE;
    mockList.mockRejectedValue(new Error("network down"));

    const { items, loadError } = await loadProjectRunsMergedWithDemoFallback("default");

    expect(loadError).toBe(false);
    expect(items).toHaveLength(1);
    expect(items[0]?.runId).toBe("claims-intake-modernization");
  });
});
