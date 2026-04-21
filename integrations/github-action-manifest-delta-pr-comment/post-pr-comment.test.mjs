/**
 * Smoke tests for the sticky PR-comment upsert. We exercise `upsertStickyComment`
 * with a fake `gh` client so the create / update branching is covered without
 * actually invoking the `gh` binary or hitting the GitHub API.
 *
 * Run locally with:
 *   node --test integrations/github-action-manifest-delta-pr-comment/post-pr-comment.test.mjs
 */
import { test } from "node:test";
import assert from "node:assert/strict";

import {
  DEFAULT_MARKER,
  buildBody,
  findStickyComment,
  upsertStickyComment,
} from "./post-pr-comment.mjs";

test("buildBody prepends the marker on its own line", () => {
  assert.equal(buildBody("<!-- m -->", "hello"), "<!-- m -->\nhello");
});

test("buildBody throws when the marker is empty", () => {
  assert.throws(() => buildBody("", "hello"), /marker/);
});

test("findStickyComment returns null when no comment matches", () => {
  const found = findStickyComment(
    [{ id: 1, body: "noise" }, { id: 2, body: "more noise" }],
    DEFAULT_MARKER,
  );
  assert.equal(found, null);
});

test("findStickyComment ignores entries with non-string bodies", () => {
  const found = findStickyComment(
    [{ id: 1, body: null }, { id: 2 }, { id: 3, body: 42 }],
    DEFAULT_MARKER,
  );
  assert.equal(found, null);
});

test("findStickyComment returns the first matching comment", () => {
  const target = { id: 22, body: `${DEFAULT_MARKER}\nold body` };
  const found = findStickyComment(
    [{ id: 11, body: "noise" }, target, { id: 33, body: `${DEFAULT_MARKER}\ndupe` }],
    DEFAULT_MARKER,
  );
  assert.equal(found, target);
});

test("upsertStickyComment creates a new comment when none exists", async () => {
  const calls = [];

  /** Fake `gh` client. The `update` method MUST NOT be called on the create path. */
  const client = {
    listIssueComments: async ({ prNumber }) => {
      calls.push(["list", prNumber]);
      return [{ id: 1, body: "unrelated review nit" }];
    },
    createIssueComment: async (args) => {
      calls.push(["create", args]);
      return { id: 4242 };
    },
    updateIssueComment: async () => { throw new Error("must not update on create path"); },
  };

  const result = await upsertStickyComment({
    owner: "acme",
    repo: "platform",
    prNumber: 7,
    body: `${DEFAULT_MARKER}\n## hello`,
    marker: DEFAULT_MARKER,
    client,
  });

  assert.equal(result.action, "created");
  assert.equal(result.commentId, 4242);
  assert.equal(calls.length, 2);
  assert.deepEqual(calls[0], ["list", 7]);
  assert.equal(calls[1][0], "create");
  assert.equal(calls[1][1].owner, "acme");
  assert.equal(calls[1][1].repo, "platform");
  assert.equal(calls[1][1].prNumber, 7);
  assert.match(calls[1][1].body, /## hello/);
});

test("upsertStickyComment updates the existing sticky comment when one is present", async () => {
  const calls = [];

  const client = {
    listIssueComments: async () => [
      { id: 11, body: "noise" },
      { id: 22, body: `${DEFAULT_MARKER}\nold delta body` },
      { id: 33, body: "more noise" },
    ],
    createIssueComment: async () => { throw new Error("must not create on update path"); },
    updateIssueComment: async (args) => {
      calls.push(["update", args]);
      return { id: args.commentId };
    },
  };

  const result = await upsertStickyComment({
    owner: "acme",
    repo: "platform",
    prNumber: 7,
    body: `${DEFAULT_MARKER}\nnew delta body`,
    marker: DEFAULT_MARKER,
    client,
  });

  assert.equal(result.action, "updated");
  assert.equal(result.commentId, 22);
  assert.equal(calls.length, 1);
  assert.equal(calls[0][1].commentId, 22);
  assert.match(calls[0][1].body, /new delta body/);
});

test("upsertStickyComment treats a custom marker as sticky", async () => {
  const customMarker = "<!-- archlucid:manifest-delta:tenant-acme -->";

  const client = {
    listIssueComments: async () => [
      { id: 22, body: `${DEFAULT_MARKER}\ndefault sticky from another tenant` },
      { id: 33, body: `${customMarker}\nthis is the one to update` },
    ],
    createIssueComment: async () => { throw new Error("must not create"); },
    updateIssueComment: async (args) => ({ id: args.commentId }),
  };

  const result = await upsertStickyComment({
    owner: "acme",
    repo: "platform",
    prNumber: 7,
    body: `${customMarker}\nfresh body`,
    marker: customMarker,
    client,
  });

  assert.equal(result.action, "updated");
  assert.equal(result.commentId, 33, "must not collide with the default-marker sticky");
});

test("upsertStickyComment validates required arguments", async () => {
  const client = {
    listIssueComments: async () => [],
    createIssueComment: async () => ({ id: 1 }),
    updateIssueComment: async () => ({ id: 1 }),
  };

  await assert.rejects(
    () => upsertStickyComment({ owner: "", repo: "r", prNumber: 1, body: "b", marker: "m", client }),
    /owner/,
  );
  await assert.rejects(
    () => upsertStickyComment({ owner: "o", repo: "", prNumber: 1, body: "b", marker: "m", client }),
    /repo/,
  );
  await assert.rejects(
    () => upsertStickyComment({ owner: "o", repo: "r", prNumber: "", body: "b", marker: "m", client }),
    /prNumber/,
  );
  await assert.rejects(
    () => upsertStickyComment({ owner: "o", repo: "r", prNumber: 1, body: "", marker: "m", client }),
    /body/,
  );
  await assert.rejects(
    () => upsertStickyComment({ owner: "o", repo: "r", prNumber: 1, body: "b", marker: "", client }),
    /marker/,
  );
  await assert.rejects(
    () => upsertStickyComment({ owner: "o", repo: "r", prNumber: 1, body: "b", marker: "m", client: null }),
    /client/,
  );
});
