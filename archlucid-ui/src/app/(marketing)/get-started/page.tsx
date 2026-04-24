import type { Metadata } from "next";
import Image from "next/image";
import Link from "next/link";
import type { ReactNode } from "react";

import { BUYER_GET_STARTED_VERTICAL_SLUGS } from "./get-started-verticals";
import { BRAND_CATEGORY, BRAND_CATEGORY_LEGACY } from "@/lib/brand-category";

export const metadata: Metadata = {
  title: "Get started · ArchLucid",
  description: `Sign in, pick a vertical, run a sample, read your first finding — the first thirty minutes with ArchLucid (${BRAND_CATEGORY}), hosted, no install.`,
  robots: { index: true, follow: true },
  other: {
    "x-archlucid-brand-category-legacy": BRAND_CATEGORY_LEGACY,
  },
};

const PLACEHOLDER_MARKER = "<<placeholder copy — replace before external use>>";

type Step = {
  readonly n: number;
  readonly title: string;
  readonly body: string;
};

const STEPS: readonly Step[] = [
  {
    n: 1,
    title: "Sign in",
    body: `Sign in at archlucid.com with your work identity (Microsoft Entra ID or a Google Workspace account). ${PLACEHOLDER_MARKER}`,
  },
  {
    n: 2,
    title: "Pick a vertical",
    body: `Choose the industry profile closest to the system you want to evaluate. The defaults match the briefs shipped in templates/briefs/ — pick the closest match; you can change it after the first run. ${PLACEHOLDER_MARKER}`,
  },
  {
    n: 3,
    title: "Run a sample",
    body: `ArchLucid pre-populates a sample architecture request shaped for the vertical you picked, then runs the analysis pipeline against it. No upload required for the first run. ${PLACEHOLDER_MARKER}`,
  },
  {
    n: 4,
    title: "Read your first finding",
    body: `Open the committed run and read the first typed finding — what was flagged, why it was flagged, and which evidence backs it. This is the smallest unit of value the product produces. ${PLACEHOLDER_MARKER}`,
  },
  {
    n: 5,
    title: "Decide what to do next",
    body: `Either invite a colleague and run a second sample, or hand off to a guided pilot. ${PLACEHOLDER_MARKER}`,
  },
] as const;

export default function GetStartedPage(): ReactNode {
  return (
    <main className="mx-auto max-w-3xl px-4 py-10">
      <h1 className="text-2xl font-semibold tracking-tight text-neutral-900 dark:text-neutral-50">
        Your first 30 minutes with ArchLucid
      </h1>
      <p
        className="mt-2 text-sm leading-relaxed text-neutral-700 dark:text-neutral-300"
        data-testid="get-started-brand-category-paragraph"
      >
        ArchLucid is an {BRAND_CATEGORY} product — this page walks through signup, vertical selection, and your first
        sample run.
      </p>
      <p className="mt-2 text-sm text-neutral-600 dark:text-neutral-400">
        ArchLucid is a SaaS product. Nothing on this page asks you to install Docker, SQL Server, .NET, Node, Terraform,
        or a CLI.
      </p>
      <p className="mt-2 text-sm text-neutral-600 dark:text-neutral-400">
        Five steps. Roughly thirty minutes end-to-end on a normal connection.
      </p>

      <section aria-labelledby="vertical-picker-heading" className="mt-8" data-testid="get-started-vertical-picker">
        <h2 id="vertical-picker-heading" className="text-lg font-semibold text-neutral-900 dark:text-neutral-50">
          Pick a vertical to start
        </h2>
        <p className="mt-1 text-xs text-neutral-500 dark:text-neutral-400">
          Defaults mirror the existing briefs in templates/briefs/.
        </p>
        <ul className="mt-3 grid grid-cols-2 gap-2 sm:grid-cols-3" role="list">
          {BUYER_GET_STARTED_VERTICAL_SLUGS.map((slug) => (
            <li key={slug}>
              <button
                type="button"
                data-testid={`get-started-vertical-${slug}`}
                data-vertical-slug={slug}
                className="w-full rounded-md border border-neutral-200 bg-white px-3 py-2 text-left text-sm text-neutral-900 hover:border-teal-400 dark:border-neutral-800 dark:bg-neutral-950 dark:text-neutral-50"
              >
                {slug}
              </button>
            </li>
          ))}
        </ul>
      </section>

      <ol className="mt-10 space-y-8" data-testid="get-started-steps">
        {STEPS.map((step) => (
          <li key={step.n} data-testid={`get-started-step-${step.n}`} className="flex gap-4">
            <div className="flex-shrink-0">
              <Image
                src={`/get-started/step-${step.n}-placeholder.png`}
                alt=""
                width={160}
                height={100}
                data-testid={`get-started-step-${step.n}-image`}
                className="rounded-md border border-dashed border-neutral-300 bg-neutral-100 dark:border-neutral-700 dark:bg-neutral-900"
              />
            </div>
            <div>
              <h3 className="text-base font-semibold text-neutral-900 dark:text-neutral-50">
                {step.n}. {step.title}
              </h3>
              <p className="mt-1 text-sm text-neutral-700 dark:text-neutral-300">{step.body}</p>
            </div>
          </li>
        ))}
      </ol>

      <section aria-labelledby="next-heading" className="mt-12 border-t border-neutral-200 pt-6 dark:border-neutral-800">
        <h2 id="next-heading" className="text-lg font-semibold text-neutral-900 dark:text-neutral-50">
          Where to go next
        </h2>
        <ul className="mt-3 list-disc space-y-1 pl-5 text-sm text-neutral-700 dark:text-neutral-300">
          <li>
            For the operator path after the sample run, see{" "}
            <Link className="text-teal-700 underline underline-offset-2 dark:text-teal-300" href="/pricing">
              pricing
            </Link>{" "}
            or talk to your account team via the Request a quote button on the pricing page.
          </li>
          <li>
            For the sponsor-facing narrative, see the executive sponsor brief in the public repository at
            docs/EXECUTIVE_SPONSOR_BRIEF.md.
          </li>
        </ul>
      </section>
    </main>
  );
}
