import type { Metadata } from "next";
import Link from "next/link";

import { DemoPreviewMarketingBody, DemoPreviewNotAvailable } from "../demo/preview/DemoPreviewMarketingBody";
import type { DemoCommitPagePreviewResponse } from "@/types/demo-preview";

export const revalidate = 300;

export const metadata: Metadata = {
  title: "ArchLucid · Live demo",
  description: "Read-only demo run bundle for procurement and sponsor walkthroughs.",
  robots: { index: false, follow: false },
};

function resolveDemoPreviewApiBase(): string {
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

export default async function LiveDemoMarketingPage() {
  const base = resolveDemoPreviewApiBase();

  if (!base) {
    return (
      <main className="mx-auto max-w-5xl px-4 py-10">
        <h1 className="text-2xl font-semibold text-neutral-900 dark:text-neutral-50">Live demo</h1>
        <p className="mt-3 text-sm text-neutral-600 dark:text-neutral-400">
          The live demo is not enabled in this environment.
        </p>
      </main>
    );
  }

  const url = `${base}/v1/public/demo/sample-run`;
  let response: Response;

  try {
    response = await fetch(url, { next: { revalidate: 300 } });
  } catch {
    return (
      <main className="mx-auto max-w-5xl px-4 py-10">
        <h1 className="text-2xl font-semibold text-neutral-900 dark:text-neutral-50">Live demo</h1>
        <p className="mt-3 text-sm text-neutral-600 dark:text-neutral-400">
          The live demo could not be loaded right now. Please try again later.
        </p>
      </main>
    );
  }

  if (response.status === 404)
    return (
      <main className="mx-auto max-w-5xl px-4 py-10">
        <h1 className="text-2xl font-semibold text-neutral-900 dark:text-neutral-50">Live demo</h1>
        <div className="mt-6">
          <DemoPreviewNotAvailable />
        </div>
      </main>
    );

  if (!response.ok) {
    return (
      <main className="mx-auto max-w-5xl px-4 py-10">
        <h1 className="text-2xl font-semibold text-neutral-900 dark:text-neutral-50">Live demo</h1>
        <p className="mt-3 text-sm text-neutral-600 dark:text-neutral-400">
          The live demo could not be loaded right now. Please try again later.
        </p>
      </main>
    );
  }

  const payload = (await response.json()) as DemoCommitPagePreviewResponse;

  return (
    <main className="mx-auto max-w-5xl px-4 py-10">
      <h1 className="text-2xl font-semibold text-neutral-900 dark:text-neutral-50">Live demo</h1>
      <p className="mt-2 text-sm text-neutral-600 dark:text-neutral-400">
        A read-only view of a completed architecture analysis.
      </p>
      <p className="mt-2 text-xs text-neutral-500 dark:text-neutral-400">
        <span className="font-semibold text-neutral-600 dark:text-neutral-300">Verify:</span> this host renders JSON from{" "}
        <code className="text-[0.8rem]">GET …/v1/public/demo/sample-run</code> — parity UI lives at{" "}
        <Link className="text-teal-700 underline underline-offset-2 dark:text-teal-300" href="/demo/preview">
          /demo/preview
        </Link>{" "}
        (anonymous cached demo manifest page).
      </p>
      <div className="mt-8">
        <DemoPreviewMarketingBody payload={payload} />
      </div>
    </main>
  );
}
