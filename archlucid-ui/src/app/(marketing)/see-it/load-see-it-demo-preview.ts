import { readFileSync } from "node:fs";
import { join } from "node:path";

import type { DemoCommitPagePreviewResponse } from "@/types/demo-preview";

export type SeeItPreviewSource = "live" | "snapshot";

export type SeeItLoadResult = {
  source: SeeItPreviewSource;
  /** Present when live fetch succeeded (for debugging / future cache headers). */
  etag?: string;
  payload: DemoCommitPagePreviewResponse;
};

/** Next.js extends `RequestInit` with cache directives for server `fetch`. */
export type SeeItFetchInit = RequestInit & { next?: { revalidate?: number; tags?: string[] } };

export type LoadSeeItDemoPreviewOptions = {
  fetchFn?: (input: RequestInfo | URL, init?: SeeItFetchInit) => Promise<Response>;
  readSnapshotFile?: () => DemoCommitPagePreviewResponse;
};

const DEMO_PREVIEW_PATH = "/v1/demo/preview";

const FETCH_TIMEOUT_MS = 12_000;

/**
 * Resolves anonymous demo API base. Prefer `NEXT_PUBLIC_DEMO_API_BASE` (see-it prompt), then the same chain as
 * `/demo/preview`.
 */
export function resolveSeeItDemoApiBase(): string {
  const demoApi = process.env.NEXT_PUBLIC_DEMO_API_BASE?.trim();

  if (demoApi)
    return demoApi.replace(/\/$/, "");

  const preview = process.env.NEXT_PUBLIC_DEMO_PREVIEW_API_BASE?.trim();

  if (preview)
    return preview.replace(/\/$/, "");

  const server = process.env.ARCHLUCID_API_BASE_URL?.trim();

  if (server)
    return server.replace(/\/$/, "");

  const pub = process.env.NEXT_PUBLIC_ARCHLUCID_API_BASE_URL?.trim();

  if (pub)
    return pub.replace(/\/$/, "");

  return "";
}

function defaultReadSnapshotFromPublic(): DemoCommitPagePreviewResponse {
  const path = join(process.cwd(), "public", "demo-preview-snapshot.json");
  const raw = readFileSync(path, "utf8");

  return JSON.parse(raw) as DemoCommitPagePreviewResponse;
}

function tryReadOptionalEtagFromPublic(): string | undefined {
  try {
    const path = join(process.cwd(), "public", "demo-preview-snapshot.etag");
    const raw = readFileSync(path, "utf8").trim();

    if (raw)
      return raw;
  } catch {
    // Optional file — ignore.
  }

  return undefined;
}

/**
 * Fetches `GET /v1/demo/preview` (rate limiting applies on the API host; single request, no auth).
 * On non-2xx, timeout, network error, or empty base: returns the checked-in snapshot from `public/demo-preview-snapshot.json`.
 */
export async function loadSeeItDemoPreview(options?: LoadSeeItDemoPreviewOptions): Promise<SeeItLoadResult> {
  const fetchFn = options?.fetchFn ?? ((input: RequestInfo | URL, init?: SeeItFetchInit) => fetch(input, init));
  const readSnapshot = options?.readSnapshotFile ?? defaultReadSnapshotFromPublic;
  const base = resolveSeeItDemoApiBase();

  if (!base) {
    return { source: "snapshot", payload: readSnapshot() };
  }

  const url = `${base}${DEMO_PREVIEW_PATH}`;
  const headers: Record<string, string> = { Accept: "application/json" };
  const optionalEtag = tryReadOptionalEtagFromPublic();

  if (optionalEtag)
    headers["If-None-Match"] = optionalEtag;

  try {
    const response = await fetchFn(url, {
      method: "GET",
      headers,
      signal: AbortSignal.timeout(FETCH_TIMEOUT_MS),
      next: { revalidate: 300 },
    });

    if (response.status === 304)
      return { source: "snapshot", etag: optionalEtag, payload: readSnapshot() };

    if (!response.ok)
      return { source: "snapshot", payload: readSnapshot() };

    const etag = response.headers.get("ETag")?.trim() ?? undefined;
    const payload = (await response.json()) as DemoCommitPagePreviewResponse;

    return { source: "live", etag, payload };
  } catch {
    return { source: "snapshot", payload: readSnapshot() };
  }
}
