import Link from "next/link";

import { type WhyArchLucidComparisonRow } from "@/marketing/why-archlucid-comparison";

export type WhyArchlucidMarketingViewProps = {
  rows: readonly WhyArchLucidComparisonRow[];
  /**
   * When false, skips the /demo/preview iframe (jest-axe cannot scan iframes in jsdom).
   * Production page passes true (default).
   */
  showDemoEmbed?: boolean;
};

function CitationCell({ citation }: { citation: string }) {
  const trimmed = citation.trim();

  if (trimmed.startsWith("http://") || trimmed.startsWith("https://")) {
    return (
      <a
        className="text-sky-700 underline underline-offset-2 dark:text-sky-400"
        href={trimmed}
        rel="noopener noreferrer"
        target="_blank"
      >
        {trimmed}
      </a>
    );
  }

  return <span className="text-neutral-700 dark:text-neutral-300">{trimmed}</span>;
}

/**
 * Public “Why ArchLucid” differentiation page — no operator auth; citations are enforced in Vitest.
 */
export function WhyArchlucidMarketingView({ rows, showDemoEmbed = true }: WhyArchlucidMarketingViewProps) {
  return (
    <main className="mx-auto max-w-5xl px-4 py-10">
      <h1 className="text-3xl font-semibold tracking-tight text-neutral-900 dark:text-neutral-50">
        Why ArchLucid
      </h1>
      <p className="mt-3 max-w-3xl text-sm leading-relaxed text-neutral-700 dark:text-neutral-300">
        ArchLucid is an AI Architecture Intelligence platform: specialized agents analyze architecture requests,
        produce explainable findings, and feed governance workflows with a durable audit trail — grounded in what
        ships today in V1. The table below lists **five capability claims**; each row cites either a path in this
        repository, a public HTTPS source, or an explicit first-party baseline disclaimer — the same strings ship in
        the downloadable proof-pack PDF (CI keeps the page and PDF builder in sync).
      </p>

      <section className="mt-8 rounded-lg border border-neutral-200 bg-neutral-50/80 p-4 dark:border-neutral-800 dark:bg-neutral-900/40">
        <h2 className="text-base font-semibold text-neutral-900 dark:text-neutral-50">Side-by-side proof pack</h2>
        <p className="mt-2 max-w-3xl text-sm leading-relaxed text-neutral-700 dark:text-neutral-300">
          Download a single PDF that bundles the same deterministic demo preview as <code>/demo/preview</code> (manifest
          excerpt, explanation, citations, timeline) plus the **benchmarked differentiation** table (identical rows to
          the inline table below). Requires demo mode on the API host (otherwise the link returns 404 by design).
        </p>
        <p className="mt-3">
          <a
            data-testid="why-proof-pack-download"
            className="inline-flex items-center rounded-md bg-sky-700 px-3 py-2 text-sm font-medium text-white hover:bg-sky-600 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-sky-500 dark:bg-sky-600 dark:hover:bg-sky-500"
            href="/api/proxy/v1/marketing/why-archlucid-pack.pdf"
            download="why-archlucid-pack.pdf"
          >
            Download the side-by-side proof pack (PDF)
          </a>
        </p>
      </section>

      <section className="mt-10" aria-labelledby="why-demo-heading">
        <h2 id="why-demo-heading" className="text-xl font-semibold text-neutral-900 dark:text-neutral-50">
          See a real commit-shaped page
        </h2>
        <p className="mt-2 max-w-3xl text-sm leading-relaxed text-neutral-600 dark:text-neutral-400">
          Anonymous read-only preview of the latest committed demo-seed run (cached JSON + marketing UI). Same shape as
          the operator commit experience — no sign-in. Described in docs/DEMO_PREVIEW.md and docs/adr/0027-demo-preview-cached-anonymous-commit-page.md.
        </p>
        {showDemoEmbed ? (
          <div className="mt-4 overflow-hidden rounded-lg border border-neutral-200 bg-white shadow-sm dark:border-neutral-800 dark:bg-neutral-900">
            <iframe
              title="ArchLucid demo commit page preview"
              src="/demo/preview"
              className="h-[min(70vh,520px)] w-full border-0"
              loading="lazy"
            />
          </div>
        ) : (
          <p
            className="mt-4 rounded-lg border border-dashed border-neutral-300 bg-neutral-50 px-3 py-8 text-center text-sm text-neutral-600 dark:border-neutral-700 dark:bg-neutral-900 dark:text-neutral-400"
            data-testid="why-demo-embed-placeholder"
          >
            Demo embed omitted (test build). In production this area shows an iframe of{" "}
            <Link className="text-sky-700 underline underline-offset-2 dark:text-sky-400" href="/demo/preview">
              /demo/preview
            </Link>
            .
          </p>
        )}
        <p className="mt-3 text-sm text-neutral-600 dark:text-neutral-400">
          {showDemoEmbed ? "Opens in-page above; " : null}
          <Link className="text-sky-700 underline underline-offset-2 hover:text-sky-600 dark:text-sky-400" href="/demo/preview">
            Open /demo/preview
          </Link>
          {showDemoEmbed ? " in a full tab." : "."}
        </p>
      </section>

      <section className="mt-12" aria-labelledby="why-compare-heading">
        <h2 id="why-compare-heading" className="text-xl font-semibold text-neutral-900 dark:text-neutral-50">
          Benchmarked differentiation (five claims)
        </h2>
        <p className="mt-2 max-w-3xl text-sm text-neutral-600 dark:text-neutral-400">
          Competitor baselines use neutral category labels and hour ranges where we do not yet have a third-party study
          — those cells carry the explicit first-party disclaimer in the citation column. ArchLucid evidence points only
          at artifacts in this repository or public routes; there are no roadmap-only claims in this table.
        </p>

        <div className="mt-4 overflow-x-auto rounded-lg border border-neutral-200 dark:border-neutral-800">
          <table className="w-full min-w-[960px] border-collapse text-left text-sm">
            <caption className="border-b border-neutral-200 bg-neutral-100 px-3 py-2 text-left text-neutral-800 dark:border-neutral-700 dark:bg-neutral-900 dark:text-neutral-200">
              Five capability claims — claim, evidence, baseline, citation, narrative (same row order as the PDF pack)
            </caption>
            <thead>
              <tr className="border-b border-neutral-200 bg-neutral-50 dark:border-neutral-800 dark:bg-neutral-900/80">
                <th scope="col" className="px-3 py-2 font-semibold text-neutral-900 dark:text-neutral-100">
                  Claim
                </th>
                <th scope="col" className="px-3 py-2 font-semibold text-neutral-900 dark:text-neutral-100">
                  ArchLucid evidence
                </th>
                <th scope="col" className="px-3 py-2 font-semibold text-neutral-900 dark:text-neutral-100">
                  Competitor baseline
                </th>
                <th scope="col" className="px-3 py-2 font-semibold text-neutral-900 dark:text-neutral-100">
                  Citation
                </th>
                <th scope="col" className="px-3 py-2 font-semibold text-neutral-900 dark:text-neutral-100">
                  Narrative (≤4 sentences)
                </th>
              </tr>
            </thead>
            <tbody>
              {rows.map((row, index) => (
                <tr
                  key={`why-row-${index}`}
                  className="border-b border-neutral-100 odd:bg-white even:bg-neutral-50/80 dark:border-neutral-800 dark:odd:bg-neutral-950 dark:even:bg-neutral-900/40"
                >
                  <th
                    scope="row"
                    className="max-w-[220px] px-3 py-3 align-top font-medium text-neutral-900 dark:text-neutral-100"
                  >
                    {row.claim}
                  </th>
                  <td className="max-w-[260px] px-3 py-3 align-top text-neutral-700 dark:text-neutral-300">
                    {row.archlucidEvidence}
                  </td>
                  <td className="max-w-[260px] px-3 py-3 align-top text-neutral-700 dark:text-neutral-300">
                    {row.competitorBaseline}
                  </td>
                  <td className="max-w-[200px] px-3 py-3 align-top text-neutral-700 dark:text-neutral-300">
                    <CitationCell citation={row.citation} />
                  </td>
                  <td className="max-w-[320px] px-3 py-3 align-top text-sm leading-relaxed text-neutral-700 dark:text-neutral-300">
                    {row.narrativeParagraph}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </section>

      <section className="mt-10 border-t border-neutral-200 pt-8 dark:border-neutral-800">
        <p className="text-sm text-neutral-600 dark:text-neutral-400">
          For the executive sponsor narrative, see docs/EXECUTIVE_SPONSOR_BRIEF.md. For positioning phrasing, see
          docs/go-to-market/POSITIONING.md.
        </p>
      </section>
    </main>
  );
}
