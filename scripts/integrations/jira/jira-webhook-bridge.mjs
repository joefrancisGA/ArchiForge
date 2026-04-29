#!/usr/bin/env node
/**
 * ArchLucid CloudEvents → Jira REST v3 (reference bridge; no deps).
 *
 *   node jira-webhook-bridge.mjs process ./sample-alert-fired.json
 *   SKIP_HMAC=1 node … process ./sample-cloudevents.json
 *   ARCHLUCID_JIRA_BRIDGE_SERVE=1 node jira-webhook-bridge.mjs
 *
 * Env: ARCHLUCID_BASE_URL, ARCHLUCID_API_KEY, ARCHLUCID_WEBHOOK_HMAC_SECRET,
 * JIRA_BASE_URL, JIRA_EMAIL, JIRA_API_TOKEN, JIRA_PROJECT_KEY,
 * optional JIRA_ISSUE_TYPE (default Task), ALERT_ISSUE_TYPE (default Bug),
 * ARCHLUCID_JIRA_BRIDGE_PORT (8787), MAX_FINDINGS_PER_RUN (25).
 */
import crypto from "node:crypto";
import fs from "node:fs";
import http from "node:http";

const HDR = "x-archlucid-webhook-signature";
const T_RUN_DONE = "com.archlucid.authority.run.completed";
const T_ALERT = "com.archlucid.alert.fired";

function getenv(k, def = "") {
  const v = process.env[k];
  return !v||v==="" ? def : String(v).trim();
}

function trimSlash(u) {
  const t = String(u).trim();
  return t.endsWith("/") ? t.slice(0, -1) : t;
}

function computeHmac(secret, bodyUtf8) {
  return "sha256=" + crypto.createHmac("sha256", secret).update(bodyUtf8, "utf8").digest("hex");
}

/** @param {string} expectedSha256prefixed */
function timingSafeSig(expectedSha256prefixed, actualHeader) {
  const ex = Buffer.from(expectedSha256prefixed.replace(/^sha256=/, ""), "hex");
  const acStr = typeof actualHeader === "string" ? actualHeader.trim() : "";
  const ac = Buffer.from(acStr.replace(/^sha256=/i, ""), "hex");
  return ex.length === ac.length && ex.length > 0 && crypto.timingSafeEqual(ex, ac);
}

/** @param {unknown} sev */
function sevToPriority(sev) {
  const s = String(sev ?? "").toLowerCase();
  if (["critical"].includes(s) || s === "0") return "Highest";
  if (["high"].includes(s) || s === "1") return "High";
  if (["medium"].includes(s) || s === "2") return "Medium";
  if (["low", "info"].includes(s) || s === "3" || s === "4") return "Low";
  return "Medium";
}

/** @param {string[]} lines */
function adfDoc(lines) {
  return {
    type: "doc",
    version: 1,
    content: lines.map((text) => ({
      type: "paragraph",
      content: [{ type: "text", text }],
    })),
  };
}

async function httpJson(method, url, headers, bodyObj) {
  /** @type {RequestInit} */
  const opt = {
    method,
    headers,
  };
  if (bodyObj !== undefined) opt.body = JSON.stringify(bodyObj);
  const res = await fetch(url, opt);
  const txt = await res.text();
  if (!res.ok) throw new Error(`${method} ${url} -> ${res.status}: ${txt}`);
  try {
    return JSON.parse(txt);
  } catch {
    return {};
  }
}

async function getRun(apiBase, key, runId) {
  const u = `${trimSlash(apiBase)}/v1/authority/runs/${encodeURIComponent(runId)}`;
  return httpJson(
    "GET",
    u,
    { Accept: "application/json", "X-Api-Key": key },
    undefined,
  );
}

async function jiraPostIssue(jiraBase, email, tok, payload) {
  const u = `${trimSlash(jiraBase)}/rest/api/3/issue`;
  const b64 = Buffer.from(`${email}:${tok}`, "utf8").toString("base64");
  return httpJson("POST", u, {
    Authorization: `Basic ${b64}`,
    Accept: "application/json",
    "Content-Type": "application/json",
  }, payload);
}

async function handleEvent(_, rawBody, hdrSigOpt) {
  const secret = getenv("ARCHLUCID_WEBHOOK_HMAC_SECRET");
  const skip = getenv("SKIP_HMAC") === "1";

  if (!skip && secret) {
    const expected = computeHmac(secret, rawBody);

    if (typeof hdrSigOpt !== "string" || hdrSigOpt.length === 0) {
      throw new Error(
        "Webhook HMAC configured but signature header missing; set SKIP_HMAC=1 only for offline file replay.",
      );
    }


    if (!timingSafeSig(expected, hdrSigOpt)) {
      throw new Error("HMAC verification failed");
    }

  }



  /** @type {any} */
  const ce = JSON.parse(rawBody);
  const t = typeof ce.type === "string" ? ce.type.trim() : "";

  const archBase = getenv("ARCHLUCID_BASE_URL");
  const apiKey = getenv("ARCHLUCID_API_KEY");
  const jiraBase = getenv("JIRA_BASE_URL");
  const jMail = getenv("JIRA_EMAIL");
  const jTok = getenv("JIRA_API_TOKEN");
  const proj = getenv("JIRA_PROJECT_KEY");
  const issueTypeFind = getenv("JIRA_ISSUE_TYPE", "Task");
  const issueTypeAlert = getenv("JIRA_ISSUE_ALERT_TYPE", getenv("ALERT_ISSUE_TYPE", "Bug"));

  const maxF = Number.parseInt(getenv("MAX_FINDINGS_PER_RUN", "25"), 10) || 25;

  if (!jiraBase||!jMail||!jTok||!proj) {
    throw new Error("Missing JIRA_BASE_URL / JIRA_EMAIL / JIRA_API_TOKEN / JIRA_PROJECT_KEY");
  }

  /** @returns {Promise<Array<{ key: string }>> } */
  const out = [];

  if (t === T_ALERT) {
    const d = ce.data ?? {};
    const summary = `[ArchLucid Alert] ${d.title || "Alert"}`;
    const lines = [
      `Alert ID: ${d.alertId}`,
      `Severity: ${d.severity}`,
      `Category: ${d.category}`,
      `Rule ID: ${d.ruleId}`,
      `Deduplication: ${d.deduplicationKey}`,
      `Run ID: ${d.runId}`,
    ].map(String);
    const body = jiraPostIssue(jiraBase, jMail, jTok, {
      fields: {
        project: { key: proj },
        issuetype: { name: issueTypeAlert },
        summary: summary.slice(0, 250),
        priority: { name: sevToPriority(d.severity) },
        description: adfDoc(lines),
      },
    });
    out.push(await body);
    return out;
  }

  if (t === T_RUN_DONE) {
    if (!archBase||!apiKey) throw new Error("run.completed requires ARCHLUCID_BASE_URL and ARCHLUCID_API_KEY");
    const runId = ce.data?.runId;
    if (!runId) throw new Error("data.runId missing");
    const detail = await getRun(archBase, apiKey, runId);
    const findings = detail?.findingsSnapshot?.findings;
    const list = Array.isArray(findings) ? findings : [];
    let n = 0;
    for (const f of list) {
      if (n >= maxF) break;
      n++;
      const title = `[ArchLucid] ${f.title ?? f.findingId ?? "finding"}`;
      const prio = sevToPriority(f.severity);
      const descLines = [
        `Run ID: ${runId}`,
        `Finding ID: ${f.findingId}`,
        `Severity: ${f.severity}`,
        `Category: ${f.category}`,
        ``,
        `${f.rationale ?? ""}`.slice(0, 30000),
      ];
      const r = await jiraPostIssue(jiraBase, jMail, jTok, {
        fields: {
          project: { key: proj },
          issuetype: { name: issueTypeFind },
          summary: title.slice(0, 250),
          priority: { name: prio },
          description: adfDoc(descLines),
        },
      });
      out.push(r);
    }
    if (list.length === 0) {

      const r = await jiraPostIssue(jiraBase, jMail, jTok, {

        fields: {

          project: { key: proj },

          issuetype: { name: issueTypeFind },

          summary: `[ArchLucid] Run ${runId} (no findings in snapshot)`.slice(0, 250),

          description: adfDoc([`authority.run.completed webhook for run ${runId} — findings array empty.`]),

        },

      });


      out.push(r);

    }


    return out;

  }


  console.warn(JSON.stringify({ ok: false, ignoredType: t }));


  return [];


}

async function main() {
  const a = process.argv.slice(2);
  const serve = getenv("ARCHLUCID_JIRA_BRIDGE_SERVE") === "1";
  const port = Number.parseInt(getenv("ARCHLUCID_JIRA_BRIDGE_PORT", "8787"), 10);

  if (serve) {
    const secret = getenv("ARCHLUCID_WEBHOOK_HMAC_SECRET");
    if (!getenv("SKIP_HMAC") && !secret) {
      console.error("Set ARCHLUCID_WEBHOOK_HMAC_SECRET or SKIP_HMAC=1 for local dev.");

      process.exit(2);

    }



    const server = http.createServer((req, res) => {


      if (req.method !== "POST" || req.url !== "/webhook") {
        res.writeHead(405, { "Content-Type": "application/json" });

        res.end(JSON.stringify({ error: "POST /webhook only" }));

        return;


      }

      /** @type {Buffer[]} */


      const chunks = [];


      req.on("data", (d) => chunks.push(d));


      req.on("end", async () => {


        try {
          const raw = Buffer.concat(chunks).toString("utf8");

          const sig = req.headers[HDR];

          const results = await handleEvent(null, raw, typeof sig === "string" ? sig : undefined);


          res.writeHead(200, { "Content-Type": "application/json" });

          res.end(JSON.stringify({ ok: true, issues: results }));

        }

        catch (e) {


          console.error(String(e));


          const msg = String(e);

          const code = msg.includes("HMAC") ? 401 : 400;


          res.writeHead(code, { "Content-Type": "application/json" });


          res.end(JSON.stringify({ error: String(e) }));

        }

      });


    });


    server.listen(port, "127.0.0.1", () =>
      console.warn(`listening http://127.0.0.1:${port}/webhook`),

    );


    return;


  }


  if ((a[0] === "process" || a[0] === "file") && (a.length >= 2)) {
    const contents = fs.readFileSync(a[1], "utf8");
    console.log(JSON.stringify(await handleEvent(null, contents, undefined), undefined, 2));


    return;


  }



  console.log(`usage: ${process.argv[1]} process <cloudevents.json>`);


  console.log(`   or: ARCHLUCID_JIRA_BRIDGE_SERVE=1 …   # http://127.0.0.1:${port}/webhook`);

  process.exit(1);


}

await main();

