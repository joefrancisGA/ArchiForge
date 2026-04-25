import type { Metadata } from "next";
import Link from "next/link";

import { loadSampleAggregateRoiBulletinSyntheticMarkdown } from "@/marketing/load-sample-aggregate-roi-bulletin-synthetic";

export const metadata: Metadata = {
  title: "ArchLucid · Example aggregate ROI bulletin (synthetic)",
  description:
    "Illustrative aggregate baseline bulletin shape for procurement — not production data; real publication gates on admin preview with minTenants.",
  robots: { index: true, follow: true },
};

const adminRoiPreviewHref =
  "/api/proxy/v1/admin/roi-bulletin-preview?quarter=Q1-2026&minTenants=5";

export default function ExampleRoiBulletinMarketingPage() {
  const markdown = loadSampleAggregateRoiBulletinSyntheticMarkdown();

  return (
    <main className="mx-auto max-w-3xl px-4 py-10">
      <h1 className="text-2xl font-semibold tracking-tight text-neutral-900 dark:text-neutral-50">
        Example aggregate ROI bulletin (synthetic)
      </h1>
      <p className="mt-3 text-sm leading-relaxed text-neutral-700 dark:text-neutral-300">
        This page shows the <strong>Markdown shape</strong> of a quarterly aggregate baseline bulletin before{" "}
        <strong>N ≥ 5</strong> qualifying tenants exist. It is <strong>not</strong> a signed publication and must{" "}
        <strong>never</strong> receive a <code className="rounded bg-neutral-100 px-1 dark:bg-neutral-800">## … ROI bulletin signed:</code>{" "}
        entry in <code className="rounded bg-neutral-100 px-1 dark:bg-neutral-800">docs/CHANGELOG.md</code>.
      </p>

      <section
        className="mt-6 rounded-lg border border-amber-300 bg-amber-50 p-4 text-sm text-amber-950 dark:border-amber-700 dark:bg-amber-950/40 dark:text-amber-100"
        aria-label="Real publication gate"
      >
        <p className="m-0 font-medium">Real drafts (production SQL) are gated by the admin preview</p>
        <p className="mt-2 m-0 leading-relaxed">
          Authentic aggregate numbers require an API key with <strong>Admin access</strong>. The same contract the CLI
          uses is exposed here as a same-origin link (returns <strong>401/403</strong> without credentials — that is
          expected on a public marketing page):
        </p>
        <p className="mt-3 m-0">
          <a
            className="font-mono text-xs break-all text-amber-900 underline underline-offset-2 dark:text-amber-200"
            href={adminRoiPreviewHref}
          >
            {adminRoiPreviewHref}
          </a>
        </p>
      </section>

      <section className="mt-8" aria-labelledby="synthetic-md-heading">
        <h2 id="synthetic-md-heading" className="text-lg font-semibold text-neutral-900 dark:text-neutral-50">
          Checked-in sample Markdown
        </h2>
        <p className="mt-2 text-sm text-neutral-600 dark:text-neutral-400">
          Source of truth:{" "}
          <Link
            className="text-sky-700 underline underline-offset-2 dark:text-sky-400"
            href="https://github.com/joefrancisGA/ArchLucid/blob/main/docs/go-to-market/SAMPLE_AGGREGATE_ROI_BULLETIN_SYNTHETIC.md"
          >
            docs/go-to-market/SAMPLE_AGGREGATE_ROI_BULLETIN_SYNTHETIC.md
          </Link>{" "}
          (repository file; rendered below for convenience).
        </p>
        <pre className="mt-4 overflow-x-auto rounded-lg border border-neutral-200 bg-neutral-50 p-4 text-xs leading-relaxed text-neutral-800 dark:border-neutral-700 dark:bg-neutral-950 dark:text-neutral-200">
          {markdown}
        </pre>
      </section>

      <section className="mt-8 border-t border-neutral-200 pt-6 text-sm text-neutral-600 dark:border-neutral-800 dark:text-neutral-400">
        <p className="m-0">
          Generate a CLI synthetic draft (no API call):{" "}
          <code className="rounded bg-neutral-100 px-1 dark:bg-neutral-800">
            archlucid roi-bulletin --quarter Q1-2026 --synthetic [--explain] [--out file.md]
          </code>
          . Full CLI reference: <span className="font-mono">docs/CLI_USAGE.md</span> in the repository.
        </p>
      </section>
    </main>
  );
}
