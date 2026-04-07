import http from "node:http";
import { randomUUID } from "node:crypto";

import {
  archiveMockAssignment,
  assignMockPack,
  createMockPack,
  getMockEffectiveContent,
  getMockEffectivePacks,
  listMockPacks,
  listMockVersions,
  publishMockVersion,
  resetPolicyPacksMockState,
} from "./policy-packs-mock-state";
import {
  FIXTURE_MANIFEST_EMPTY_ARTIFACTS_ID,
  FIXTURE_MANIFEST_ID,
  FIXTURE_RUN_ID,
  fixtureArtifactDescriptorsNonEmpty,
  fixtureManifestSummary,
  fixtureManifestSummaryEmptyArtifacts,
  fixtureRunDetail,
} from "./fixtures/index";

function sendJson(res: http.ServerResponse, status: number, body: unknown): void {
  const payload = JSON.stringify(body);
  res.writeHead(status, {
    "Content-Type": "application/json; charset=utf-8",
    "Content-Length": Buffer.byteLength(payload, "utf8"),
  });
  res.end(payload);
}

function readJsonBody(req: http.IncomingMessage): Promise<unknown> {
  return new Promise((resolve, reject) => {
    const chunks: Buffer[] = [];
    req.on("data", (c) => chunks.push(c as Buffer));
    req.on("end", () => {
      try {
        const raw = Buffer.concat(chunks).toString("utf8");
        if (!raw.trim()) {
          resolve({});
          return;
        }
        resolve(JSON.parse(raw) as unknown);
      } catch (e) {
        reject(e);
      }
    });
    req.on("error", reject);
  });
}

/**
 * Minimal HTTP stub for ArchiForge API routes used by RSC run/manifest pages and policy-packs E2E.
 */
export function startMockArchiforgeApiServer(port: number): Promise<{ stop: () => Promise<void> }> {
  const host = "127.0.0.1";
  resetPolicyPacksMockState();

  const server = http.createServer((req, res) => {
    void (async () => {
      const u = new URL(req.url ?? "/", `http://${host}`);

      if (req.method === "GET" && u.pathname === "/health") {
        res.writeHead(200, { "Content-Type": "text/plain; charset=utf-8" });
        res.end("ok");
        return;
      }

      const pathname = decodeURIComponent(u.pathname);

      if (req.method === "GET" && pathname === "/v1/policy-packs") {
        sendJson(res, 200, listMockPacks());
        return;
      }

      if (req.method === "GET" && pathname === "/v1/policy-packs/effective") {
        sendJson(res, 200, getMockEffectivePacks());
        return;
      }

      if (req.method === "GET" && pathname === "/v1/policy-packs/effective-content") {
        sendJson(res, 200, getMockEffectiveContent());
        return;
      }

      const versionsMatch = /^\/v1\/policy-packs\/([^/]+)\/versions$/.exec(pathname);

      if (req.method === "GET" && versionsMatch) {
        sendJson(res, 200, listMockVersions(versionsMatch[1]));
        return;
      }

      if (req.method === "POST" && pathname === "/v1/policy-packs") {
        try {
          const body = (await readJsonBody(req)) as {
            name?: string;
            description?: string;
            packType?: string;
            initialContentJson?: string;
          };
          const pack = createMockPack(body);
          sendJson(res, 200, pack);
        } catch {
          sendJson(res, 400, { detail: "Invalid JSON" });
        }
        return;
      }

      const publishMatch = /^\/v1\/policy-packs\/([^/]+)\/publish$/.exec(pathname);

      if (req.method === "POST" && publishMatch) {
        try {
          const body = (await readJsonBody(req)) as { version?: string; contentJson?: string };
          publishMockVersion(publishMatch[1], (body.version ?? "1.0.0").trim(), body.contentJson ?? "{}");
          sendJson(res, 200, {
            policyPackVersionId: randomUUID(),
            policyPackId: publishMatch[1],
            version: (body.version ?? "1.0.0").trim(),
            contentJson: body.contentJson ?? "{}",
            isPublished: true,
            createdUtc: new Date().toISOString(),
          });
        } catch {
          sendJson(res, 400, { detail: "Invalid JSON" });
        }
        return;
      }

      const assignMatch = /^\/v1\/policy-packs\/([^/]+)\/assign$/.exec(pathname);

      if (req.method === "POST" && assignMatch) {
        try {
          const body = (await readJsonBody(req)) as {
            version?: string;
            scopeLevel?: string;
            isPinned?: boolean;
          };
          const row = assignMockPack(
            assignMatch[1],
            (body.version ?? "1.0.0").trim(),
            body.scopeLevel ?? "Project",
            Boolean(body.isPinned),
          );
          if (!row) {
            sendJson(res, 404, { type: "policy-pack-version-not-found" });
            return;
          }
          sendJson(res, 200, {
            assignmentId: row.assignmentId,
            tenantId: "00000000-0000-0000-0000-000000000001",
            workspaceId: "00000000-0000-0000-0000-000000000002",
            projectId: "00000000-0000-0000-0000-000000000003",
            policyPackId: row.policyPackId,
            policyPackVersion: row.version,
            isEnabled: true,
            scopeLevel: row.scopeLevel,
            isPinned: row.isPinned,
            assignedUtc: new Date().toISOString(),
          });
        } catch {
          sendJson(res, 400, { detail: "Invalid JSON" });
        }
        return;
      }

      const archiveMatch = /^\/v1\/policy-packs\/assignments\/([^/]+)\/archive$/.exec(pathname);

      if (req.method === "POST" && archiveMatch) {
        const ok = archiveMockAssignment(archiveMatch[1]);
        if (!ok) {
          sendJson(res, 404, { detail: "not found" });
          return;
        }
        res.writeHead(204);
        res.end();
        return;
      }

      if (req.method !== "GET") {
        res.writeHead(405);
        res.end();
        return;
      }

      // RSC server-side fetch uses `getServerApiBaseUrl()` → these paths (see `src/lib/api.ts`).
      const runMatchV1 = /^\/v1\/authority\/runs\/([^/]+)$/.exec(pathname);
      const runMatchLegacy = /^\/api\/authority\/runs\/([^/]+)$/.exec(pathname);
      const runMatch = runMatchV1 ?? runMatchLegacy;

      if (runMatch) {
        const runId = runMatch[1];

        if (runId === FIXTURE_RUN_ID) {
          sendJson(res, 200, fixtureRunDetail());
        } else {
          sendJson(res, 404, { detail: "Run not found." });
        }

        return;
      }

      const summaryMatchV1 = /^\/v1\/authority\/manifests\/([^/]+)\/summary$/.exec(pathname);
      const summaryMatchLegacy = /^\/api\/authority\/manifests\/([^/]+)\/summary$/.exec(pathname);
      const summaryMatch = summaryMatchV1 ?? summaryMatchLegacy;

      if (summaryMatch) {
        const manifestId = summaryMatch[1];

        if (manifestId === FIXTURE_MANIFEST_ID) {
          sendJson(res, 200, fixtureManifestSummary());
        } else if (manifestId === FIXTURE_MANIFEST_EMPTY_ARTIFACTS_ID) {
          sendJson(res, 200, fixtureManifestSummaryEmptyArtifacts());
        } else {
          sendJson(res, 404, { detail: "Manifest not found." });
        }

        return;
      }

      const artifactsMatchV1 = /^\/v1\/artifacts\/manifests\/([^/]+)$/.exec(pathname);
      const artifactsMatchLegacy = /^\/api\/artifacts\/manifests\/([^/]+)$/.exec(pathname);
      const artifactsMatch = artifactsMatchV1 ?? artifactsMatchLegacy;

      if (artifactsMatch) {
        const manifestId = artifactsMatch[1];

        if (manifestId === FIXTURE_MANIFEST_ID) {
          sendJson(res, 200, fixtureArtifactDescriptorsNonEmpty());
        } else if (manifestId === FIXTURE_MANIFEST_EMPTY_ARTIFACTS_ID) {
          sendJson(res, 200, []);
        } else {
          sendJson(res, 200, []);
        }

        return;
      }

      sendJson(res, 404, { detail: "E2E mock: no handler for this path." });
    })().catch(() => {
      sendJson(res, 500, { detail: "E2E mock internal error" });
    });
  });

  return new Promise((resolve, reject) => {
    server.once("error", reject);
    server.listen(port, host, () => {
      resolve({
        stop: () =>
          new Promise((res, rej) => {
            server.close((err) => {
              if (err) {
                rej(err);
              } else {
                res();
              }
            });
          }),
      });
    });
  });
}
