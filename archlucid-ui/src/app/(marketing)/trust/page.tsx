import type { Metadata } from "next";
import Link from "next/link";
import type { ReactNode } from "react";

import { MarketingAccessibilityMarkdownFragment } from "@/components/marketing/MarketingAccessibilityMarkdownFragment";
import {
  parseTrustCenterLastReviewedUtc,
  readTrustCenterMarkdown,
  TRUST_CENTER_BLOB_GITHUB_URL,
  TRUST_CENTER_RAW_GITHUB_URL,
} from "@/lib/trust-center-marketing";

export const metadata: Metadata = {
  title: "Trust Center",
  description:
    "ArchLucid security posture, self-assessed controls, scheduled assurance, and procurement artifacts — one place for GRC reviewers.",
};

function stripLeadingTitleAndConstantForBody(markdown: string): string {
  const lines = markdown.replace(/\r\n/g, "\n").split("\n");
  let i = 0;

  while (i < lines.length && (lines[i]?.trim().length === 0 || lines[i]?.trimStart().startsWith(">"))) {
    i++;
  }

  if (i < lines.length && lines[i]?.startsWith("# ")) {
    i++;
  }

  while (i < lines.length && lines[i]?.trim().length === 0) {
    i++;
  }

  if (i < lines.length && /<!--\s*TRUST_CENTER_LAST_REVIEWED_UTC:/.test(lines[i] ?? "")) {
    i++;
  }

  while (i < lines.length && lines[i]?.trim().length === 0) {
    i++;
  }

  if (i < lines.length && lines[i]?.startsWith("**Last reviewed")) {
    i++;
  }

  while (i < lines.length && lines[i]?.trim().length === 0) {
    i++;
  }

  if (i < lines.length && lines[i]?.trim() === "---") {
    i++;
  }

  while (i < lines.length && lines[i]?.trim().length === 0) {
    i++;
  }

  return lines.slice(i).join("\n").trim();
}

export default function MarketingTrustCenterPage(): ReactNode {
  let markdown: string;
  let lastReviewed: string | null = null;

  try {
    markdown = readTrustCenterMarkdown();
    lastReviewed = parseTrustCenterLastReviewedUtc(markdown);
  } catch {
    markdown = "";
  }

  const bodyMarkdown = markdown.length > 0 ? stripLeadingTitleAndConstantForBody(markdown) : "";

  return (
    <main id="main-content" className="mx-auto max-w-3xl px-4 py-10" tabIndex={-1}>
      <h1 className="text-3xl font-bold tracking-tight text-neutral-900 dark:text-neutral-50">Trust Center</h1>
      <p className="mt-2 text-sm text-neutral-600 dark:text-neutral-400">
        {lastReviewed
          ? `Last reviewed (UTC): ${lastReviewed} — source markdown is maintained in the ArchLucid repository.`
          : "Security and procurement posture — source markdown is maintained in the ArchLucid repository."}
      </p>
      <p className="mt-3 text-sm text-neutral-700 dark:text-neutral-300">
        This page is <strong>not</strong> a claim of SOC 2 compliance or completed third-party penetration testing. It
        consolidates <strong>self-assessed</strong> documentation, <strong>V1.1-scheduled</strong> assurance, and{" "}
        <strong>in-flight engagements</strong> with links your security team can trace in your procurement workflow.
      </p>

      <div className="mt-6">
        <a
          className="inline-flex items-center gap-2 rounded-md bg-blue-700 px-4 py-2 text-sm font-semibold text-white shadow-sm transition-colors hover:bg-blue-800 focus:outline-none focus-visible:ring-2 focus-visible:ring-blue-500 focus-visible:ring-offset-2 dark:bg-blue-600 dark:hover:bg-blue-700"
          data-testid="trust-center-evidence-pack-download"
          download
          href="/v1/marketing/trust-center/evidence-pack.zip"
          rel="noopener"
        >
          Download evidence pack (ZIP)
        </a>
        <p className="mt-2 text-xs text-neutral-600 dark:text-neutral-400">
          One file: DPA template, subprocessors, SLA summary, <code>security.txt</code>, CAIQ Lite, SIG Core, owner
          security self-assessment, 2026-Q2 pen-test SoW, and the audit coverage matrix. Anonymous; cached 1 hour with a
          content-driven ETag.
        </p>
      </div>

      {bodyMarkdown.length > 0 ? (
        <div className="mt-8">
          <MarketingAccessibilityMarkdownFragment
            markdownBody={bodyMarkdown}
            tableCaption="ArchLucid trust center posture and artifacts"
          />
        </div>
      ) : (
        <TrustCenterFallbackTable />
      )}

      <footer className="mt-12 border-t border-neutral-200 pt-6 text-sm text-neutral-600 dark:border-neutral-800 dark:text-neutral-400">
        <p className="m-0 font-semibold text-neutral-800 dark:text-neutral-200">Source documents</p>
        <ul className="mt-2 list-disc space-y-1 pl-5">
          <li>
            <Link
              className="text-blue-700 underline underline-offset-2 hover:text-blue-900 dark:text-blue-300 dark:hover:text-blue-200"
              href={TRUST_CENTER_BLOB_GITHUB_URL}
              rel="noopener noreferrer"
              target="_blank"
            >
              Trust Center documentation on GitHub (browse)
            </Link>
          </li>
          <li>
            <Link
              className="text-blue-700 underline underline-offset-2 hover:text-blue-900 dark:text-blue-300 dark:hover:text-blue-200"
              href={TRUST_CENTER_RAW_GITHUB_URL}
              rel="noopener noreferrer"
              target="_blank"
            >
              Raw markdown (download / diff-friendly)
            </Link>
          </li>
        </ul>
      </footer>
    </main>
  );
}

/** Minimal static mirror if markdown is unavailable at build/runtime. */
function TrustCenterFallbackTable(): ReactNode {
  return (
    <div className="mt-8 overflow-x-auto rounded-lg border border-neutral-200 dark:border-neutral-800">
      <table className="w-full min-w-[40rem] border-collapse text-left text-sm">
        <caption className="sr-only">Trust center posture summary</caption>
        <thead className="bg-neutral-100 dark:bg-neutral-900/60">
          <tr>
            <th className="border-b border-neutral-200 px-3 py-2 font-semibold dark:border-neutral-800" scope="col">
              Control
            </th>
            <th className="border-b border-neutral-200 px-3 py-2 font-semibold dark:border-neutral-800" scope="col">
              Status
            </th>
            <th className="border-b border-neutral-200 px-3 py-2 font-semibold dark:border-neutral-800" scope="col">
              Evidence
            </th>
            <th className="border-b border-neutral-200 px-3 py-2 font-semibold dark:border-neutral-800" scope="col">
              Last reviewed
            </th>
          </tr>
        </thead>
        <tbody>
          <tr className="odd:bg-white even:bg-neutral-50 dark:odd:bg-neutral-950 dark:even:bg-neutral-900/60">
            <td className="border border-neutral-200 px-3 py-2 dark:border-neutral-800">SOC 2 self-assessment</td>
            <td className="border border-neutral-200 px-3 py-2 dark:border-neutral-800">Self-asserted</td>
            <td className="border border-neutral-200 px-3 py-2 dark:border-neutral-800">Security self-assessment pack</td>
            <td className="border border-neutral-200 px-3 py-2 dark:border-neutral-800">2026-04-24</td>
          </tr>
          <tr className="odd:bg-white even:bg-neutral-50 dark:odd:bg-neutral-950 dark:even:bg-neutral-900/60">
            <td className="border border-neutral-200 px-3 py-2 dark:border-neutral-800">Penetration test programme</td>
            <td className="border border-neutral-200 px-3 py-2 dark:border-neutral-800">V1.1-scheduled</td>
            <td className="border border-neutral-200 px-3 py-2 dark:border-neutral-800">Deferred scope note (repository)</td>
            <td className="border border-neutral-200 px-3 py-2 dark:border-neutral-800">2026-04-24</td>
          </tr>
        </tbody>
      </table>
      <p className="m-0 px-3 py-2 text-xs text-neutral-500 dark:text-neutral-500">
        Markdown source was not found at build time; open the GitHub links in the footer for the full table.
      </p>
    </div>
  );
}
