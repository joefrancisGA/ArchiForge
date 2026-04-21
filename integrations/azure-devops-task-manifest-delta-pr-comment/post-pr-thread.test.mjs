/**
 * Run: node --test integrations/azure-devops-task-manifest-delta-pr-comment/post-pr-thread.test.mjs
 */
import assert from "node:assert/strict";
import { readFile } from "node:fs/promises";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";
import { test } from "node:test";

import {
  buildBody,
  buildPullRequestApiBase,
  findStickyMatches,
  listAllThreads,
  resolveAdoAuthHeaders,
  upsertStickyPrThreadAndStatus,
} from "./post-pr-thread.mjs";

import {
  buildStatusCreateJson,
  buildThreadCreateJson,
} from "./post-pr-thread-wire.mjs";

const __dirname = dirname(fileURLToPath(import.meta.url));

async function loadFixture(name) {
  const p = join(__dirname, "tests", "fixtures", "azure-devops-pipeline-task", name);

  return (await readFile(p, "utf8")).trim();
}

function saveEnv() {
  return {
    pat: process.env.ARCHLUCID_AZURE_DEVOPS_PAT,
    sys: process.env.SYSTEM_ACCESSTOKEN,
  };
}

function restoreEnv(saved) {
  if (saved.pat === undefined) delete process.env.ARCHLUCID_AZURE_DEVOPS_PAT;
  else process.env.ARCHLUCID_AZURE_DEVOPS_PAT = saved.pat;

  if (saved.sys === undefined) delete process.env.SYSTEM_ACCESSTOKEN;
  else process.env.SYSTEM_ACCESSTOKEN = saved.sys;
}

test("wire: thread JSON matches C# golden fixture", async () => {
  const expected = await loadFixture("thread-create.sample.json");
  const md = "## ArchLucid sample markdown\n\n- line\n";
  const actual = buildThreadCreateJson(md);

  assert.equal(actual, expected);
});

test("wire: status JSON matches C# golden fixtures", async () => {
  assert.equal(
    buildStatusCreateJson("Operator compare ready.", "https://operator.example/compare?left=a&right=b"),
    await loadFixture("status-create.sample.json"),
  );

  assert.equal(
    buildStatusCreateJson("Short desc", null),
    await loadFixture("status-create.no-target-url.sample.json"),
  );
});

test("buildBody prepends marker", () => {
  assert.equal(buildBody("<!-- m -->", "x"), "<!-- m -->\nx");
});

test("resolveAdoAuthHeaders: Bearer when PAT empty and SYSTEM_ACCESSTOKEN set", () => {
  const saved = saveEnv();
  process.env.ARCHLUCID_AZURE_DEVOPS_PAT = "";
  process.env.SYSTEM_ACCESSTOKEN = "secret-sys-value";

  try {
    const h = resolveAdoAuthHeaders();

    assert.equal(h.authorization, "Bearer secret-sys-value");
  }
  finally {
    restoreEnv(saved);
  }
});

test("resolveAdoAuthHeaders: Basic when PAT set", () => {
  const saved = saveEnv();
  process.env.ARCHLUCID_AZURE_DEVOPS_PAT = "patvalue";
  delete process.env.SYSTEM_ACCESSTOKEN;

  try {
    const h = resolveAdoAuthHeaders();
    const expected = `Basic ${Buffer.from(":patvalue", "utf8").toString("base64")}`;

    assert.equal(h.authorization, expected);
  }
  finally {
    restoreEnv(saved);
  }
});

test("listAllThreads paginates until short page", async () => {
  let calls = 0;
  const fullPage = Array.from({ length: 100 }, (_, i) => ({
    id: i,
    comments: [],
    lastUpdatedDate: "2020-01-01",
  }));

  const fetchImpl = async url => {
    calls++;
    const u = String(url);

    if (calls === 1) {
      assert.ok(u.includes("skip=0"));

      return {
        ok: true,
        async json() {
          return { value: fullPage };
        },
      };
    }

    assert.ok(u.includes("skip=100"));

    return {
      ok: true,
      async json() {
        return { value: [{ id: 999, comments: [], lastUpdatedDate: "2021-01-01" }] };
      },
    };
  };

  const threads = await listAllThreads(
    "https://dev.azure.com/o/p/_apis/git/repositories/r/pullrequests/1",
    fetchImpl,
    { authorization: "Bearer x" },
    100,
  );

  assert.equal(threads.length, 101);
  assert.equal(calls, 2);
});

test("upsertStickyPrThreadAndStatus creates thread when no marker match", async () => {
  const fetchImpl = async (url, init) => {
    const u = String(url);

    if (u.includes("/threads?") && init?.method === "GET") {
      return {
        ok: true,
        async json() {
          return { value: [] };
        },
      };
    }

    if (init?.method === "POST" && u.includes("/threads")) {
      return { ok: true, async json() { return {}; }, async text() { return ""; } };
    }

    throw new Error(`unexpected ${u} ${init?.method}`);
  };

  const auth = { authorization: "Bearer z" };

  const result = await upsertStickyPrThreadAndStatus({
    basePath: "https://dev.azure.com/o/p/_apis/git/repositories/r/pullrequests/1",
    marker: "<!-- archlucid:manifest-delta -->",
    fullBody: "<!-- archlucid:manifest-delta -->\n## hi",
    fetchImpl,
    authHeaders: auth,
  });

  assert.equal(result.action, "created");
});

test("401 from list threads throws", async () => {
  const fetchImpl = async () => ({
    ok: false,
    status: 401,
    async text() {
      return "nope";
    },
  });

  await assert.rejects(
    () =>
      listAllThreads(
        "https://dev.azure.com/o/p/_apis/git/repositories/r/pullrequests/1",
        fetchImpl,
        { authorization: "Bearer x" },
      ),
    /401/,
  );
});

test("redaction: console.error from resolveAdoAuthHeaders does not echo PAT value", () => {
  const saved = saveEnv();
  process.env.ARCHLUCID_AZURE_DEVOPS_PAT = "supersecretpat999";
  delete process.env.SYSTEM_ACCESSTOKEN;

  const original = console.error;
  const lines = [];

  console.error = (...args) => {
    lines.push(args.map(a => String(a)).join(" "));
  };

  try {
    resolveAdoAuthHeaders();
    const combined = lines.join("|");

    assert.ok(!combined.includes("supersecretpat999"));
  }
  finally {
    console.error = original;
    restoreEnv(saved);
  }
});

test("findStickyMatches collects thread/comment ids", () => {
  const marker = "<!-- archlucid:manifest-delta -->";
  const threads = [
    {
      id: 10,
      lastUpdatedDate: "2021-01-02",
      comments: [{ id: 100, content: `noise` }],
    },
    {
      id: 20,
      lastUpdatedDate: "2022-01-02",
      comments: [{ id: 200, content: `${marker}\nbody` }],
    },
  ];

  const m = findStickyMatches(threads, marker);

  assert.equal(m.length, 1);
  assert.equal(m[0].threadId, 20);
});

test("buildPullRequestApiBase encodes org and project", () => {
  const b = buildPullRequestApiBase("my org", "proj name", "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa", 7);

  assert.ok(b.includes("my%20org"));
  assert.ok(b.includes("proj%20name"));
});
