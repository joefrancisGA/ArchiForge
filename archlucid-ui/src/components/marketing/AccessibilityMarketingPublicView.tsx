import Link from "next/link";
import type { ReactNode } from "react";

import { MarketingAccessibilityContentSection } from "@/components/marketing/MarketingAccessibilityContentSection";

type AccessibilityMarketingPublicViewProps = {
  sections: ReadonlyMap<string, string>;
  lastReviewedLine: string | null;
};

function sectionId(title: string): string {
  return `a11y-${title
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, "-")
    .replace(/^-|-$/g, "")}`;
}

/**
 * Public WCAG 2.1 AA self-attestation (marketing). Content bodies are sourced from root `ACCESSIBILITY.md`.
 */
export function AccessibilityMarketingPublicView(props: AccessibilityMarketingPublicViewProps): ReactNode {
  const target = props.sections.get("Target compliance level");
  const current = props.sections.get("Current status");
  const tooling = props.sections.get("Tooling");
  const controls = props.sections.get("Existing accessibility controls");
  const exemptions = props.sections.get("Known exemptions");
  const cadence = props.sections.get("Review cadence");

  if (
    target === undefined ||
    current === undefined ||
    tooling === undefined ||
    controls === undefined ||
    exemptions === undefined ||
    cadence === undefined
  ) {
    throw new Error("AccessibilityMarketingPublicView requires all synced ACCESSIBILITY.md sections.");
  }

  return (
    <main id="main-content" className="mx-auto max-w-3xl px-4 py-10" tabIndex={-1}>
      <h1 className="text-3xl font-bold tracking-tight text-neutral-900 dark:text-neutral-50">Accessibility</h1>
      <p className="mt-3 text-sm text-neutral-600 dark:text-neutral-400">
        WCAG 2.1 Level AA — public self-attestation for the ArchLucid marketing site and product posture (no formal VPAT).
      </p>

      <MarketingAccessibilityContentSection
        id={sectionId("Target compliance level")}
        title="Target compliance level"
        markdownBody={target}
        tableCaption="Target compliance summary"
      />

      <MarketingAccessibilityContentSection
        id={sectionId("Current status")}
        title="Current status"
        markdownBody={current}
        tableCaption="Operator pages covered by automated accessibility checks"
      />

      <section aria-labelledby="a11y-tooling-scope" className="scroll-mt-24">
        <h2 id="a11y-tooling-scope" className="mt-10 text-xl font-semibold tracking-tight text-neutral-900 dark:text-neutral-50">
          Tooling and scope
        </h2>
        <p className="mt-3 text-sm text-neutral-600 dark:text-neutral-400">
          Automated checks use <strong className="font-semibold text-neutral-800 dark:text-neutral-200">axe-core</strong> via{" "}
          <strong className="font-semibold text-neutral-800 dark:text-neutral-200">@axe-core/playwright</strong> and static analysis via{" "}
          <strong className="font-semibold text-neutral-800 dark:text-neutral-200">eslint-plugin-jsx-a11y</strong> as described in the policy
          tables below.
        </p>

        <MarketingAccessibilityContentSection
          id={sectionId("Tooling")}
          title="Tooling"
          markdownBody={tooling}
          tableCaption="Accessibility tooling and CI scope"
          headingLevel={3}
        />

        <MarketingAccessibilityContentSection
          id={sectionId("Existing accessibility controls")}
          title="Existing accessibility controls"
          markdownBody={controls}
          tableCaption="Product accessibility controls"
          headingLevel={3}
        />
      </section>

      <MarketingAccessibilityContentSection
        id={sectionId("Known exemptions")}
        title="Known exemptions"
        markdownBody={exemptions}
        tableCaption="Known accessibility exemptions"
      />

      <section aria-labelledby="a11y-vpat" className="scroll-mt-24">
        <h2 id="a11y-vpat" className="mt-10 text-xl font-semibold tracking-tight text-neutral-900 dark:text-neutral-50">
          VPAT
        </h2>
        <p className="mt-3 leading-relaxed text-neutral-800 dark:text-neutral-200">
          ArchLucid does not publish a formal <span className="font-semibold">VPAT</span> or third-party accessibility conformance report for
          download. This page is a <span className="font-semibold">WCAG 2.1 AA self-attestation</span> only, aligned with the same statements in
          the repository policy file.
        </p>
      </section>

      <MarketingAccessibilityContentSection
        id={sectionId("Review cadence")}
        title="Review cadence"
        markdownBody={cadence}
        tableCaption="Accessibility review cadence"
      />

      <section aria-labelledby="a11y-report" className="scroll-mt-24">
        <h2 id="a11y-report" className="mt-10 text-xl font-semibold tracking-tight text-neutral-900 dark:text-neutral-50">
          Reporting an accessibility barrier
        </h2>
        <p className="mt-3 leading-relaxed text-neutral-800 dark:text-neutral-200">
          Email{" "}
          <Link
            className="text-blue-700 underline underline-offset-2 hover:text-blue-900 dark:text-blue-300 dark:hover:text-blue-200"
            href="mailto:accessibility@archlucid.net"
          >
            accessibility@archlucid.net
          </Link>{" "}
          with the page, assistive technology (if any), and what you expected versus what happened. This alias routes to the same operational
          custodian as <span className="font-semibold">security@archlucid.net</span>; triage distinguishes accessibility follow-up from
          coordinated security disclosure.
        </p>
      </section>

      <footer className="mt-12 border-t border-neutral-200 pt-6 text-sm text-neutral-600 dark:border-neutral-800 dark:text-neutral-400">
        <p>
          {props.lastReviewedLine ?? "Last reviewed: (missing — update ACCESSIBILITY.md)"} — review cadence:{" "}
          <span className="font-medium text-neutral-800 dark:text-neutral-200">annually</span>.
        </p>
      </footer>
    </main>
  );
}
