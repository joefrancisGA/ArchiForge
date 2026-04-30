import type { Metadata } from "next";
import Link from "next/link";
import type { ReactNode } from "react";

import { BUYER_GET_STARTED_VERTICAL_SLUGS, VERTICAL_DISPLAY_NAMES } from "./get-started-verticals";
import { BRAND_CATEGORY, BRAND_CATEGORY_LEGACY } from "@/lib/brand-category";
import { DEFAULT_GITHUB_BLOB_BASE } from "@/lib/docs-public-base";

export const metadata: Metadata = {
  title: "Get started · ArchLucid",
  description: `Sign in, pick a vertical, run a sample, read your first finding — the first thirty minutes with ArchLucid (${BRAND_CATEGORY}), hosted, no install.`,
  robots: { index: true, follow: true },
  other: {
    "x-archlucid-brand-category-legacy": BRAND_CATEGORY_LEGACY,
  },
};

type Step = {
  readonly n: number;
  readonly title: string;
  readonly body: string;
};

const STEPS: readonly Step[] = [
  {
    n: 1,
    title: "Sign in",
    body: "Open archlucid.net and sign in with your work identity (Microsoft Entra ID or a Google Workspace account). The sign-in flow uses your existing identity provider — there is no separate account to create and no credit card is required to start. You will land on a clean workspace ready for your first architecture run.",
  },
  {
    n: 2,
    title: "Pick a vertical",
    body: "A short picker asks which industry profile to start from. The defaults match the briefs in templates/briefs/ — financial-services, healthcare, public-sector, public-sector-us, retail, saas. Choose the closest match; you can change it later. The vertical sets default compliance rules, terminology, and analysis priorities so the first run produces findings relevant to your domain. You are not locked in — the vertical can be changed at any time, and you can run against multiple verticals from the same workspace.",
  },
  {
    n: 3,
    title: "Run a sample & read your first finding",
    body: "ArchLucid pre-populates a sample architecture request shaped for the vertical you picked, then runs the analysis pipeline — no upload required for the first pass. Within a few seconds you get a finalized manifest with structured findings. Open the run and read your first typed finding — what was flagged, why it was flagged, what evidence backs it — the smallest unit of value the product produces.",
  },
  {
    n: 4,
    title: "Decide what to do next",
    body: "Either invite a colleague and run a second sample, or hand off to a guided pilot. Invite a colleague to sign in and run the same sample or a different vertical — no configuration is needed. When you are ready for real inputs, the guided pilot path walks through committing a manifest and reviewing artifacts you would ship in production.",
  },
] as const;

/** When set at build time, marketing shows a CTA to the public demo (e.g. https://demo.archlucid.net). */
function getLiveDemoUrl(): string | null {
  const raw = process.env.NEXT_PUBLIC_DEMO_URL?.trim();
  if (!raw) {
    return null;
  }
  if (!/^https:\/\//i.test(raw) || raw.includes("..")) {
    return null;
  }
  return raw;
}

export default function GetStartedPage(): ReactNode {
  const liveDemoUrl = getLiveDemoUrl();
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
        Four milestones. Roughly thirty minutes end-to-end on a normal connection.
      </p>
      <p className="mt-2 text-sm text-neutral-600 dark:text-neutral-400">
        Want proof without signing in first?{" "}
        <Link className="font-medium text-teal-800 underline dark:text-teal-300" href="/demo/explain">
          See a live run — citations &amp; provenance
        </Link>{" "}
        (same seeded demo surface as{" "}
        <Link className="font-medium text-teal-800 underline dark:text-teal-300" href="/demo/preview">
          /demo/preview
        </Link>
        , interactive explainability view).{" "}
        <span className="font-medium text-neutral-700 dark:text-neutral-300">Verify:</span>{" "}
        <Link className="font-medium text-teal-800 underline dark:text-teal-300" href="/see-it">
          /see-it
        </Link>
        ,{" "}
        <Link className="font-medium text-teal-800 underline dark:text-teal-300" href="/why">
          /why
        </Link>
        .
      </p>

      {liveDemoUrl ? (
        <div
          className="mt-6 rounded-md border border-teal-200 bg-teal-50 p-4 text-sm text-teal-950 dark:border-teal-800 dark:bg-teal-950/40 dark:text-teal-100"
          data-testid="get-started-live-demo-cta"
        >
          <p className="font-medium">Try the live demo</p>
          <p className="mt-1 text-teal-900/90 dark:text-teal-200/90">
            Open the shared sandbox (ArchLucid in simulator mode — no install). You can review pre-seeded sample runs
            and start your own.
          </p>
          <p className="mt-3">
            <a
              className="inline-flex font-medium text-teal-800 underline underline-offset-2 dark:text-teal-300"
              data-testid="get-started-live-demo-link"
              href={liveDemoUrl}
              rel="noopener noreferrer"
            >
              Open demo environment
            </a>
          </p>
        </div>
      ) : null}

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
                {VERTICAL_DISPLAY_NAMES[slug]}
              </button>
            </li>
          ))}
        </ul>
      </section>

      <ol className="mt-10 space-y-8" data-testid="get-started-steps">
        {STEPS.map((step) => (
          <li key={step.n} data-testid={`get-started-step-${step.n}`} className="flex gap-4">
            <div
              className="flex h-10 w-10 flex-shrink-0 items-center justify-center rounded-full bg-teal-100 text-sm font-bold text-teal-800 dark:bg-teal-900 dark:text-teal-200"
              data-testid={`get-started-step-${step.n}-indicator`}
              aria-hidden="true"
            >
              {step.n}
            </div>
            <div>
              <h3 className="text-base font-semibold text-neutral-900 dark:text-neutral-50">
                {step.title}
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
            Ready for a real pilot with your own data? The{" "}
            <a
              className="text-teal-700 underline underline-offset-2 dark:text-teal-300"
              href={`${DEFAULT_GITHUB_BLOB_BASE}/docs/CORE_PILOT.md`}
              rel="noopener noreferrer"
            >
              Core Pilot guide
            </a>{" "}
            walks through creating a request, committing a manifest, and reviewing real artifacts.
          </li>
          <li>
            For the operator path after the sample run, see{" "}
            <Link className="text-teal-700 underline underline-offset-2 dark:text-teal-300" href="/pricing">
              pricing
            </Link>{" "}
            or request a quote from the pricing page.
          </li>
          <li>
            For the sponsor-facing narrative, see the{" "}
            <a
              className="text-teal-700 underline underline-offset-2 dark:text-teal-300"
              href={`${DEFAULT_GITHUB_BLOB_BASE}/docs/EXECUTIVE_SPONSOR_BRIEF.md`}
              rel="noopener noreferrer"
            >
              executive sponsor brief
            </a>
            .
          </li>
        </ul>
      </section>
    </main>
  );
}
