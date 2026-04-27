import type { Metadata } from "next";

import {
  DemoPreviewMarketingBody,
  DemoPreviewNotAvailable,
} from "../../demo/preview/DemoPreviewMarketingBody";
import type { DemoCommitPagePreviewResponse } from "@/types/demo-preview";

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

export async function generateMetadata(props: PageProps): Promise<Metadata> {
  const { runId } = await props.params;

  return {
    title: `ArchLucid · Showcase (${runId})`,
    description: "Read-only public showcase of a committed Contoso-style architecture run.",
    robots: { index: true, follow: true },
  };
}

export default async function MarketingShowcasePage(props: PageProps) {
  const { runId } = await props.params;
  const base = resolveShowcaseApiBase();

  if (!base) {
    return (
      <main className="mx-auto max-w-5xl px-4 py-10">
        <h1 className="text-2xl font-semibold text-neutral-900 dark:text-neutral-50">Public showcase</h1>
        <p className="mt-3 text-sm text-neutral-600 dark:text-neutral-400">
          This example isn&apos;t available in the current environment.
        </p>
      </main>
    );
  }

  const encoded = encodeURIComponent(runId);
  const url = `${base}/v1/marketing/showcase/${encoded}`;
  const response = await fetch(url, { next: { revalidate: 300 } });

  if (response.status === 404)
    return (
      <main className="mx-auto max-w-5xl px-4 py-10">
        <h1 className="text-2xl font-semibold text-neutral-900 dark:text-neutral-50">Public showcase</h1>
        <div className="mt-6">
          <DemoPreviewNotAvailable />
        </div>
      </main>
    );

  if (!response.ok) {
    return (
      <main className="mx-auto max-w-5xl px-4 py-10">
        <h1 className="text-2xl font-semibold text-neutral-900 dark:text-neutral-50">Public showcase</h1>
        <p className="mt-3 text-sm text-neutral-600 dark:text-neutral-400">
          This showcase could not be loaded right now. Please try again later.
        </p>
      </main>
    );
  }

  let payload: DemoCommitPagePreviewResponse;
  try {
    payload = (await response.json()) as DemoCommitPagePreviewResponse;
  } catch {
    return (
      <main className="mx-auto max-w-5xl px-4 py-10">
        <h1 className="text-2xl font-semibold text-neutral-900 dark:text-neutral-50">Public showcase</h1>
        <p className="mt-3 text-sm text-neutral-600 dark:text-neutral-400">
          The server returned data this page could not read. Please try again later.
        </p>
      </main>
    );
  }

  if (payload == null || typeof payload !== "object" || payload.run == null || payload.manifest == null) {
    return (
      <main className="mx-auto max-w-5xl px-4 py-10">
        <h1 className="text-2xl font-semibold text-neutral-900 dark:text-neutral-50">Public showcase</h1>
        <div className="mt-6">
          <DemoPreviewNotAvailable />
        </div>
      </main>
    );
  }

  return (
    <main className="mx-auto max-w-5xl px-4 py-10">
      <h1 className="text-2xl font-semibold text-neutral-900 dark:text-neutral-50">Public showcase</h1>
      <p className="mt-2 text-sm text-neutral-600 dark:text-neutral-400">
        A read-only view of a completed architecture analysis.
      </p>
      <div className="mt-6">
        <DemoPreviewMarketingBody payload={payload} />
      </div>
    </main>
  );
}
