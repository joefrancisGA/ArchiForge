import type { Page, Route } from "@playwright/test";

import type { ComparisonExplanation } from "@/types/explanation";
import type { GoldenManifestComparison } from "@/types/comparison";
import type { ArtifactDescriptor, ManifestSummary, RunComparison, RunDetail } from "@/types/authority";

import {
  fixtureArtifactDescriptorsNonEmpty,
  fixtureComparisonExplanation,
  fixtureGoldenManifestComparison,
  fixtureLegacyRunComparison,
  fixtureManifestSummary,
  fixtureRunDetail,
  FIXTURE_LEFT_RUN_ID,
  FIXTURE_MANIFEST_ID,
  FIXTURE_RIGHT_RUN_ID,
  FIXTURE_RUN_ID,
} from "../fixtures";
import {
  backendApiPath,
  matchesArtifactBundleGet,
  matchesArtifactListGet,
  matchesCompareExplainGet,
  matchesLegacyCompareRunsGet,
  matchesManifestSummaryGet,
  matchesRunDetailGet,
  matchesStructuredCompareGet,
} from "./route-match";

/** PKZIP empty archive (22-byte EOCD) — valid for download smoke without a real compressor. */
export const FIXTURE_EMPTY_ZIP_BYTES = Buffer.from([
  0x50, 0x4b, 0x05, 0x06, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
  0x00, 0x00, 0x00, 0x00,
]);

export type RunDetailRouteSpec = { runId: string; body: RunDetail };

export type ManifestSummaryRouteSpec = { manifestId: string; body: ManifestSummary };

export type ArtifactListRouteSpec = { manifestId: string; body: ArtifactDescriptor[] };

export type LegacyCompareRouteSpec = {
  leftRunId: string;
  rightRunId: string;
  body: RunComparison;
};

export type StructuredCompareRouteSpec = {
  baseRunId: string;
  targetRunId: string;
  body: GoldenManifestComparison;
};

export type CompareExplanationRouteSpec = {
  baseRunId: string;
  targetRunId: string;
  body: ComparisonExplanation;
};

export type ArtifactBundleRouteSpec = {
  manifestId: string;
  /** Defaults to {@link FIXTURE_EMPTY_ZIP_BYTES}. */
  body?: Buffer;
  /** When true, HEAD returns 200 with Content-Length and no body. */
  headOk?: boolean;
};

export type OperatorJourneyRouteConfig = {
  runDetail?: RunDetailRouteSpec | null;
  manifestSummary?: ManifestSummaryRouteSpec | null;
  artifactList?: ArtifactListRouteSpec | null;
  legacyCompare?: LegacyCompareRouteSpec | null;
  structuredCompare?: StructuredCompareRouteSpec | null;
  compareExplanation?: CompareExplanationRouteSpec | null;
  artifactBundle?: ArtifactBundleRouteSpec | null;
};

async function fulfillJson(route: Route, status: number, body: unknown): Promise<void> {
  await route.fulfill({
    status,
    contentType: "application/json",
    body: JSON.stringify(body),
  });
}

export async function registerOperatorJourneyApiRoutes(
  page: Page,
  config: OperatorJourneyRouteConfig,
): Promise<void> {
  await page.route("**/*", async (route) => {
    const req = route.request();
    const url = new URL(req.url());
    const method = req.method();

    if (backendApiPath(url) === null) {
      await route.continue();
      return;
    }

    const tryFulfill = async (): Promise<boolean> => {
      if (config.runDetail && method === "GET" && matchesRunDetailGet(url, config.runDetail.runId)) {
        await fulfillJson(route, 200, config.runDetail.body);
        return true;
      }

      if (
        config.manifestSummary &&
        method === "GET" &&
        matchesManifestSummaryGet(url, config.manifestSummary.manifestId)
      ) {
        await fulfillJson(route, 200, config.manifestSummary.body);
        return true;
      }

      if (config.artifactList && method === "GET" && matchesArtifactListGet(url, config.artifactList.manifestId)) {
        await fulfillJson(route, 200, config.artifactList.body);
        return true;
      }

      if (
        config.legacyCompare &&
        method === "GET" &&
        matchesLegacyCompareRunsGet(url, config.legacyCompare.leftRunId, config.legacyCompare.rightRunId)
      ) {
        await fulfillJson(route, 200, config.legacyCompare.body);
        return true;
      }

      if (
        config.structuredCompare &&
        method === "GET" &&
        matchesStructuredCompareGet(url, config.structuredCompare.baseRunId, config.structuredCompare.targetRunId)
      ) {
        await fulfillJson(route, 200, config.structuredCompare.body);
        return true;
      }

      if (
        config.compareExplanation &&
        method === "GET" &&
        matchesCompareExplainGet(url, config.compareExplanation.baseRunId, config.compareExplanation.targetRunId)
      ) {
        await fulfillJson(route, 200, config.compareExplanation.body);
        return true;
      }

      if (config.artifactBundle && matchesArtifactBundleGet(url, config.artifactBundle.manifestId)) {
        const zip = config.artifactBundle.body ?? FIXTURE_EMPTY_ZIP_BYTES;

        if (method === "HEAD" && config.artifactBundle.headOk) {
          await route.fulfill({
            status: 200,
            headers: { "Content-Type": "application/zip", "Content-Length": String(zip.length) },
          });
          return true;
        }

        if (method === "GET") {
          await route.fulfill({
            status: 200,
            contentType: "application/zip",
            body: zip,
          });
          return true;
        }
      }

      return false;
    };

    const handled = await tryFulfill();
    if (!handled) {
      await route.continue();
    }
  });
}

/** Legacy + structured compare for the default E2E left/right run IDs (no AI explain route). */
function defaultFixturePairLegacyStructuredConfig(): Pick<
  OperatorJourneyRouteConfig,
  "legacyCompare" | "structuredCompare"
> {
  return {
    legacyCompare: {
      leftRunId: FIXTURE_LEFT_RUN_ID,
      rightRunId: FIXTURE_RIGHT_RUN_ID,
      body: fixtureLegacyRunComparison(),
    },
    structuredCompare: {
      baseRunId: FIXTURE_LEFT_RUN_ID,
      targetRunId: FIXTURE_RIGHT_RUN_ID,
      body: fixtureGoldenManifestComparison(),
    },
  };
}

/** Client compare page: mock legacy + structured GETs for the standard fixture pair. */
export async function registerDefaultPairLegacyStructuredCompare(page: Page): Promise<void> {
  await registerOperatorJourneyApiRoutes(page, defaultFixturePairLegacyStructuredConfig());
}

/** Default fixture pair: run + manifest + artifacts + bundle (for future run/manifest journey tests from the browser). */
export async function registerDefaultRunManifestArtifactRoutes(page: Page): Promise<void> {
  await registerOperatorJourneyApiRoutes(page, {
    runDetail: { runId: FIXTURE_RUN_ID, body: fixtureRunDetail() },
    manifestSummary: { manifestId: FIXTURE_MANIFEST_ID, body: fixtureManifestSummary() },
    artifactList: { manifestId: FIXTURE_MANIFEST_ID, body: fixtureArtifactDescriptorsNonEmpty() },
    artifactBundle: { manifestId: FIXTURE_MANIFEST_ID, body: FIXTURE_EMPTY_ZIP_BYTES, headOk: true },
  });
}

/** Compare page (client): legacy + structured + optional AI explanation GETs. */
export async function registerCompareAndExplainRoutes(page: Page): Promise<void> {
  await registerOperatorJourneyApiRoutes(page, {
    ...defaultFixturePairLegacyStructuredConfig(),
    compareExplanation: {
      baseRunId: FIXTURE_LEFT_RUN_ID,
      targetRunId: FIXTURE_RIGHT_RUN_ID,
      body: fixtureComparisonExplanation(),
    },
  });
}

/**
 * Stubs generic `/api/proxy` GETs used by full-route screenshot crawls when no backend is reachable.
 * Register **after** {@link registerOperatorJourneyApiRoutes} so this handler runs first (Playwright matches last registered routes first).
 */
export async function registerScreenshotSuiteProxyRoutes(page: Page): Promise<void> {
  await page.route("**/*", async (route) => {
    const req = route.request();

    if (req.method() !== "GET") {
      await route.fallback();

      return;
    }

    const url = new URL(req.url());
    const apiPath = backendApiPath(url);

    if (apiPath === null) {
      await route.fallback();

      return;
    }

    if (apiPath === "/health/ready") {
      await fulfillJson(route, 200, {
        status: "Healthy",
        entries: [{ name: "database", status: "Healthy", durationMs: 12 }],
      });

      return;
    }

    if (apiPath === "/version") {
      await fulfillJson(route, 200, {
        informationalVersion: "e2e-screenshots",
        commitSha: "e2e000000000000000000000000000000000000",
      });

      return;
    }

    if (apiPath === "/health") {
      await fulfillJson(route, 200, {
        status: "Healthy",
        entries: [
          {
            name: "circuit_breakers",
            status: "Healthy",
            data: {
              gates: [{ name: "completion", state: "Closed", breakDurationSeconds: 0 }],
            },
          },
        ],
      });

      return;
    }

    if (apiPath === "/v1/diagnostics/operator-task-success-rates") {
      await fulfillJson(route, 200, {
        windowNote: "E2E screenshot fixture.",
        firstRunCommittedTotal: 1,
        firstSessionCompletedTotal: 2,
        firstRunCommittedPerSessionRatio: 0.5,
      });

      return;
    }

    await route.fallback();
  });
}
