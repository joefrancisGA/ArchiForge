import Link from "next/link";

import { LayerHeader } from "@/components/LayerHeader";

const DOCS_REPO_BASE =
  process.env.NEXT_PUBLIC_ARCHLUCID_DOCS_REPO_BASE ??
  "https://github.com/joefrancisGA/ArchLucid/blob/main";

/**
 * Operator trust and security home (signed-in shell). Procurement-oriented strip plus NDA-gated pen-test posture.
 * Public engagement table lives at <c>/security-trust</c> (marketing) — this route is <c>/workspace/security-trust</c> so
 * App Router does not collide with the parallel marketing page.
 */
export default function OperatorSecurityTrustPage() {
  return (
    <div className="space-y-6">
      <LayerHeader pageKey="security-trust" />

      <section
        aria-label="NDA-gated third-party security assessments"
        className="rounded-lg border border-sky-200 bg-sky-50/80 px-4 py-3 dark:border-sky-900 dark:bg-sky-950/40"
      >
        <p className="m-0 text-sm font-semibold text-sky-950 dark:text-sky-100">Third-party pen-test summaries</p>
        <p className="m-0 mt-2 text-sm text-sky-950/90 dark:text-sky-100/90">
          Pen-test redacted summaries are available <strong>under NDA only</strong>. The public{" "}
          <Link
            className="font-medium text-sky-800 underline underline-offset-2 hover:text-sky-950 dark:text-sky-300 dark:hover:text-sky-100"
            href={`${DOCS_REPO_BASE}/docs/go-to-market/TRUST_CENTER.md`}
            target="_blank"
            rel="noopener noreferrer"
          >
            Trust Center
          </Link>{" "}
          records engagement existence and high-level posture. To request the most recent redacted summary, email{" "}
          <a
            className="font-medium text-sky-800 underline underline-offset-2 hover:text-sky-950 dark:text-sky-300 dark:hover:text-sky-100"
            href="mailto:security@archlucid.com"
          >
            security@archlucid.com
          </a>
          . (Owner decision 2026-04-22 — see{" "}
          <Link
            className="font-medium text-sky-800 underline underline-offset-2 hover:text-sky-950 dark:text-sky-300 dark:hover:text-sky-100"
            href={`${DOCS_REPO_BASE}/docs/PENDING_QUESTIONS.md`}
            target="_blank"
            rel="noopener noreferrer"
          >
            PENDING_QUESTIONS.md
          </Link>{" "}
          item 20.)
        </p>
      </section>

      <section aria-label="Security trust badges legend" className="space-y-2">
        <h2 className="text-lg font-semibold">Badges legend</h2>
        <p className="m-0 text-sm text-neutral-700 dark:text-neutral-300">
          Operator-facing labels for security posture — none imply a <strong>public</strong> pen-test publication.
        </p>
        <div className="overflow-x-auto rounded-lg border border-neutral-200 dark:border-neutral-800">
          <table className="w-full min-w-[28rem] border-collapse text-left text-sm">
            <thead className="bg-neutral-100 dark:bg-neutral-900/60">
              <tr>
                <th className="border-b border-neutral-200 px-3 py-2 font-semibold dark:border-neutral-800">Label</th>
                <th className="border-b border-neutral-200 px-3 py-2 font-semibold dark:border-neutral-800">Meaning</th>
              </tr>
            </thead>
            <tbody>
              <tr>
                <td className="border-b border-neutral-100 px-3 py-2 font-medium dark:border-neutral-800/80">
                  NDA-gated security assessment
                </td>
                <td className="border-b border-neutral-100 px-3 py-2 text-neutral-700 dark:text-neutral-300 dark:border-neutral-800/80">
                  Redacted third-party assessment material is shared under NDA; the marketing site and this page do not
                  host the redacted report body.
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </section>

      <section className="space-y-2">
        <h2 className="text-lg font-semibold">Repository trust center</h2>
        <p className="m-0 text-sm text-neutral-700 dark:text-neutral-300">
          Buyer-facing index (policies, DPA template, subprocessors, SOC 2 self-assessment, CAIQ / SIG): open the{" "}
          <Link
            className="text-sky-700 underline underline-offset-2 hover:text-sky-900 dark:text-sky-400 dark:hover:text-sky-200"
            href={`${DOCS_REPO_BASE}/docs/go-to-market/TRUST_CENTER.md`}
            target="_blank"
            rel="noopener noreferrer"
          >
            Trust Center (markdown)
          </Link>
          .
        </p>
      </section>
    </div>
  );
}
