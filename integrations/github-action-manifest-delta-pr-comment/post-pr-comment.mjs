#!/usr/bin/env node
/**
 * Idempotently upsert a "sticky" PR comment that carries the structured ArchLucid
 * manifest-delta Markdown produced by the sibling `fetch-manifest-delta.mjs` script.
 *
 * The sticky behaviour is achieved by **prepending an HTML-comment marker**
 * (default: `<!-- archlucid:manifest-delta -->`). The action lists the PR's
 * comments via `gh api`, finds one whose body contains that marker, and either
 * PATCHes it in place or POSTs a new one. Re-runs of the workflow therefore
 * never spam — they just rewrite the same comment.
 *
 * Marker comments are HTML comments so they render as nothing in the PR view,
 * but they survive round-trips through the GitHub REST API unmodified, which
 * lets us identify "ours" without external state.
 *
 * The pure `upsertStickyComment(...)` function is exported so the smoke test
 * can exercise the create / update branching with a fake `gh` client; the
 * production client (`ghClient`) wraps `gh api` via `child_process.spawn`.
 */

import { spawn } from "node:child_process";
import { readFile } from "node:fs/promises";
import { fileURLToPath } from "node:url";

/**
 * Default sticky marker. Anything matching this substring on a comment body is
 * treated as the previous ArchLucid delta comment and overwritten in place.
 */
export const DEFAULT_MARKER = "<!-- archlucid:manifest-delta -->";

/**
 * Compose the comment body: marker on its own line, then the Markdown payload
 * verbatim. Keeping the marker on its own line keeps it out of the rendered
 * Markdown and out of code fences, even if the payload starts with one.
 */
export function buildBody(marker, payload) {
  if (!marker) throw new Error("marker is required");
  if (payload === undefined || payload === null) throw new Error("payload is required");

  return `${marker}\n${payload}`;
}

/**
 * Find the first comment whose body contains the marker, or null. We use
 * `includes(...)` rather than `startsWith(...)` so authors can prepend or
 * append context lines manually without breaking sticky detection.
 */
export function findStickyComment(comments, marker) {
  if (!Array.isArray(comments)) return null;
  if (!marker) throw new Error("marker is required");

  return comments.find(c => typeof c?.body === "string" && c.body.includes(marker)) ?? null;
}

/**
 * Pure, transport-agnostic upsert. `client` must implement
 * `listIssueComments`, `createIssueComment`, and `updateIssueComment`.
 * Returns `{ action, commentId }` so the caller can log a deterministic line.
 */
export async function upsertStickyComment({ owner, repo, prNumber, body, marker, client }) {
  if (!owner) throw new Error("owner is required");
  if (!repo) throw new Error("repo is required");
  if (prNumber === undefined || prNumber === null || prNumber === "") throw new Error("prNumber is required");
  if (!body) throw new Error("body is required");
  if (!marker) throw new Error("marker is required");
  if (!client) throw new Error("client is required");

  const comments = await client.listIssueComments({ owner, repo, prNumber });
  const existing = findStickyComment(comments, marker);

  if (existing) {
    await client.updateIssueComment({ owner, repo, commentId: existing.id, body });

    return { action: "updated", commentId: existing.id };
  }

  const created = await client.createIssueComment({ owner, repo, prNumber, body });

  return { action: "created", commentId: created?.id ?? null };
}

/**
 * Spawns `gh` and returns its stdout, throwing on non-zero exit. Optional
 * `stdinJson` is JSON-encoded and piped to stdin so we can use `gh api --input -`
 * to send a request body without quoting headaches.
 */
function ghExec(args, stdinJson) {
  return new Promise((resolve, reject) => {
    const child = spawn("gh", args, { stdio: ["pipe", "pipe", "pipe"] });
    let stdout = "";
    let stderr = "";

    child.stdout.on("data", d => { stdout += d.toString(); });
    child.stderr.on("data", d => { stderr += d.toString(); });
    child.on("error", reject);
    child.on("close", code => {
      if (code !== 0) {
        reject(new Error(`gh exited ${code}: ${(stderr || stdout).trim()}`));

        return;
      }

      resolve(stdout);
    });

    if (stdinJson !== undefined) {
      child.stdin.write(JSON.stringify(stdinJson));
    }
    child.stdin.end();
  });
}

/**
 * Production GitHub client backed by `gh api`. `gh` reads its credentials from
 * `GH_TOKEN` / `GITHUB_TOKEN` in the environment, which the composite action
 * wires from the `github-token` input.
 *
 * `--paginate` collapses the paginated `issues/{number}/comments` array into a
 * single JSON array, so PRs with > 30 comments still resolve correctly.
 */
export const ghClient = {
  async listIssueComments({ owner, repo, prNumber }) {
    const out = await ghExec([
      "api",
      "--paginate",
      "-H", "Accept: application/vnd.github+json",
      `repos/${owner}/${repo}/issues/${prNumber}/comments`,
    ]);

    const trimmed = out.trim();
    if (!trimmed) return [];

    const parsed = JSON.parse(trimmed);
    return Array.isArray(parsed) ? parsed : [];
  },

  async createIssueComment({ owner, repo, prNumber, body }) {
    const out = await ghExec([
      "api",
      "--method", "POST",
      "-H", "Accept: application/vnd.github+json",
      `repos/${owner}/${repo}/issues/${prNumber}/comments`,
      "--input", "-",
    ], { body });

    return JSON.parse(out);
  },

  async updateIssueComment({ owner, repo, commentId, body }) {
    const out = await ghExec([
      "api",
      "--method", "PATCH",
      "-H", "Accept: application/vnd.github+json",
      `repos/${owner}/${repo}/issues/comments/${commentId}`,
      "--input", "-",
    ], { body });

    return JSON.parse(out);
  },
};

/**
 * CLI entrypoint. We deliberately gate the side-effecting code behind an
 * `import.meta.url === argv[1]` check so the smoke test can `import` this
 * module without the production `gh` shell-out ever running.
 */
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
  const owner = process.env.GITHUB_REPO_OWNER || "";
  const repo = process.env.GITHUB_REPO_NAME || "";
  const prNumber = process.env.GITHUB_PR_NUMBER || "";
  const bodyPath = process.env.ARCHLUCID_DELTA_BODY_PATH || "";
  const marker = process.env.ARCHLUCID_STICKY_MARKER || DEFAULT_MARKER;

  if (!owner || !repo || !prNumber || !bodyPath) {
    console.error(
      "Missing required env: GITHUB_REPO_OWNER, GITHUB_REPO_NAME, GITHUB_PR_NUMBER, ARCHLUCID_DELTA_BODY_PATH",
    );
    process.exit(1);
  }

  const payload = await readFile(bodyPath, "utf8");
  const body = buildBody(marker, payload);

  const result = await upsertStickyComment({
    owner,
    repo,
    prNumber,
    body,
    marker,
    client: ghClient,
  });

  console.log(`archlucid:pr-comment ${result.action} comment ${result.commentId ?? "<unknown>"}`);
}
