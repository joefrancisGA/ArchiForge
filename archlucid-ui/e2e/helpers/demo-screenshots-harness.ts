/**
 * Preflight, failure scanning, filenames, and report output for {@link ../live-api-demo-screenshots.spec.ts}.
 */
import * as fs from "node:fs";
import * as path from "node:path";

import type { APIRequestContext, Page } from "@playwright/test";

import {
  getGraphForRunRaw,
  liveAcceptHeaders,
  liveApiBase,
  listRecentAudit,
  toRunGuidPathSegment,
} from "./live-api-client";

/** Substrings that indicate demo-unsafe or broken UI — case-sensitive match on visible text. */
export const DEMO_SCREENSHOT_FAILURE_SUBSTRINGS: readonly string[] = [
  "This architecture review could not be loaded",
  "No reviews in this workspace yet",
  "No graph on screen yet",
  "Loading graph viewer",
  "Loading workflow data",
  "Loading alerts",
  "Registered packs 0",
  "Effective layers 0",
  "Selected pack —",
  "No audit events",
  "No matching alerts",
  "Platform services: Healthy on an error page",
  "NEXT_PUBLIC_",
  "API-enforced",
  "client-only bundle",
  "not wired in this build",
  "coming soon",
];

/** Repo root (ArchLucid) from `archlucid-ui/e2e/**.ts`. */
export function demoScreenshotsRepoRoot(): string {
  return path.resolve(__dirname, "..", "..", "..");
}

export function demoScreenshotsOutputDir(timestamp: string): string {
  return path.join(demoScreenshotsRepoRoot(), "artifacts", "screenshots", timestamp);
}

/** Deterministic ordering — `/runs?projectId=default` is the supported reviews list URL (not `/runs/projectId/default`). */
export const DEMO_SCREENSHOT_ROUTES: readonly string[] = [
  "/",
  "/runs?projectId=default",
  "/runs/claims-intake-modernization-run",
  "/manifests/a1c2e3f4-a5b6-7890-abcd-ef1234567890",
  "/runs/claims-intake-modernization-run/findings/phi-minimization-risk",
  "/graph",
  "/ask",
  "/governance",
  "/audit",
  "/alerts",
  "/policy-packs",
  "/see-it",
  "/demo/preview",
];

export type PreflightCheck = { name: string; ok: boolean; detail?: string };

export type DemoPreflightResult = {
  ok: boolean;
  checks: PreflightCheck[];
  /** Resolved architecture run id (GUID-shaped) used for graph probe — baseline seed default {@link TRUSTED_BASELINE_RUN_ID_N}. */
  trustedBaselineRunIdN: string;
  graphRunSegment: string;
  alertsDemoReady: boolean;
  alertCount: number;
};

/**
 * Canonical Claims Intake marketing / UI slug (`/runs/claims-intake-modernization-run`, manifests UUID, …).
 * Live API accepts GUID-shaped architecture run ids only — use {@link TRUSTED_BASELINE_RUN_ID_N} for SQL-backed
 * preflight probes (Contoso Retail trusted baseline seed in docs/TRUSTED_BASELINE.md).
 */
export const CLAIMS_INTAKE_CANONICAL_RUN_ID = "claims-intake-modernization";

export const CLAIMS_INTAKE_MANIFEST_ID = "a1c2e3f4-a5b6-7890-abcd-ef1234567890";

export const CLAIMS_INTAKE_FINDING_ID = "phi-minimization-risk";

/** {@link ContosoRetailDemoIdentifiers.RunBaseline}: baseline demo row in `dbo.Runs` (single-catalog dev seed). */
export const TRUSTED_BASELINE_RUN_ID_N = "6e8c4a102b1f4c9a9d3e10b2a4f0c501";

/** Manifest version label on the Contoso baseline committed row (informational — preflight prefers authority manifest id from run detail). */
export const TRUSTED_BASELINE_MANIFEST_VERSION = "contoso-baseline-v1";

async function countAlertsApi(request: APIRequestContext): Promise<number> {
  const res = await request.get(`${liveApiBase}/v1/alerts`, {
    params: { take: "100" },
    headers: liveAcceptHeaders(),
  });

  if (!res.ok()) {
    const t = await res.text();

    throw new Error(`GET /v1/alerts failed ${res.status()}: ${t.slice(0, 400)}`);
  }

  const body: unknown = await res.json();

  if (Array.isArray(body)) {
    return body.length;
  }

  if (body !== null && typeof body === "object" && "items" in body) {
    const items = (body as { items?: unknown }).items;

    return Array.isArray(items) ? items.length : 0;
  }

  return 0;
}

type RunDetailJson = {
  run?: { runId?: string; goldenManifestId?: string | null };
};

export async function runDemoScreenshotPreflight(
  request: APIRequestContext,
  uiBaseUrl: string,
): Promise<DemoPreflightResult> {
  const checks: PreflightCheck[] = [];

  const push = (name: string, ok: boolean, detail?: string): void => {
    checks.push({ name, ok, detail });
  };

  let trustedBaselineRunIdN = TRUSTED_BASELINE_RUN_ID_N;
  let graphRunSegment = toRunGuidPathSegment(TRUSTED_BASELINE_RUN_ID_N);
  let alertsDemoReady = false;
  let alertCount = 0;

  try {
    const uiProbe = await request.get(uiBaseUrl.replace(/\/$/, "") + "/", { timeout: 60_000 });

    push("UI reachable", uiProbe.ok(), uiProbe.ok() ? undefined : `HTTP ${uiProbe.status()}`);
  } catch (e) {
    push("UI reachable", false, (e as Error).message);
  }

  try {
    const health = await request.get(`${liveApiBase}/health/ready`, { timeout: 60_000 });

    push("API /health/ready", health.ok(), health.ok() ? undefined : `HTTP ${health.status()}`);
  } catch (e) {
    push("API /health/ready", false, (e as Error).message);
  }

  try {
    const runRes = await request.get(
      `${liveApiBase}/v1/architecture/run/${encodeURIComponent(TRUSTED_BASELINE_RUN_ID_N)}`,
      {
        headers: liveAcceptHeaders(),
      },
    );

    if (!runRes.ok()) {
      push(
        "Trusted baseline architecture run (Contoso seed)",
        false,
        `HTTP ${runRes.status()}: ${(await runRes.text()).slice(0, 400)}`,
      );
      push(
        "Trusted baseline golden manifest summary (authority)",
        false,
        "skipped — run detail request failed",
      );
    } else {
      const detail = (await runRes.json()) as RunDetailJson;
      const rid = detail.run?.runId?.trim();
      const goldenManifestId = detail.run?.goldenManifestId?.trim();

      if (!rid) {
        push("Trusted baseline architecture run (Contoso seed)", false, "response missing run.runId");
      } else {
        trustedBaselineRunIdN = rid.replace(/-/g, "").trim().toLowerCase();
        graphRunSegment = toRunGuidPathSegment(trustedBaselineRunIdN);
        push("Trusted baseline architecture run (Contoso seed)", true);
      }

      if (!rid) {
        push(
          "Trusted baseline golden manifest summary (authority)",
          false,
          "skipped — run.runId missing",
        );
      } else if (!goldenManifestId || goldenManifestId.length === 0) {
        push(
          "Trusted baseline golden manifest summary (authority)",
          false,
          "response missing run.goldenManifestId (run may not be committed)",
        );
      } else {
        const manifestRes = await request.get(
          `${liveApiBase}/v1/authority/manifests/${encodeURIComponent(goldenManifestId)}/summary`,
          { headers: liveAcceptHeaders() },
        );

        push(
          "Trusted baseline golden manifest summary (authority)",
          manifestRes.ok(),
          manifestRes.ok() ? undefined : `HTTP ${manifestRes.status()}: ${(await manifestRes.text()).slice(0, 400)}`,
        );
      }
    }
  } catch (e) {
    push("Trusted baseline architecture run (Contoso seed)", false, (e as Error).message);
    push("Trusted baseline golden manifest summary (authority)", false, (e as Error).message);
  }

  try {
    const graphRes = await getGraphForRunRaw(request, graphRunSegment);

    if (!graphRes.ok()) {
      push("Graph API (nodes/edges)", false, `HTTP ${graphRes.status()}: ${(await graphRes.text()).slice(0, 400)}`);
    } else {
      const gj = (await graphRes.json()) as { nodes?: unknown[]; edges?: unknown[] };
      const nc = Array.isArray(gj.nodes) ? gj.nodes.length : 0;
      const ec = Array.isArray(gj.edges) ? gj.edges.length : 0;
      const ok = nc >= 1 && ec >= 1;

      push(
        "Graph API (nodes/edges)",
        ok,
        ok ? undefined : `nodes=${nc} edges=${ec} (need at least 1 each)`,
      );
    }
  } catch (e) {
    push("Graph API (nodes/edges)", false, (e as Error).message);
  }

  try {
    const auditItems = await listRecentAudit(request, 200);
    const ok = auditItems.length >= 1;

    push("Audit API (≥1 event)", ok, ok ? undefined : `count=${auditItems.length}`);
  } catch (e) {
    push("Audit API (≥1 event)", false, (e as Error).message);
  }

  try {
    alertCount = await countAlertsApi(request);
    alertsDemoReady = alertCount >= 1;
    push(
      "Alerts API",
      true,
      alertsDemoReady ? `count=${alertCount}` : `count=0 (alerts route will be skipped as not demo-ready)`,
    );
  } catch (e) {
    push("Alerts API", false, (e as Error).message);
  }

  const ok = checks.every((c) => c.ok);

  return {
    ok,
    checks,
    trustedBaselineRunIdN,
    graphRunSegment,
    alertsDemoReady,
    alertCount,
  };
}

export function routeToScreenshotFilename(routePath: string): string {
  const trimmed = routePath.trim();
  const pseudo = trimmed.startsWith("/") ? `http://local${trimmed}` : `http://local/${trimmed}`;
  const u = new URL(pseudo);
  const combined = `${u.pathname}${u.search}`.replace(/^\//, "");
  const slug = (combined.length > 0 ? combined : "index").replace(/[^a-zA-Z0-9]+/g, "-").replace(/^-|-$/g, "");

  return `${slug.length > 0 ? slug : "index"}.png`;
}

export function collectFailureSubstringsInText(visibleText: string): string[] {
  const hit: string[] = [];

  for (const s of DEMO_SCREENSHOT_FAILURE_SUBSTRINGS) {
    if (visibleText.includes(s)) {
      hit.push(s);
    }
  }

  return hit;
}

export async function waitForShellReady(page: Page, timeoutMs: number): Promise<void> {
  await page.locator('[data-app-ready="true"]').waitFor({ state: "attached", timeout: timeoutMs });
}

/** Best-effort: dynamic routes often show these loaders; wait before scanning/snapshotting. */
export async function settleDemoRoute(page: Page, routePath: string): Promise<void> {
  const p = routePath.split("?")[0] ?? routePath;

  if (p === "/graph" || p.startsWith("/graph")) {
    const graphLoading = page.getByText(/loading graph viewer/i).first();
    if ((await graphLoading.count()) > 0)
      await graphLoading.waitFor({ state: "hidden", timeout: 120_000 });

    return;
  }

  if (p === "/governance" || p.startsWith("/governance")) {
    const govLoading = page.getByText(/loading workflow data/i).first();
    if ((await govLoading.count()) > 0)
      await govLoading.waitFor({ state: "hidden", timeout: 120_000 });

    return;
  }

  if (p === "/alerts" || p.startsWith("/alerts")) {
    const alertsLoading = page.getByText(/loading alerts/i).first();
    if ((await alertsLoading.count()) > 0)
      await alertsLoading.waitFor({ state: "hidden", timeout: 120_000 });
  }
}

export type RouteCaptureResult = {
  route: string;
  path: string;
  screenshotFile: string | null;
  status: "pass" | "fail" | "skipped";
  issues: string[];
};

export type DemoScreenshotsReport = {
  generatedAtUtc: string;
  uiBaseUrl: string;
  apiBaseUrl: string;
  viewport: { width: number; height: number };
  preflight: { ok: boolean; checks: PreflightCheck[]; alertsDemoReady: boolean; alertCount: number };
  routes: RouteCaptureResult[];
  exitFailedRouteCount: number;
};

export function writeDemoScreenshotsReports(outDir: string, report: DemoScreenshotsReport): void {
  fs.mkdirSync(outDir, { recursive: true });

  fs.writeFileSync(path.join(outDir, "report.json"), JSON.stringify(report, null, 2), "utf8");

  const lines: string[] = [
    "# Demo screenshots report",
    "",
    "Canonical URLs: reviews list uses `/runs?projectId=default` (not `/runs/projectId/default`). Demo preview is `/demo/preview` (not `/demo-preview`).",
    "",
    `Generated: ${report.generatedAtUtc}`,
    `UI: ${report.uiBaseUrl}`,
    `API: ${report.apiBaseUrl}`,
    `Viewport: ${report.viewport.width}x${report.viewport.height}`,
    "",
    "## Preflight",
    "",
    `- **OK:** ${report.preflight.ok}`,
    `- **Alerts demo-ready:** ${report.preflight.alertsDemoReady} (API count ${report.preflight.alertCount})`,
    "",
    "| Check | OK | Detail |",
    "| --- | --- | --- |",
    ...report.preflight.checks.map((c) => `| ${c.name} | ${c.ok} | ${c.detail ?? ""} |`),
    "",
    "## Routes",
    "",
    "| Route | Screenshot | Status | Issues |",
    "| --- | --- | --- | --- |",
    ...report.routes.map((r) => {
      const issues = r.issues.length > 0 ? r.issues.join("; ") : "";

      return `| \`${r.route}\` | ${r.screenshotFile ?? "—"} | **${r.status}** | ${issues.replace(/\|/g, "\\|")} |`;
    }),
    "",
    `**Failed routes (exit):** ${report.exitFailedRouteCount}`,
    "",
  ];

  fs.writeFileSync(path.join(outDir, "report.md"), lines.join("\n"), "utf8");
}
