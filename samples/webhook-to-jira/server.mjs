/**
 * Minimal ArchLucid outbound-webhook receiver → Jira-shaped payload (sample only).
 * Requires: Node 18+ (global fetch). Env: WEBHOOK_SECRET (required), PORT (default 8787).
 */
import http from "node:http";
import crypto from "node:crypto";

const headerName = "x-archlucid-webhook-signature";
const prefix = "sha256=";

function verifySignature(secret, rawBodyUtf8, headerValue) {
  if (!headerValue || !headerValue.startsWith(prefix)) return false;
  const expected = crypto.createHmac("sha256", secret).update(rawBodyUtf8).digest("hex").toLowerCase();
  const provided = headerValue.slice(prefix.length).trim().toLowerCase();
  return provided.length === expected.length && crypto.timingSafeEqual(Buffer.from(expected), Buffer.from(provided));
}

function severityToJiraPriority(cloudEvent) {
  const data = cloudEvent?.data ?? cloudEvent;
  const sev = (data?.severity ?? data?.Severity ?? "medium").toString().toLowerCase();
  if (sev === "critical" || sev === "error") return "Highest";
  if (sev === "warning") return "High";
  return "Medium";
}

function buildJiraIssuePayload(cloudEvent) {
  const data = cloudEvent?.data ?? cloudEvent;
  const title = data?.title ?? data?.Title ?? cloudEvent?.type ?? "ArchLucid webhook";
  const body = typeof data === "object" ? JSON.stringify(data, null, 2) : String(data ?? "");

  return {
    fields: {
      project: { key: process.env.JIRA_PROJECT_KEY ?? "DEMO" },
      summary: `[ArchLucid] ${title}`.slice(0, 254),
      description: { type: "doc", version: 1, content: [{ type: "paragraph", content: [{ type: "text", text: body.slice(0, 32000) }] }] },
      issuetype: { name: "Task" },
      priority: { name: severityToJiraPriority(cloudEvent) },
    },
  };
}

const server = http.createServer(async (req, res) => {
  if (req.method !== "POST" || req.url !== "/webhook") {
    res.writeHead(404).end();
    return;
  }

  const secret = process.env.WEBHOOK_SECRET ?? "";
  if (!secret) {
    res.writeHead(500, { "content-type": "text/plain" }).end("WEBHOOK_SECRET is required");
    return;
  }

  const chunks = [];
  for await (const c of req) chunks.push(c);
  const raw = Buffer.concat(chunks);
  const rawUtf8 = raw.toString("utf8");
  const sig = req.headers[headerName];
  const sigStr = Array.isArray(sig) ? sig[0] : sig ?? "";

  if (!verifySignature(secret, rawUtf8, sigStr)) {
    res.writeHead(401, { "content-type": "text/plain" }).end("invalid signature");
    return;
  }

  let cloudEvent;
  try {
    cloudEvent = JSON.parse(rawUtf8);
  } catch {
    res.writeHead(400, { "content-type": "text/plain" }).end("invalid json");
    return;
  }

  const jiraPayload = buildJiraIssuePayload(cloudEvent);
  const base = process.env.JIRA_BASE_URL ?? "";
  const email = process.env.JIRA_EMAIL ?? "";
  const token = process.env.JIRA_API_TOKEN ?? "";

  if (base && email && token) {
    const auth = Buffer.from(`${email}:${token}`).toString("base64");
    const jiraRes = await fetch(`${base.replace(/\/$/, "")}/rest/api/3/issue`, {
      method: "POST",
      headers: { authorization: `Basic ${auth}`, "content-type": "application/json" },
      body: JSON.stringify(jiraPayload),
    });
    const text = await jiraRes.text();
    res.writeHead(jiraRes.ok ? 200 : 502, { "content-type": "application/json" }).end(text);
    return;
  }

  res
    .writeHead(200, { "content-type": "application/json" })
    .end(JSON.stringify({ ok: true, mode: "dry-run", jiraPayload }, null, 2));
});

const port = Number(process.env.PORT ?? 8787);
server.listen(port, "127.0.0.1", () => {
  console.log(`webhook-to-jira sample listening on http://127.0.0.1:${port}/webhook`);
});
