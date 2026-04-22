import Link from "next/link";

import {
  WHY_ARCHLUCID_COMPETITOR_LANDSCAPE_CITATION,
  type WhyArchlucidComparisonRow,
} from "@/marketing/why-archlucid-comparison";

export type WhyArchlucidMarketingViewProps = {
  rows: readonly WhyArchlucidComparisonRow[];
  /**
   * When false, skips the /demo/preview iframe (jest-axe cannot scan iframes in jsdom).
   * Production page passes true (default).
   */
  showDemoEmbed?: boolean;
};

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
        ships today in V1. This page compares us to common EAM incumbents using the same sourcing rules as{" "}
        <span className="whitespace-nowrap">docs/go-to-market/COMPETITIVE_LANDSCAPE.md</span>.
      </p>

      <section className="mt-8 rounded-lg border border-neutral-200 bg-neutral-50/80 p-4 dark:border-neutral-800 dark:bg-neutral-900/40">
        <h2 className="text-base font-semibold text-neutral-900 dark:text-neutral-50">Side-by-side proof pack</h2>
        <p className="mt-2 max-w-3xl text-sm leading-relaxed text-neutral-700 dark:text-neutral-300">
          Download a single PDF that bundles the same deterministic demo preview as <code>/demo/preview</code> (manifest
          excerpt, explanation, citations, timeline) plus a sourced incumbent scaffold. Requires demo mode on the API
          host (otherwise the link returns 404 by design).
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
          ArchLucid vs. EAM incumbents (summary)
        </h2>
        <p className="mt-2 max-w-3xl text-sm text-neutral-600 dark:text-neutral-400">
          Competitor columns paraphrase the matrix in {WHY_ARCHLUCID_COMPETITOR_LANDSCAPE_CITATION}. The ArchLucid column
          cites repository evidence only — no roadmap claims.
        </p>

        <div className="mt-4 overflow-x-auto rounded-lg border border-neutral-200 dark:border-neutral-800">
          <table className="w-full min-w-[640px] border-collapse text-left text-sm">
            <caption className="border-b border-neutral-200 bg-neutral-100 px-3 py-2 text-left text-neutral-800 dark:border-neutral-700 dark:bg-neutral-900 dark:text-neutral-200">
              Capability comparison — LeanIX, Ardoq, MEGA HOPEX vs. ArchLucid (V1)
            </caption>
            <thead>
              <tr className="border-b border-neutral-200 bg-neutral-50 dark:border-neutral-800 dark:bg-neutral-900/80">
                <th scope="col" className="px-3 py-2 font-semibold text-neutral-900 dark:text-neutral-100">
                  Dimension
                </th>
                <th scope="col" className="px-3 py-2 font-semibold text-neutral-900 dark:text-neutral-100">
                  LeanIX (SAP)
                </th>
                <th scope="col" className="px-3 py-2 font-semibold text-neutral-900 dark:text-neutral-100">
                  Ardoq
                </th>
                <th scope="col" className="px-3 py-2 font-semibold text-neutral-900 dark:text-neutral-100">
                  MEGA HOPEX
                </th>
                <th scope="col" className="px-3 py-2 font-semibold text-neutral-900 dark:text-neutral-100">
                  ArchLucid (V1)
                </th>
              </tr>
            </thead>
            <tbody>
              {rows.map((row) => (
                <tr
                  key={row.dimension}
                  className="border-b border-neutral-100 odd:bg-white even:bg-neutral-50/80 dark:border-neutral-800 dark:odd:bg-neutral-950 dark:even:bg-neutral-900/40"
                >
                  <th
                    scope="row"
                    className="px-3 py-3 align-top font-medium text-neutral-900 dark:text-neutral-100"
                  >
                    {row.dimension}
                  </th>
                  <td className="px-3 py-3 align-top text-neutral-700 dark:text-neutral-300">{row.leanix}</td>
                  <td className="px-3 py-3 align-top text-neutral-700 dark:text-neutral-300">{row.ardoq}</td>
                  <td className="px-3 py-3 align-top text-neutral-700 dark:text-neutral-300">{row.megaHopex}</td>
                  <td className="px-3 py-3 align-top text-neutral-800 dark:text-neutral-200">
                    <p className="m-0 leading-relaxed">{row.archlucid}</p>
                    <p className="mt-2 text-xs leading-snug text-neutral-500 dark:text-neutral-500">
                      <span className="font-medium text-neutral-600 dark:text-neutral-400">Proof: </span>
                      <cite className="not-italic">{row.archlucidCitation}</cite>
                    </p>
                    <p className="mt-2 text-xs leading-snug text-neutral-500 dark:text-neutral-500">
                      <span className="font-medium text-neutral-600 dark:text-neutral-400">Evidence: </span>
                      <span className="not-italic">{row.evidenceAnchor}</span>
                    </p>
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
