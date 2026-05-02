/**
 * Minimal sample transformers for ITSM webhook bridges — no outbound HTTP.
 * Validates CloudEvents envelopes for Jira Table API vs ServiceNow Table API payloads.
 */

function requireString(payload, dottedKey) {
  const parts = dottedKey.split(".").filter(Boolean);
  let cur = payload;

  for (const p of parts) {
    if (cur === null || typeof cur !== "object" || !(p in cur)) {
      throw new Error(`Missing ${dottedKey}`);
    }

    cur = cur[p];
  }

  if (typeof cur !== "string" || cur.trim().length === 0) {
    throw new Error(`Bad string at ${dottedKey}`);
  }

  return cur.trim();
}

/**
 * Canonical idempotency key for bridge integrations: CloudEvent **id** (UUID) duplicates should be discarded by consumer.
 *
 * @param {Record<string, unknown>} cloudEvent
 * @returns {string}
 */
export function extractIntegrationIdempotencyKey(cloudEvent) {
  const idem = String(cloudEvent.id ?? "");

  if (idem.trim().length === 0) {
    throw new Error("missing CloudEvent id");
  }

  return idem;
}

/**
 * @param {Record<string, unknown>} cloudEvent
 * @returns {string}
 */
export function buildArchLucidCorrelationBacklink(cloudEvent) {
  if (typeof cloudEvent.data !== "object" || cloudEvent.data === null) {
    throw new Error("missing data object");
  }

  /** @type {Record<string, unknown>} */
  const data = cloudEvent.data;
  const runId = requireString(data, "runId");

  return `/reviews/${encodeURIComponent(runId)}`;
}

/**
 * @param {Record<string, unknown>} cloudEvent parsed JSON CloudEvent envelope
 * @returns {Record<string, unknown>} Jira issue create skeleton (ADF description)
 */
export function mapCloudEventToJiraIssueSkeleton(cloudEvent) {
  if (cloudEvent.specversion !== "1.0") {
    throw new Error("unsupported CloudEvents specversion");
  }

  if (typeof cloudEvent.data !== "object" || cloudEvent.data === null) {
    throw new Error("missing data object");
  }

  /** @type {Record<string, unknown>} */
  const data = cloudEvent.data;
  const runId = requireString(data, "runId");

  /** @type {Record<string, unknown>} */
  const issue = {
    fields: {
      project: { key: "PLACEHOLDER_PROJECT_KEY" },
      summary: `ArchLucid run completed — ${runId}`,
      description: {
        type: "doc",
        version: 1,
        content: [
          {
            type: "paragraph",
            content: [{ type: "text", text: `Run ${runId} (manifest in CloudEvent payload).` }],
          },
          {
            type: "paragraph",
            content: [{ type: "text", text: `ArchLucid backlink: ${buildArchLucidCorrelationBacklink(cloudEvent)}` }],
          },
        ],
      },
      issuetype: { name: "Task" },
    },
  };

  return issue;
}

/**
 * @param {Record<string, unknown>} cloudEvent
 * @returns {Record<string, unknown>} ServiceNow Table API JSON body skeleton
 */
export function mapCloudEventToServiceNowIncidentSkeleton(cloudEvent) {
  if (cloudEvent.type !== "com.archlucid.authority.run.completed") {
    throw new Error("unsupported event type");
  }

  if (typeof cloudEvent.data !== "object" || cloudEvent.data === null) {
    throw new Error("missing data object");
  }

  /** @type {Record<string, unknown>} */
  const data = cloudEvent.data;
  const runId = requireString(data, "runId");

  /** @type {Record<string, unknown>} */
  const incident = {
    short_description: `ArchLucid run completed (${runId})`,
    urgency: "2",
    impact: "2",
    correlation_id: extractIntegrationIdempotencyKey(cloudEvent),
    work_notes: JSON.stringify({
      manifestId: data.manifestId ?? null,
      tenantId: data.tenantId ?? null,
      cloudEventSource: cloudEvent.source,
      archlucidBacklink: buildArchLucidCorrelationBacklink(cloudEvent),
    }),
  };

  return incident;
}
