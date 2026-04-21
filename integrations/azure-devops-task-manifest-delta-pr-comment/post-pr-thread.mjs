#!/usr/bin/env node
/**
 * Sticky upsert for Azure DevOps PR threads + informational PR status (REST 7.1).
 * Mirrors `integrations/github-action-manifest-delta-pr-comment/post-pr-comment.mjs` semantics.
 */
import { readFile } from "node:fs/promises";
import { fileURLToPath } from "node:url";

import {
  buildStatusCreateJson,
  buildThreadCreateJson,
} from "./post-pr-thread-wire.mjs";

const API_VER = "7.1";

export const DEFAULT_MARKER = "<!-- archlucid:manifest-delta -->";

export function buildBody(marker, payload) {
  if (!marker) throw new Error("marker is required");
  if (payload === undefined || payload === null) throw new Error("payload is required");

  return `${marker}\n${payload}`;
}

export function resolveAdoAuthHeaders() {
  const pat = (process.env.ARCHLUCID_AZURE_DEVOPS_PAT ?? "").trim();
  const sys = (process.env.SYSTEM_ACCESSTOKEN ?? "").trim();

  if (!pat && sys) {
    console.error("archlucid:ado-auth mode=Bearer");

    return { authorization: `Bearer ${sys}` };
  }

  if (pat) {
    console.error("archlucid:ado-auth mode=Basic");

    const b64 = Buffer.from(`:${pat}`, "utf8").toString("base64");

    return { authorization: `Basic ${b64}` };
  }

  throw new Error(
    "Missing Azure DevOps credentials: set SYSTEM_ACCESSTOKEN (use checkout persistCredentials: true) or ARCHLUCID_AZURE_DEVOPS_PAT.",
  );
}

export function buildPullRequestApiBase(organization, project, repositoryId, pullRequestId) {
  const org = encodeURIComponent(organization.trim());
  const proj = encodeURIComponent(project.trim());

  return `https://dev.azure.com/${org}/${proj}/_apis/git/repositories/${repositoryId}/pullrequests/${pullRequestId}`;
}

export async function listAllThreads(basePath, fetchImpl, authHeaders, pageSize = 100) {
  const threads = [];
  let skip = 0;

  for (;;) {
    const url = new URL(`${basePath}/threads`);
    url.searchParams.set("api-version", API_VER);
    url.searchParams.set("$top", String(pageSize));
    url.searchParams.set("$skip", String(skip));

    const res = await fetchImpl(url.toString(), {
      method: "GET",
      headers: { Authorization: authHeaders.authorization },
    });

    if (!res.ok) {
      const t = await res.text();

      throw new Error(`list threads failed: HTTP ${res.status} ${t.slice(0, 2000)}`);
    }

    const json = await res.json();
    const batch = Array.isArray(json?.value) ? json.value : [];

    threads.push(...batch);

    if (batch.length < pageSize) break;

    skip += pageSize;
  }

  return threads;
}

export function findStickyMatches(threads, marker) {
  if (!Array.isArray(threads)) return [];

  const matches = [];

  for (const thread of threads) {
    const comments = Array.isArray(thread?.comments) ? thread.comments : [];

    for (const c of comments) {
      const content = typeof c?.content === "string" ? c.content : "";

      if (content.includes(marker)) {
        matches.push({
          threadId: thread.id,
          commentId: c.id,
          lastUpdatedDate: thread.lastUpdatedDate ?? thread.publishedDate ?? "",
        });

        break;
      }
    }
  }

  return matches;
}

export async function patchThreadComment(basePath, threadId, commentId, newContent, fetchImpl, authHeaders) {
  const url = `${basePath}/threads/${threadId}/comments/${commentId}?api-version=${API_VER}`;
  const body = JSON.stringify({ content: newContent });

  const res = await fetchImpl(url, {
    method: "PATCH",
    headers: {
      "Content-Type": "application/json",
      Authorization: authHeaders.authorization,
    },
    body,
  });

  if (!res.ok) {
    const t = await res.text();

    throw new Error(`patch comment failed: HTTP ${res.status} ${t.slice(0, 2000)}`);
  }
}

export async function postThread(basePath, markdownWithMarker, fetchImpl, authHeaders) {
  const url = `${basePath}/threads?api-version=${API_VER}`;
  const body = buildThreadCreateJson(markdownWithMarker);

  const res = await fetchImpl(url, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
      Authorization: authHeaders.authorization,
    },
    body,
  });

  if (!res.ok) {
    const t = await res.text();

    throw new Error(`post thread failed: HTTP ${res.status} ${t.slice(0, 2000)}`);
  }
}

export async function postPullRequestStatus(basePath, description, targetUrl, fetchImpl, authHeaders) {
  const url = `${basePath}/statuses?api-version=${API_VER}`;
  const body = buildStatusCreateJson(description, targetUrl);

  const res = await fetchImpl(url, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
      Authorization: authHeaders.authorization,
    },
    body,
  });

  if (!res.ok) {
    const t = await res.text();

    throw new Error(`post status failed: HTTP ${res.status} ${t.slice(0, 2000)}`);
  }
}

function pickMostRecentMatch(matches) {
  if (matches.length === 0) throw new Error("internal: pickMostRecentMatch requires at least one match");

  if (matches.length === 1) return matches[0];

  const sorted = [...matches].sort((a, b) => String(b.lastUpdatedDate).localeCompare(String(a.lastUpdatedDate)));

  console.warn(
    `archlucid: multiple sticky threads matched; updating threadId=${sorted[0].threadId} (most recent lastUpdatedDate). Also found: ${matches
      .map(m => m.threadId)
      .join(", ")}`,
  );

  return sorted[0];
}

export async function upsertStickyPrThreadAndStatus(options) {
  const {
    basePath,
    marker,
    fullBody,
    fetchImpl,
    authHeaders,
  } = options;

  const threads = await listAllThreads(basePath, fetchImpl, authHeaders);
  const matches = findStickyMatches(threads, marker);

  if (matches.length === 0) {
    await postThread(basePath, fullBody, fetchImpl, authHeaders);

    return { action: "created" };
  }

  const pick = pickMostRecentMatch(matches);

  await patchThreadComment(basePath, pick.threadId, pick.commentId, fullBody, fetchImpl, authHeaders);

  return { action: "updated", threadId: pick.threadId };
}

const isCli = (() => {
  if (!process.argv[1]) return false;

  try {
    return fileURLToPath(import.meta.url) === process.argv[1];
  }
  catch {
    return false;
  }
})();

if (isCli) {
  const org = process.env.ARCHLUCID_ADO_ORGANIZATION ?? "";
  const project = process.env.ARCHLUCID_ADO_PROJECT ?? "";
  const repoId = process.env.ARCHLUCID_ADO_REPOSITORY_ID ?? "";
  const prId = process.env.ARCHLUCID_ADO_PULL_REQUEST_ID ?? "";
  const bodyPath = process.env.ARCHLUCID_DELTA_BODY_PATH ?? "";
  const marker = process.env.ARCHLUCID_STICKY_MARKER || DEFAULT_MARKER;
  const compareTemplate = process.env.ARCHLUCID_COMPARE_URL_TEMPLATE || "";
  const baseRunId = process.env.ARCHLUCID_BASE_RUN_ID || "";
  const targetRunId = process.env.ARCHLUCID_TARGET_RUN_ID || "";

  if (!org || !project || !repoId || !prId || !bodyPath) {
    console.error(
      "Missing required env: ARCHLUCID_ADO_ORGANIZATION, ARCHLUCID_ADO_PROJECT, ARCHLUCID_ADO_REPOSITORY_ID, ARCHLUCID_ADO_PULL_REQUEST_ID, ARCHLUCID_DELTA_BODY_PATH",
    );
    process.exit(1);
  }

  const payload = await readFile(bodyPath, "utf8");
  const body = buildBody(marker, payload);
  const basePath = buildPullRequestApiBase(org, project, repoId, prId);

  let targetUrl = "";

  if (compareTemplate.includes("{baseRunId}") && compareTemplate.includes("{targetRunId}")) {
    targetUrl = compareTemplate.replaceAll("{baseRunId}", baseRunId).replaceAll("{targetRunId}", targetRunId);
  }

  const statusDesc = body.length > 512 ? body.slice(0, 512) : body;

  try {
    const auth = resolveAdoAuthHeaders();

    const result = await upsertStickyPrThreadAndStatus({
      basePath,
      marker,
      fullBody: body,
      fetchImpl: globalThis.fetch,
      authHeaders: auth,
    });

    await postPullRequestStatus(basePath, statusDesc, targetUrl || undefined, globalThis.fetch, auth);

    console.log(`archlucid:ado-pr-thread ${result.action}`);
  }
  catch (e) {
    console.error(e instanceof Error ? e.message : e);
    process.exit(1);
  }
}
