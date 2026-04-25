import type { Metadata } from "next";

import { DemoPreviewMarketingBody, DemoPreviewNotAvailable } from "../../demo/preview/DemoPreviewMarketingBody";
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
          Configure <code>NEXT_PUBLIC_DEMO_PREVIEW_API_BASE</code> (or server <code>ARCHLUCID_API_BASE_URL</code>) so this
          marketing host can reach the ArchLucid API anonymously.
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
        <p className="mt-3 text-sm text-red-700 dark:text-red-300">
          The showcase API returned HTTP {response.status}. Confirm the API is reachable at <code>{url}</code>.
        </p>
      </main>
    );
  }

  const payload = (await response.json()) as DemoCommitPagePreviewResponse;

  return (
    <main className="mx-auto max-w-5xl px-4 py-10">
      <h1 className="text-2xl font-semibold text-neutral-900 dark:text-neutral-50">Public showcase</h1>
      <p className="mt-2 text-sm text-neutral-600 dark:text-neutral-400">
        Read-only marketing view of run <code>{runId}</code> — same projection as the demo commit page preview.
      </p>
      <div className="mt-6">
        <DemoPreviewMarketingBody payload={payload} />
      </div>
    </main>
  );
}
