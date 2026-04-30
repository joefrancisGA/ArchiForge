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
        summarizes what is in scope now and points to canonical in-repo documents (no new certifications are claimed here).
      </p>
      <ul className="list-disc space-y-2 pl-5 text-sm text-neutral-700 dark:text-neutral-300">
        <li>
          Trust Center index: <code className="text-[0.85em]">docs/go-to-market/TRUST_CENTER.md</code> in the ArchLucid
          repository.
        </li>
        <li>
          CAIQ Lite + SIG Core pre-fills live under{" "}
          <code className="text-[0.85em]">docs/security/CAIQ_LITE_2026.md</code> and{" "}
          <code className="text-[0.85em]">docs/security/SIG_CORE_2026.md</code> in-repo.
        </li>
        <li>
          Control/evidence mapping: <code className="text-[0.85em]">docs/security/COMPLIANCE_MATRIX.md</code>.
        </li>
        <li>
          DPA template + subprocessors: <code className="text-[0.85em]">docs/go-to-market/DPA_TEMPLATE.md</code>,{" "}
          <code className="text-[0.85em]">docs/go-to-market/SUBPROCESSORS.md</code>.
        </li>
      </ul>
      <p className="mt-8 text-sm leading-relaxed text-neutral-700 dark:text-neutral-300">
        <span className="font-semibold text-neutral-900 dark:text-neutral-100">Verify:</span> open the in-product Trust Center at{" "}
        <Link className="text-teal-700 underline underline-offset-2 dark:text-teal-300" href="/trust">
          /trust
        </Link>{" "}
        (downloads align with the same Markdown paths above). Canonical blobs on{" "}
        <code className="text-[0.85em]">main</code>:{" "}
        <a
          className="text-teal-700 underline underline-offset-2 dark:text-teal-300"
          href={`${DEFAULT_GITHUB_BLOB_BASE}/docs/go-to-market/TRUST_CENTER.md`}
          rel="noopener noreferrer"
          target="_blank"
        >
          TRUST_CENTER.md
        </a>
        ,{" "}
        <a
          className="text-teal-700 underline underline-offset-2 dark:text-teal-300"
          href={`${DEFAULT_GITHUB_BLOB_BASE}/docs/security/COMPLIANCE_MATRIX.md`}
          rel="noopener noreferrer"
          target="_blank"
        >
          COMPLIANCE_MATRIX.md
        </a>
        .
      </p>
    </main>
  );
}
