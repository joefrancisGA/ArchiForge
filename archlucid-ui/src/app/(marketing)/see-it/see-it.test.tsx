import { render, screen } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";

import { loadSeeItDemoPreview } from "./load-see-it-demo-preview";
import { createMinimalDemoPreviewPayload } from "./see-it.fixtures";
import { SeeItMarketingBody } from "./SeeItMarketingBody";

describe("loadSeeItDemoPreview", () => {
  afterEach(() => {
    vi.unstubAllEnvs();
    vi.restoreAllMocks();
  });

  it("returns live payload when fetch returns 200 JSON", async () => {
    vi.stubEnv("NEXT_PUBLIC_DEMO_API_BASE", "https://demo-api.test");

    const livePayload = createMinimalDemoPreviewPayload();
    const fetchFn = vi.fn().mockResolvedValue(
      new Response(JSON.stringify(livePayload), {
        status: 200,
        headers: { "Content-Type": "application/json", ETag: '"fixture-etag"' },
      }),
    );

    const result = await loadSeeItDemoPreview({
      fetchFn,
      readSnapshotFile: () => {
        throw new Error("snapshot must not be read on success path");
      },
    });

    expect(result.source).toBe("live");
    expect(result.etag).toBe('"fixture-etag"');
    expect(result.payload.run.runId).toBe(livePayload.run.runId);
    expect(fetchFn).toHaveBeenCalledWith(
      "https://demo-api.test/v1/demo/preview",
      expect.objectContaining({ method: "GET" }),
    );
  });

  it("returns snapshot payload when fetch throws", async () => {
    vi.stubEnv("NEXT_PUBLIC_DEMO_API_BASE", "https://demo-api.test");

    const snapshotPayload = createMinimalDemoPreviewPayload();
    snapshotPayload.run.runId = "00000000000000000000000000000000";

    const fetchFn = vi.fn().mockRejectedValue(new Error("network down"));

    const result = await loadSeeItDemoPreview({
      fetchFn,
      readSnapshotFile: () => snapshotPayload,
    });

    expect(result.source).toBe("snapshot");
    expect(result.payload.run.runId).toBe("00000000000000000000000000000000");
  });

  it("returns snapshot when response is 304 Not Modified", async () => {
    vi.stubEnv("NEXT_PUBLIC_DEMO_API_BASE", "https://demo-api.test");

    const snapshotPayload = createMinimalDemoPreviewPayload();
    const fetchFn = vi.fn().mockResolvedValue(new Response(null, { status: 304 }));

    const result = await loadSeeItDemoPreview({
      fetchFn,
      readSnapshotFile: () => snapshotPayload,
    });

    expect(result.source).toBe("snapshot");
    expect(result.payload).toBe(snapshotPayload);
  });

  it("returns snapshot when response is 500", async () => {
    vi.stubEnv("NEXT_PUBLIC_DEMO_API_BASE", "https://demo-api.test");

    const snapshotPayload = createMinimalDemoPreviewPayload();
    const fetchFn = vi.fn().mockResolvedValue(new Response("", { status: 500 }));

    const result = await loadSeeItDemoPreview({
      fetchFn,
      readSnapshotFile: () => snapshotPayload,
    });

    expect(result.source).toBe("snapshot");
    expect(result.payload).toBe(snapshotPayload);
  });
});

describe("SeeItMarketingBody", () => {
  it("renders live mode without snapshot notice", () => {
    const payload = createMinimalDemoPreviewPayload();

    render(<SeeItMarketingBody source="live" payload={payload} />);

    expect(screen.getByTestId("see-it-demo-banner")).toHaveTextContent("isDemoData=true");
    expect(screen.queryByTestId("see-it-snapshot-notice")).toBeNull();
    expect(screen.getByTestId("see-it-finding-counts")).toHaveTextContent("findingCount=7");
    expect(screen.getByTestId("see-it-proof-pack-download")).toHaveAttribute(
      "href",
      "/api/proxy/v1/marketing/why-archlucid-pack.pdf",
    );
  });

  it("renders snapshot mode with snapshot notice", () => {
    const payload = createMinimalDemoPreviewPayload();

    render(<SeeItMarketingBody source="snapshot" payload={payload} />);

    expect(screen.getByTestId("see-it-snapshot-notice")).toBeInTheDocument();
  });
});
