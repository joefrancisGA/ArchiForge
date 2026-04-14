/**
 * Live API + SQL: data retention archival is owned by ArchLucid.Worker (no public HTTP trigger on the API).
 * This file documents the gap and keeps a cheap regression check that committed runs remain listable.
 */
import { expect, test } from "@playwright/test";

import {
  commitRun,
  createRun,
  executeRun,
  listArchitectureRuns,
  liveApiBase,
  waitForReadyForCommit,
  waitForRunDetailCommitted,
} from "./helpers/live-api-client";

test.describe("live-api-archival", () => {
  test.beforeAll(async ({ request }) => {
    const health = await request.get(`${liveApiBase}/health/ready`, { timeout: 60_000 });

    if (!health.ok()) {
      throw new Error(
        `Live API not ready at ${liveApiBase}/health/ready (status ${health.status()}). Start ArchLucid.Api with Sql + DevelopmentBypass.`,
      );
    }
  });

  test("archival trigger — no HTTP surface on ArchLucid.Api for live E2E", () => {
    test.skip(
      true,
      "Run archival is performed by DataArchivalCoordinator in ArchLucid.Worker; ArchLucid.Api exposes no DevelopmentBypass-friendly POST to archive runs. Use worker integration tests or staging drills (docs/runbooks/DATA_ARCHIVAL_HEALTH.md).",
    );
  });

  test("multiple committed runs remain visible on GET /v1/architecture/runs", async ({ request }) => {
    test.setTimeout(300_000);

    const runIds: string[] = [];

    for (let i = 0; i < 2; i++) {
      const createBody = {
        requestId: `E2E-ARCHIVAL-LIST-${Date.now()}-${i}`,
        description: "Live E2E: list consistency smoke (archival placeholder suite).",
        systemName: `ArchivalListTest-${i}`,
        environment: "prod",
        cloudProvider: 1,
        constraints: [] as string[],
        requiredCapabilities: ["SQL"],
        assumptions: [] as string[],
        priorManifestVersion: null as string | null,
      };

      const { runId } = await createRun(request, createBody);
      runIds.push(runId);

      await executeRun(request, runId);
      await waitForReadyForCommit(request, runId, 90_000);
      await commitRun(request, runId);
      await waitForRunDetailCommitted(request, runId, 60_000);
    }

    const rows = await listArchitectureRuns(request);

    for (const id of runIds) {
      const row = rows.find((r) => r.runId === id);

      expect(row, `list should include committed run ${id}`).toBeDefined();
    }
  });
});
