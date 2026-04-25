import type { Metadata } from "next";
import Link from "next/link";

import { loadSeeItDemoPreview } from "./load-see-it-demo-preview";
import { SeeItMarketingBody } from "./SeeItMarketingBody";

export const revalidate = 300;

export const metadata: Metadata = {
  title: "ArchLucid · See it in 30 seconds",
  description:
    "No-install snapshot of the latest committed Contoso demo run — same JSON as GET /v1/demo/preview, with a static fallback when the API is unreachable.",
  robots: { index: true, follow: true },
  other: {
    "data-demo": "true",
  },
};

export default async function SeeItMarketingPage() {
  const { source, payload } = await loadSeeItDemoPreview();

  return (
    <main className="mx-auto max-w-3xl px-4 py-10">
      <h1 className="text-2xl font-semibold tracking-tight text-neutral-900 dark:text-neutral-50">
        See a real committed manifest in 30 seconds
      </h1>
      <p className="mt-2 text-sm text-neutral-600 dark:text-neutral-400">
        No install — data comes from <code className="text-xs">GET /v1/demo/preview</code> when the demo API is
        reachable; otherwise a checked-in snapshot keeps this page online.
      </p>
      <p className="mt-3 rounded-md border border-amber-200 bg-amber-50/80 px-3 py-2 text-xs text-amber-950 dark:border-amber-800 dark:bg-amber-950/50 dark:text-amber-100">
        <span className="font-semibold">Sample only:</span>{" "}
        <Link className="text-teal-800 underline underline-offset-2 dark:text-teal-200" href="/WORKED_EXAMPLE_ROI.pdf">
          See worked example (PDF)
        </Link>{" "}
        — fictional Contoso tenant ROI from the per-tenant value-report DOCX path (markdown mirror in the repo at{" "}
        <code className="text-[0.7rem]">docs/go-to-market/WORKED_EXAMPLE_ROI.md</code>).
      </p>
      <div className="mt-8">
        <SeeItMarketingBody source={source} payload={payload} />
      </div>
    </main>
  );
}
