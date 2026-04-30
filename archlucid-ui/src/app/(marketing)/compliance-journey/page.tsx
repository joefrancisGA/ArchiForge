import type { Metadata } from "next";
import Link from "next/link";

import { DEFAULT_GITHUB_BLOB_BASE } from "@/lib/docs-public-base";

export const metadata: Metadata = {
  title: "Compliance journey",
  description: "Where ArchLucid is today on security and compliance — honest scope, no over-claims.",
};

/** Public compliance posture page — content pointers only; no new certifications claimed. */
export default function ComplianceJourneyPage() {
  return (
    <main className="mx-auto max-w-3xl px-4 py-10">
      <h1 className="mb-2 text-2xl font-semibold text-neutral-900 dark:text-neutral-100">Compliance journey</h1>
      <p className="mb-6 text-sm leading-relaxed text-neutral-700 dark:text-neutral-300">
        ArchLucid is <strong>not SOC 2 attested</strong> today. We publish self-assessment material, questionnaires, and
        engineering controls so buyers can diligence the product without mistaking roadmap for certification. This page
        summarizes what is in scope now — no new certifications are claimed here.
      </p>
      <ul className="list-disc space-y-2 pl-5 text-sm text-neutral-700 dark:text-neutral-300">
        <li>
          <Link className="text-teal-700 underline underline-offset-2 dark:text-teal-300" href="/trust">
            Trust Center
          </Link>{" "}
          — consolidated posture, questionnaires, and procurement downloads.
        </li>
        <li>
          CAIQ Lite and SIG Core pre-fills:{" "}
          <a
            className="text-teal-700 underline underline-offset-2 dark:text-teal-300"
            href={`${DEFAULT_GITHUB_BLOB_BASE}/docs/security/CAIQ_LITE_2026.md`}
            rel="noopener noreferrer"
            target="_blank"
          >
            CAIQ Lite
          </a>
          ,{" "}
          <a
            className="text-teal-700 underline underline-offset-2 dark:text-teal-300"
            href={`${DEFAULT_GITHUB_BLOB_BASE}/docs/security/SIG_CORE_2026.md`}
            rel="noopener noreferrer"
            target="_blank"
          >
            SIG Core
          </a>
          .
        </li>
        <li>
          Control and evidence mapping:{" "}
          <a
            className="text-teal-700 underline underline-offset-2 dark:text-teal-300"
            href={`${DEFAULT_GITHUB_BLOB_BASE}/docs/security/COMPLIANCE_MATRIX.md`}
            rel="noopener noreferrer"
            target="_blank"
          >
            Compliance matrix
          </a>
          .
        </li>
        <li>
          Data processing terms:{" "}
          <a
            className="text-teal-700 underline underline-offset-2 dark:text-teal-300"
            href={`${DEFAULT_GITHUB_BLOB_BASE}/docs/go-to-market/DPA_TEMPLATE.md`}
            rel="noopener noreferrer"
            target="_blank"
          >
            DPA template
          </a>{" "}
          and{" "}
          <a
            className="text-teal-700 underline underline-offset-2 dark:text-teal-300"
            href={`${DEFAULT_GITHUB_BLOB_BASE}/docs/go-to-market/SUBPROCESSORS.md`}
            rel="noopener noreferrer"
            target="_blank"
          >
            subprocessor list
          </a>
          .
        </li>
      </ul>
      <p className="mt-8 text-sm leading-relaxed text-neutral-700 dark:text-neutral-300">
        <span className="font-semibold text-neutral-900 dark:text-neutral-100">Verify:</span> start from the in-product{" "}
        <Link className="text-teal-700 underline underline-offset-2 dark:text-teal-300" href="/trust">
          Trust Center
        </Link>
        , or open the same materials on GitHub:{" "}
        <a
          className="text-teal-700 underline underline-offset-2 dark:text-teal-300"
          href={`${DEFAULT_GITHUB_BLOB_BASE}/docs/go-to-market/TRUST_CENTER.md`}
          rel="noopener noreferrer"
          target="_blank"
        >
          Trust Center pack
        </a>
        ,{" "}
        <a
          className="text-teal-700 underline underline-offset-2 dark:text-teal-300"
          href={`${DEFAULT_GITHUB_BLOB_BASE}/docs/security/COMPLIANCE_MATRIX.md`}
          rel="noopener noreferrer"
          target="_blank"
        >
          compliance matrix
        </a>
        .
      </p>
    </main>
  );
}
