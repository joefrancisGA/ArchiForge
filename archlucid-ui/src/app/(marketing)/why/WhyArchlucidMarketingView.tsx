import Link from "next/link";
import type { ReactNode } from "react";

import { BRAND_CATEGORY } from "@/lib/brand-category";
import { WHY_MARKET_LANDSCAPE_MARKETING_ROWS } from "@/lib/why-market-landscape-comparison";
import { type WhyVerifyLink, WHY_COMPARISON_VERIFY_LINK_ROWS } from "@/lib/why-comparison-verify-points";
import { type WhyHardComparisonRow, whyHardCellDisplay } from "@/lib/why-comparison";

function renderWhyVerifyLink(link: WhyVerifyLink): ReactNode {
  const className =
    "break-words text-sky-700 underline underline-offset-2 hover:text-sky-600 dark:text-sky-400";

  const key = `${link.href}|${link.label}`;

  if (link.href.startsWith("http")) {
    return (
      <a key={key} className={className} href={link.href} target="_blank" rel="noopener noreferrer">
        {link.label}
      </a>
    );
  }

  if (link.href.endsWith(".zip")) {
    return (
      <a key={key} className={className} href={link.href} download>
        {link.label}
      </a>
    );
  }

  return (
    <Link key={key} className={className} href={link.href}>
      {link.label}
    </Link>
  );
}

function WhyHardComparisonVerifyCell({ links }: { readonly links: readonly WhyVerifyLink[] }): ReactNode {
  return (
    <div className="flex max-w-[14rem] flex-col gap-1 align-top text-xs leading-snug">{links.map(renderWhyVerifyLink)}</div>
  );
}

export type WhyArchlucidMarketingViewProps = {
  /** Parsed from `WHY_COMPARISON_ROWS_SERIALIZED` on the marketing route for a single JSON source path. */
  frontDoorRows: readonly WhyHardComparisonRow[];
  /**
   * When false, skips the /demo/preview iframe (jest-axe cannot scan iframes in jsdom).
   * Production page passes true (default).
   */
  showDemoEmbed?: boolean;
};

/**
 * Public “Why ArchLucid” differentiation page — no operator auth.
 */
export function WhyArchlucidMarketingView({ frontDoorRows, showDemoEmbed = true }: WhyArchlucidMarketingViewProps) {
  return (
    <main className="mx-auto max-w-5xl px-4 py-10">
      <h1 className="text-3xl font-semibold tracking-tight text-neutral-900 dark:text-neutral-50">
        Why ArchLucid
      </h1>
      <p
        className="mt-3 max-w-3xl text-sm leading-relaxed text-neutral-700 dark:text-neutral-300"
        data-testid="why-brand-category-paragraph"
      >
        ArchLucid is an {BRAND_CATEGORY} platform: specialized agents analyze architecture requests, produce
        explainable findings, and feed governance workflows with a durable audit trail. The comparison table below is
        claim-by-claim, with symbol-only scores in the product columns. The downloadable proof pack bundles a deeper,
        citation-backed narrative for teams who need to compare vendors on paper.
      </p>

      <section className="mt-10 rounded-xl border border-sky-200 bg-gradient-to-br from-white via-white to-sky-50 px-6 py-6 shadow-sm dark:border-sky-900/70 dark:from-neutral-950 dark:via-neutral-950 dark:to-sky-950/40">
        <p className="text-xs font-semibold uppercase tracking-[0.2em] text-sky-800 dark:text-sky-300">
          First-principles outcome
        </p>
        <h2 id="why-hero-outcome-heading" className="mt-2 text-2xl font-semibold tracking-tight text-neutral-900 dark:text-neutral-50">
          Governed AI architecture reviews—not ad-hoc chat
        </h2>
        <p className="mt-3 max-w-3xl text-sm leading-relaxed text-neutral-700 dark:text-neutral-300">
          ArchLucid runs a structured multi-agent Authority pipeline against every architecture request so findings,
          comparisons, manifests, and audit exports share one proven trace. Buyers still compare incumbent stack tools; the
          adjacent-category table on this page summarizes the same buyer-safe landscape claims captured in{" "}
          <code className="rounded bg-neutral-100 px-1 py-0.5 dark:bg-neutral-900/60">docs/go-to-market/COMPETITIVE_LANDSCAPE.md</code>{" "}
          §2.3—not a substitute for the symbol-scored hard-comparison grid further down.
        </p>
      </section>

      <section className="mt-12" aria-labelledby="why-market-landscape-heading">
        <h2 id="why-market-landscape-heading" className="text-xl font-semibold text-neutral-900 dark:text-neutral-50">
          Adjacent-category landscape (qualitative · three rows · five lenses)
        </h2>
        <p className="mt-2 max-w-3xl text-xs leading-snug text-neutral-600 dark:text-neutral-400">
          Summarized wording only — same sources as{" "}
          <code className="rounded bg-neutral-100 px-1 py-0.5 dark:bg-neutral-800">docs/go-to-market/COMPETITIVE_LANDSCAPE.md</code>{" "}
          §2.3; citations and benchmark detail live in that document and in the deterministic proof artefacts linked from
          this page.
        </p>
        <div className="mt-4 overflow-x-auto rounded-lg border border-neutral-200 dark:border-neutral-800">
          <table
            data-testid="why-market-landscape-mini-table"
            className="w-full min-w-[60rem] border-collapse text-left text-sm"
          >
            <thead>
              <tr className="border-b border-neutral-200 bg-neutral-50 dark:border-neutral-800 dark:bg-neutral-900/80">
                <th scope="col" className="px-3 py-2 font-semibold text-neutral-900 dark:text-neutral-100">
                  Dimension
                </th>
                <th scope="col" className="min-w-[12rem] px-3 py-2 font-semibold text-neutral-900 dark:text-neutral-100">
                  ArchLucid
                </th>
                <th scope="col" className="min-w-[12rem] px-3 py-2 font-semibold text-neutral-900 dark:text-neutral-100">
                  GitHub Copilot (architecture ad-hoc)
                </th>
                <th scope="col" className="min-w-[12rem] px-3 py-2 font-semibold text-neutral-900 dark:text-neutral-100">
                  ChatGPT / Claude (manual prompting)
                </th>
                <th scope="col" className="min-w-[12rem] px-3 py-2 font-semibold text-neutral-900 dark:text-neutral-100">
                  Structurizr (+ AI assist)
                </th>
              </tr>
            </thead>
            <tbody>
              {WHY_MARKET_LANDSCAPE_MARKETING_ROWS.map((row) => (
                <tr
                  key={row.dimension}
                  className="border-b border-neutral-100 odd:bg-white even:bg-neutral-50/80 dark:border-neutral-800 dark:odd:bg-neutral-950 dark:even:bg-neutral-900/40"
                >
                  <th
                    scope="row"
                    className="max-w-[200px] px-3 py-3 align-top font-medium text-neutral-900 dark:text-neutral-100"
                  >
                    {row.dimension}
                  </th>
                  <td className="px-3 py-3 align-top text-neutral-800 dark:text-neutral-200">{row.archlucid}</td>
                  <td className="px-3 py-3 align-top text-neutral-800 dark:text-neutral-200">
                    {row.githubCopilotAdHocArchitecture}
                  </td>
                  <td className="px-3 py-3 align-top text-neutral-800 dark:text-neutral-200">
                    {row.manualChatgptClaude}
                  </td>
                  <td className="px-3 py-3 align-top text-neutral-800 dark:text-neutral-200">
                    {row.structurizrWithAssist}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </section>

      <section className="mt-8 rounded-lg border border-neutral-200 bg-neutral-50/80 p-4 dark:border-neutral-800 dark:bg-neutral-900/40">
        <h2 className="text-base font-semibold text-neutral-900 dark:text-neutral-50">Side-by-side proof pack</h2>
        <p className="mt-2 max-w-3xl text-sm leading-relaxed text-neutral-700 dark:text-neutral-300">
          Download a single PDF that bundles the same deterministic demo preview as <code>/demo/preview</code> (manifest
          excerpt, explanation, citations, timeline) plus the benchmarked differentiation narrative table (five
          detailed rows with citations — not the symbol-only front-door grid below). Requires demo mode on the API host
          (otherwise the link returns 404 by design).
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
          See a real finalized-manifest page
        </h2>
        <p className="mt-2 max-w-3xl text-sm leading-relaxed text-neutral-600 dark:text-neutral-400">
          Anonymous read-only preview of the latest finalized demo-seed run (cached JSON + marketing UI). Same shape as
          the operator finalize experience — no sign-in.
        </p>
        {showDemoEmbed ? (
          <div className="mt-4 overflow-hidden rounded-lg border border-neutral-200 bg-white shadow-sm dark:border-neutral-800 dark:bg-neutral-900">
            <iframe
              title="ArchLucid demo manifest page preview"
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

      <section className="mt-12" aria-labelledby="why-hard-compare-heading">
        <h2 id="why-hard-compare-heading" className="text-xl font-semibold text-neutral-900 dark:text-neutral-50">
          Hard comparison (front-door)
        </h2>
        <p className="mt-2 max-w-3xl text-sm text-neutral-600 dark:text-neutral-400">
          Symbols only in the product columns (✓ / partial / —). Each row is labeled the same way across public
          collateral so reviewers can trace claims to proof links.
        </p>

        <div className="mt-4 overflow-x-auto rounded-lg border border-neutral-200 dark:border-neutral-800">
          <table className="w-full min-w-[72rem] border-collapse text-left text-sm" data-testid="why-hard-comparison-table">
            <caption className="border-b border-neutral-200 bg-neutral-100 px-3 py-2 text-left text-neutral-800 dark:border-neutral-700 dark:bg-neutral-900 dark:text-neutral-200">
              ArchLucid vs common stacks — technically scoped rows
            </caption>
            <thead>
              <tr className="border-b border-neutral-200 bg-neutral-50 dark:border-neutral-800 dark:bg-neutral-900/80">
                <th scope="col" className="min-w-[220px] px-3 py-2 font-semibold text-neutral-900 dark:text-neutral-100">
                  Claim
                </th>
                <th scope="col" className="min-w-[140px] px-3 py-2 font-semibold text-neutral-900 dark:text-neutral-100">
                  Verify
                </th>
                <th scope="col" className="px-3 py-2 font-semibold text-neutral-900 dark:text-neutral-100">
                  ArchLucid
                </th>
                <th scope="col" className="min-w-[120px] px-3 py-2 font-semibold text-neutral-900 dark:text-neutral-100">
                  draw.io+Confluence
                </th>
                <th scope="col" className="min-w-[140px] px-3 py-2 font-semibold text-neutral-900 dark:text-neutral-100">
                  GitHub Copilot for generic IaC review
                </th>
                <th scope="col" className="min-w-[120px] px-3 py-2 font-semibold text-neutral-900 dark:text-neutral-100">
                  Generic AI architect tool
                </th>
              </tr>
            </thead>
            <tbody>
              {frontDoorRows.map((row, index) => (
                <tr
                  key={`why-hard-row-${index}`}
                  className="border-b border-neutral-100 odd:bg-white even:bg-neutral-50/80 dark:border-neutral-800 dark:odd:bg-neutral-950 dark:even:bg-neutral-900/40"
                >
                  <th
                    scope="row"
                    className="max-w-[320px] px-3 py-3 align-top font-medium text-neutral-900 dark:text-neutral-100"
                  >
                    {row.label}
                  </th>
                  <td className="px-3 py-3 align-top text-neutral-800 dark:text-neutral-200">
                    <WhyHardComparisonVerifyCell links={WHY_COMPARISON_VERIFY_LINK_ROWS[index] ?? []} />
                  </td>
                  <td className="whitespace-nowrap px-3 py-3 align-top text-neutral-800 dark:text-neutral-200">
                    {whyHardCellDisplay(row.archlucid)}
                  </td>
                  <td className="whitespace-nowrap px-3 py-3 align-top text-neutral-800 dark:text-neutral-200">
                    {whyHardCellDisplay(row.drawioConfluence)}
                  </td>
                  <td className="whitespace-nowrap px-3 py-3 align-top text-neutral-800 dark:text-neutral-200">
                    {whyHardCellDisplay(row.githubCopilotIac)}
                  </td>
                  <td className="whitespace-nowrap px-3 py-3 align-top text-neutral-800 dark:text-neutral-200">
                    {whyHardCellDisplay(row.genericAiArchitect)}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </section>

      <section className="mt-10 border-t border-neutral-200 pt-8 dark:border-neutral-800">
        <p className="text-sm text-neutral-600 dark:text-neutral-400">
          For sponsor-ready language and procurement context, see the{" "}
          <Link className="text-sky-700 underline underline-offset-2 dark:text-sky-400" href="/get-started">
            getting started guide
          </Link>{" "}
          or ask your account team for the executive overview pack.
        </p>
      </section>
    </main>
  );
}
