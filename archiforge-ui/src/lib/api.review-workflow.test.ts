import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

import {
  compareGoldenManifestRuns,
  compareRuns,
  explainComparisonRuns,
  getArtifactDescriptor,
  listArtifacts,
} from "./api";

function jsonResponse(data: unknown): Response {
  return new Response(JSON.stringify(data), {
    status: 200,
    headers: { "content-type": "application/json" },
  });
}

describe("API client review/compare contracts (55R smoke)", () => {
  beforeEach(() => {
    vi.stubGlobal("fetch", vi.fn(() => Promise.resolve(jsonResponse([]))));
  });

  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it("listArtifacts requests manifest descriptor list path", async () => {
    const fetchMock = vi.mocked(fetch);

    await listArtifacts("manifest-xyz");

    expect(fetchMock).toHaveBeenCalledTimes(1);
    const url = String(fetchMock.mock.calls[0][0]);

    expect(url).toContain("/v1/artifacts/manifests/manifest-xyz");
    expect(url).not.toContain("/artifact/");
  });

  it("getArtifactDescriptor requests descriptor subresource", async () => {
    const fetchMock = vi.mocked(fetch);
    fetchMock.mockResolvedValueOnce(
      jsonResponse({
        artifactId: "a1",
        artifactType: "Inventory",
        name: "n",
        format: "json",
        createdUtc: "2020-01-01T00:00:00Z",
        contentHash: "h",
      }),
    );

    await getArtifactDescriptor("m1", "a1");

    const url = String(fetchMock.mock.calls[0][0]);

    expect(url).toContain("/v1/artifacts/manifests/m1/artifact/a1/descriptor");
  });

  it("compareRuns encodes left and right run query params", async () => {
    const fetchMock = vi.mocked(fetch);
    fetchMock.mockResolvedValueOnce(
      jsonResponse({
        leftRunId: "L",
        rightRunId: "R",
        runLevelDiffs: [],
      }),
    );

    await compareRuns("left id", "right&run");

    const url = String(fetchMock.mock.calls[0][0]);

    expect(url).toContain("leftRunId=");
    expect(url).toContain("rightRunId=");
    expect(url).toContain(encodeURIComponent("left id"));
    expect(url).toContain(encodeURIComponent("right&run"));
    expect(url).toContain("/v1/authority/compare/runs");
  });

  it("compareGoldenManifestRuns encodes base and target run ids", async () => {
    const fetchMock = vi.mocked(fetch);
    fetchMock.mockResolvedValueOnce(
      jsonResponse({
        baseRunId: "b",
        targetRunId: "t",
        decisionChanges: [],
        requirementChanges: [],
        securityChanges: [],
        topologyChanges: [],
        costChanges: [],
        summaryHighlights: [],
      }),
    );

    await compareGoldenManifestRuns("base=1", "target=2");

    const url = String(fetchMock.mock.calls[0][0]);

    expect(url).toContain("/v1/compare?");
    expect(url).toContain("baseRunId=");
    expect(url).toContain("targetRunId=");
    expect(url).toContain(encodeURIComponent("base=1"));
    expect(url).toContain(encodeURIComponent("target=2"));
  });

  it("explainComparisonRuns encodes base and target run ids for compare narrative", async () => {
    const fetchMock = vi.mocked(fetch);
    fetchMock.mockResolvedValueOnce(
      jsonResponse({
        highLevelSummary: "s",
        majorChanges: [],
        keyTradeoffs: [],
        narrative: "n",
      }),
    );

    await explainComparisonRuns("base id", "target&x");

    const url = String(fetchMock.mock.calls[0][0]);

    expect(url).toContain("/v1/explain/compare/explain?");
    expect(url).toContain("baseRunId=");
    expect(url).toContain("targetRunId=");
    expect(url).toContain(encodeURIComponent("base id"));
    expect(url).toContain(encodeURIComponent("target&x"));
  });
});
