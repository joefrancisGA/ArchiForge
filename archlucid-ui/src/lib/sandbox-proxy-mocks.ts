import { NextResponse } from "next/server";

import { CORRELATION_ID_HEADER } from "@/lib/correlation";

/**
 * When `ARCHLUCID_UI_SANDBOX_MOCKS` is `true` / `1` / `yes`, the Next.js API proxy returns canned JSON for a small
 * allow-list of GET routes so the operator UI can be explored without the C# API. Server-side only (`archlucid-ui/.env.local`).
 */
export function trySandboxProxyMock(
  method: "GET" | "POST",
  pathSegments: string[],
  correlationId: string,
): NextResponse | null {
  const raw = process.env.ARCHLUCID_UI_SANDBOX_MOCKS?.trim().toLowerCase() ?? "";

  if (raw !== "1" && raw !== "true" && raw !== "yes") {
    return null;
  }

  if (method !== "GET") {
    return null;
  }

  const canonicalPath = pathSegments.join("/");
  const pathKey = canonicalPath.toLowerCase();

  const id = correlationId.trim().length > 0 ? correlationId.trim() : "sandbox";

  if (pathKey === "v1/architecture/telemetry/roi") {
    return jsonResponse(
      {
        totalRuns: 12,
        totalHoursSaved: 48.5,
        averageTimeToCommitMs: 220_000,
      },
      id,
    );
  }

  const provenanceMatch =
    /^v1\/architecture\/runs\/([^/]+)\/provenance$/i.exec(canonicalPath);

  if (provenanceMatch) {
    const runId = provenanceMatch[1];

    return jsonResponse(
      {
        runId,
        nodes: [
          {
            id: "sandbox-node-context",
            type: "ContextSnapshot",
            referenceId: "00000000-0000-4000-8000-000000000001",
            name: "Sandbox context",
            metadata: { source: "mock" },
          },
          {
            id: "sandbox-node-manifest",
            type: "GoldenManifest",
            referenceId: "00000000-0000-4000-8000-000000000002",
            name: "Sandbox manifest",
          },
        ],
        edges: [
          {
            id: "sandbox-edge-1",
            type: "derivedFrom",
            fromNodeId: "sandbox-node-context",
            toNodeId: "sandbox-node-manifest",
          },
        ],
        timeline: [
          {
            timestampUtc: "2026-05-01T12:00:00.000Z",
            kind: "RunStarted",
            label: "Sandbox timeline event",
            referenceId: runId,
          },
        ],
        traceabilityGaps: [],
      },
      id,
    );
  }

  return null;
}

function jsonResponse(body: unknown, correlationId: string): NextResponse {
  const res = NextResponse.json(body, { status: 200 });
  res.headers.set(CORRELATION_ID_HEADER, correlationId);

  return res;
}
