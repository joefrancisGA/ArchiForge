import assert from "node:assert/strict";
import { readFile } from "node:fs/promises";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";
import test from "node:test";

import {
  buildArchLucidCorrelationBacklink,
  extractIntegrationIdempotencyKey,
  mapCloudEventToJiraIssueSkeleton,
  mapCloudEventToServiceNowIncidentSkeleton,
} from "./bridge-mapping.mjs";

const __dirname = dirname(fileURLToPath(import.meta.url));

test("fixture run.completed has required envelope + data fields", async () => {
  const raw = await readFile(join(__dirname, "fixture-run-completed.json"), "utf8");
  const cloudEvent = JSON.parse(raw);

  assert.equal(cloudEvent.specversion, "1.0");

  assert.equal(cloudEvent.type, "com.archlucid.authority.run.completed");
  assert.match(String(cloudEvent.source), /archlucid/i);

  assert.match(String(cloudEvent.id), /^[a-fA-F0-9-]{10,}$/);

  assert.equal(typeof cloudEvent.data, "object");

  /** @type {Record<string, unknown>} */
  const data = cloudEvent.data;

  for (const k of ["runId", "manifestId", "tenantId", "workspaceId", "projectId"]) {
    assert.equal(typeof data[k], "string");

    assert.ok(String(data[k]).length > 0);
  }

  assert.equal(extractIntegrationIdempotencyKey(cloudEvent), cloudEvent.id);
  assert.equal(buildArchLucidCorrelationBacklink(cloudEvent), `/reviews/${cloudEvent.data.runId}`);
});

test("mapCloudEventToJiraIssueSkeleton returns valid ADF + summary includes run id", async () => {
  const cloudEvent = JSON.parse(await readFile(join(__dirname, "fixture-run-completed.json"), "utf8"));

  const issue = mapCloudEventToJiraIssueSkeleton(cloudEvent);

  assert.ok(String(issue.fields.summary).includes(cloudEvent.data.runId));
  assert.ok(issue.fields.description?.content?.length > 0);
  assert.match(JSON.stringify(issue.fields.description), /ArchLucid backlink/);
});

test("mapCloudEventToServiceNowIncidentSkeleton carries correlation_id = CloudEvent id", async () => {
  const cloudEvent = JSON.parse(await readFile(join(__dirname, "fixture-run-completed.json"), "utf8"));

  const inc = mapCloudEventToServiceNowIncidentSkeleton(cloudEvent);

  assert.equal(inc.correlation_id, cloudEvent.id);

  assert.ok(String(inc.short_description).includes(String(cloudEvent.data.runId)));
  assert.match(String(inc.work_notes), /archlucidBacklink/);
});
