import type { Metadata } from "next";
import type { ReactElement, ReactNode } from "react";

import {
  DemoPreviewMarketingBody,
  DemoPreviewNotAvailable,
} from "../../demo/preview/DemoPreviewMarketingBody";
import type { DemoCommitPagePreviewResponse } from "@/types/demo-preview";
import { getShowcaseStaticDemoPayload, SHOWCASE_STATIC_DEMO_RUN_ID } from "@/lib/showcase-static-demo";

export const revalidate = 300;

type PageProps = {
  params: Promise<{ runId: string }>;
};

function resolveShowcaseApiBase(): string {
  const explicit = process.env.NEXT_PUBLIC_DEMO_PREVIEW_API_BASE?.trim();

  if (explicit)
    return explicit.replace(/\/$/, "");

  const server = process.env.ARCHLUCID_API_BASE_URL?.trim();

  if (server)
    return server.replace(/\/$/, "");

  const pub = process.env.NEXT_PUBLIC_ARCHLUCID_API_BASE_URL?.trim();

  if (pub)
    return pub.replace(/\/$/, "");

  return "";
}

function showcaseTitleForRunId(runId: string): string {
  if (runId === SHOWCASE_STATIC_DEMO_RUN_ID)
    return "Claims intake modernization";

  return runId;
}

function ShowcaseLead({ children }: { readonly children: ReactNode }) {
  return <p className="mt-2 text-sm text-neutral-600 dark:text-neutral-400">{children}</p>;
}

function ShowcaseLoadFailed(): ReactElement {
  return (
    <p className="mt-3 text-sm text-neutral-600 dark:text-neutral-400">
      This showcase could not be loaded right now. Please try again later.
    </p>
  );
}

/** Shared banner when browsing static baked-in demo preview (API not reachable). */
function ShowcaseStaticDemoBanner(): ReactElement {
  return (
    <div
      className="mt-4 rounded border border-sky-200 bg-sky-50 px-3 py-2 text-xs text-sky-950 dark:border-sky-800 dark:bg-sky-950/50 dark:text-sky-100"
      role="status"
      data-testid="showcase-static-demo-banner"
    >
      <span className="font-semibold">Static demo preview.</span> Viewing curated sample results. Connect a workspace with
      preview enabled to replace this with live data.
    </div>
  );
}

export async function generateMetadata(props: PageProps): Promise<Metadata> {
  const { runId } = await props.params;

  return {
    title: `ArchLucid · Showcase (${showcaseTitleForRunId(runId)})`,
    description: "Read-only public showcase of a committed healthcare-style architecture run.",
    robots: { index: true, follow: true },
  };
}

async function fetchShowcasePayload(
  url: string,
): Promise<{ kind: "ok"; payload: DemoCommitPagePreviewResponse } | { kind: "bad_json" } | { kind: "missing" } | { kind: "not_found" } | { kind: "http_error" } | { kind: "invalid" }> {
  try {
    const response = await fetch(url, { next: { revalidate: 300 } });

    if (response.status === 404)
      return { kind: "not_found" };

    if (!response.ok)
      return { kind: "http_error" };

    let payload: DemoCommitPagePreviewResponse;

    try {
      payload = (await response.json()) as DemoCommitPagePreviewResponse;
    } catch {
      return { kind: "bad_json" };
    }

    if (payload == null || typeof payload !== "object" || payload.run == null || payload.manifest == null)
      return { kind: "invalid" };

    return { kind: "ok", payload };
  } catch {
    return { kind: "missing" };
  }
}

/** Public read-only projection of finalized run preview (dynamic API path; static fallback when no API URL). */
export default async function MarketingShowcasePage(props: PageProps) {
  const { runId } = await props.params;
  const decodedRunId = decodeURIComponent(runId);
  const base = resolveShowcaseApiBase();

  if (!base) {
    const payload = getShowcaseStaticDemoPayload(decodedRunId);

    return (
      <main className="mx-auto max-w-5xl px-4 py-10">
        <h1 className="text-2xl font-semibold text-neutral-900 dark:text-neutral-50">Public showcase</h1>

        <ShowcaseStaticDemoBanner />

        <ShowcaseLead>A read-only view of a completed architecture analysis.</ShowcaseLead>

        <div className="mt-6">
          <DemoPreviewMarketingBody payload={payload} />
        </div>
      </main>
    );
  }

  const encoded = encodeURIComponent(runId);
  const url = `${base}/v1/marketing/showcase/${encoded}`;
  const bundle = await fetchShowcasePayload(url);

  switch (bundle.kind) {
    case "not_found": {
      return (
        <main className="mx-auto max-w-5xl px-4 py-10">
          <h1 className="text-2xl font-semibold text-neutral-900 dark:text-neutral-50">Public showcase</h1>

          <div className="mt-6">
            <DemoPreviewNotAvailable />
          </div>
        </main>
      );
    }

    case "ok":
      return (
        <main className="mx-auto max-w-5xl px-4 py-10">
          <h1 className="text-2xl font-semibold text-neutral-900 dark:text-neutral-50">Public showcase</h1>

          <ShowcaseLead>A read-only view of a completed architecture analysis.</ShowcaseLead>

          <div className="mt-6">
            <DemoPreviewMarketingBody payload={bundle.payload} />
          </div>
        </main>
      );

    case "bad_json": {
      return (
        <main className="mx-auto max-w-5xl px-4 py-10">
          <h1 className="text-2xl font-semibold text-neutral-900 dark:text-neutral-50">Public showcase</h1>

          <ShowcaseLoadFailed />

          <p className="mt-2 text-sm text-neutral-600 dark:text-neutral-400">
            The server returned data this page could not read.
          </p>
        </main>
      );
    }

    case "invalid": {
      return (
        <main className="mx-auto max-w-5xl px-4 py-10">
          <h1 className="text-2xl font-semibold text-neutral-900 dark:text-neutral-50">Public showcase</h1>

          <div className="mt-6">
            <DemoPreviewNotAvailable />
          </div>
        </main>
      );
    }

    case "http_error":
    case "missing": {
      return (
        <main className="mx-auto max-w-5xl px-4 py-10">
          <h1 className="text-2xl font-semibold text-neutral-900 dark:text-neutral-50">Public showcase</h1>

          <ShowcaseLoadFailed />
        </main>
      );
    }
  }
}
