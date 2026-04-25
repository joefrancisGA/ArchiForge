import Link from "next/link";
import type { ReactNode } from "react";

import {
  SECURITY_TRUST_NDA_NOTICE,
  SECURITY_TRUST_REPO_TRUST_CENTER_URL,
  securityTrustEngagementRows,
  type AssuranceEngagementRow,
} from "@/lib/security-trust-content";

type MarketingSecurityTrustViewProps = {
  rows?: ReadonlyArray<AssuranceEngagementRow>;
};

function renderSummaryAccess(row: AssuranceEngagementRow): ReactNode {
  if (row.summaryAccess.kind === "public" && row.summaryAccess.href) {
    return (
      <Link
        className="text-blue-700 underline underline-offset-2 hover:text-blue-900 dark:text-blue-300 dark:hover:text-blue-200"
        href={row.summaryAccess.href}
      >
        {row.summaryAccess.description}
      </Link>
    );
  }

  return <span>{row.summaryAccess.description}</span>;
}

/**
 * Public marketing view of the Trust Center "Recent assurance activity" table.
 * Engagement metadata only — no redacted findings, no customer names.
 */
export function MarketingSecurityTrustView(
  props: MarketingSecurityTrustViewProps,
): ReactNode {
  const rows = props.rows ?? securityTrustEngagementRows;

  return (
    <main
      id="main-content"
      className="mx-auto max-w-3xl px-4 py-10"
      tabIndex={-1}
    >
      <h1 className="text-3xl font-bold tracking-tight text-neutral-900 dark:text-neutral-50">
        Security &amp; trust
      </h1>
      <p className="mt-3 text-sm text-neutral-600 dark:text-neutral-400">
        Engagement metadata for ArchLucid&rsquo;s most recent assurance activity.
        This page records that an activity occurred, what it covered, and how to
        obtain redacted material under NDA — it does not publish redacted
        findings or customer names. For the consolidated posture table, questionnaires, and procurement links, open the{" "}
        <Link
          className="text-blue-700 underline underline-offset-2 hover:text-blue-900 dark:text-blue-300 dark:hover:text-blue-200"
          href="/trust"
        >
          Trust Center
        </Link>
        .
      </p>

      <section
        aria-label="NDA-gated third-party security assessments"
        className="mt-6 rounded-lg border border-sky-200 bg-sky-50/80 px-4 py-3 dark:border-sky-900 dark:bg-sky-950/40"
      >
        <p className="m-0 text-sm font-semibold text-sky-950 dark:text-sky-100">
          Third-party pen-test summaries are NDA-only
        </p>
        <p className="m-0 mt-2 text-sm text-sky-950/90 dark:text-sky-100/90">
          {SECURITY_TRUST_NDA_NOTICE}
        </p>
      </section>

      <section
        aria-labelledby="security-trust-recent-assurance-activity"
        className="mt-8 scroll-mt-24"
      >
        <h2
          id="security-trust-recent-assurance-activity"
          className="text-xl font-semibold tracking-tight text-neutral-900 dark:text-neutral-50"
        >
          Recent assurance activity
        </h2>
        <p className="mt-3 text-sm text-neutral-700 dark:text-neutral-300">
          Mirrors the equivalent table in the in-repo{" "}
          <Link
            className="text-blue-700 underline underline-offset-2 hover:text-blue-900 dark:text-blue-300 dark:hover:text-blue-200"
            href={SECURITY_TRUST_REPO_TRUST_CENTER_URL}
            target="_blank"
            rel="noopener noreferrer"
          >
            Trust Center
          </Link>
          .
        </p>
        <div className="mt-4 overflow-x-auto rounded-lg border border-neutral-200 dark:border-neutral-800">
          <table
            className="w-full min-w-[40rem] border-collapse text-left text-sm"
            aria-describedby="security-trust-recent-assurance-activity"
          >
            <caption className="sr-only">
              ArchLucid recent assurance engagement metadata
            </caption>
            <thead className="bg-neutral-100 dark:bg-neutral-900/60">
              <tr>
                <th
                  scope="col"
                  className="border-b border-neutral-200 px-3 py-2 font-semibold dark:border-neutral-800"
                >
                  Engagement
                </th>
                <th
                  scope="col"
                  className="border-b border-neutral-200 px-3 py-2 font-semibold dark:border-neutral-800"
                >
                  Vendor
                </th>
                <th
                  scope="col"
                  className="border-b border-neutral-200 px-3 py-2 font-semibold dark:border-neutral-800"
                >
                  Scope
                </th>
                <th
                  scope="col"
                  className="border-b border-neutral-200 px-3 py-2 font-semibold dark:border-neutral-800"
                >
                  Completed (UTC)
                </th>
                <th
                  scope="col"
                  className="border-b border-neutral-200 px-3 py-2 font-semibold dark:border-neutral-800"
                >
                  Summary access
                </th>
              </tr>
            </thead>
            <tbody>
              {rows.map((row) => (
                <tr key={row.id} data-testid={`assurance-row-${row.id}`}>
                  <td className="border-b border-neutral-100 px-3 py-2 font-medium dark:border-neutral-800/80">
                    {row.engagement}
                  </td>
                  <td className="border-b border-neutral-100 px-3 py-2 dark:border-neutral-800/80">
                    {row.vendor}
                  </td>
                  <td className="border-b border-neutral-100 px-3 py-2 text-neutral-700 dark:border-neutral-800/80 dark:text-neutral-300">
                    {row.scope}
                  </td>
                  <td className="border-b border-neutral-100 px-3 py-2 text-neutral-700 dark:border-neutral-800/80 dark:text-neutral-300">
                    {row.completedUtc}
                  </td>
                  <td className="border-b border-neutral-100 px-3 py-2 dark:border-neutral-800/80">
                    {renderSummaryAccess(row)}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </section>

      <footer className="mt-12 border-t border-neutral-200 pt-6 text-sm text-neutral-600 dark:border-neutral-800 dark:text-neutral-400">
        <p>
          Procurement contact: <span className="font-medium text-neutral-800 dark:text-neutral-200">security@archlucid.com</span>
        </p>
      </footer>
    </main>
  );
}
