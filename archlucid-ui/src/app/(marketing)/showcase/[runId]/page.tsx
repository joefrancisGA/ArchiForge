import type { Metadata } from "next";
import Link from "next/link";
import type { ReactElement, ReactNode } from "react";

import {
  DemoPreviewMarketingBody,
  DemoPreviewNotAvailable,
} from "../../demo/preview/DemoPreviewMarketingBody";
import type { DemoCommitPagePreviewResponse } from "@/types/demo-preview";
import { getShowcaseStaticDemoPayload } from "@/lib/showcase-static-demo";

import { ShowcaseWhatThisProves, showcaseOutcomeSnapshotFromPayload } from "./ShowcaseWhatThisProves";
export const revalidate = 300;

const SHOWCASE_HERO_SUBTITLE =
  "Reviewed architecture output — manifest, findings, and audit trail";

type PageProps = {
  params: Promise<{ runId: string }>;
};

function shouldServeShowcaseStaticOnly(): boolean {
  const a = process.env.SHOWCASE_STATIC_ONLY?.trim().toLowerCase();
  const b = process.env.NEXT_PUBLIC_SHOWCASE_STATIC_ONLY?.trim().toLowerCase();

  return a === "true" || a === "1" || b === "true" || b === "1";
}

function resolveShowcaseApiBase(): string {
  if (shouldServeShowcaseStaticOnly()) {
    return "";
  }

  const explicit = process.env.NEXT_PUBLIC_DEMO_PREVIEW_API_BASE?.trim();

  if (explicit)
    return explicit.replace(/\/$/, "");

  const server = process.env.ARCHLUCID_API_BASE_URL?.trim();

  if (server)
    return server.replace(/\/$/, "");

  const pub = process.env.NEXT_PUBLIC_ARCHLUCID_API_BASE_URL?.trim();

  if (pub)
    return pub.replace(/\/$/, "");

  return "";
}

function showcaseTitleForRunId(runId: string): string {
  const decoded = decodeURIComponent(runId);

  if (decoded === "claims-intake-modernization") {
    return "Claims Intake Modernization: Completed Architecture Output";
  }

  return `Completed example (${decoded})`;
}

function ShowcaseLead({ children }: { readonly children: ReactNode }) {
  return <p className="mt-2 text-sm text-neutral-600 dark:text-neutral-400">{children}</p>;
}

function ShowcaseHero({ runId }: { readonly runId: string }): ReactElement {
  return (
    <>
      <h1 className="text-2xl font-semibold text-neutral-900 dark:text-neutral-50">
        {showcaseTitleForRunId(runId)}
      </h1>
      <p className="mt-1 text-sm text-neutral-600 dark:text-neutral-400">{SHOWCASE_HERO_SUBTITLE}</p>
    </>
  );
}

/** Bottom conversion — public marketing surface; deep-links to trial and sign-in. */
function ShowcaseBottomCTA(): ReactElement {
  return (
    <section
      aria-label="Get started with ArchLucid"
      className="mt-10 rounded-lg border border-neutral-200 bg-neutral-50/80 p-6 dark:border-neutral-800 dark:bg-neutral-900/40"
      data-testid="showcase-bottom-cta"
    >
      <h2 className="m-0 text-lg font-semibold text-neutral-900 dark:text-neutral-50">
        Create your own architecture output
      </h2>
      <p className="mt-2 text-sm text-neutral-600 dark:text-neutral-400">
        Start a new request in your workspace to generate manifests, findings, and exports for your systems.
      </p>
      <div className="mt-4 flex flex-wrap gap-3">
        <Link
          href="/get-started"
          className="inline-flex rounded-md bg-teal-700 px-4 py-2 text-sm font-medium text-white no-underline hover:bg-teal-800 dark:bg-teal-600 dark:hover:bg-teal-500"
        >
          Start your own analysis
        </Link>
        <Link
          href="/auth/signin"
          className="inline-flex rounded-md border border-neutral-300 bg-white px-4 py-2 text-sm font-medium text-neutral-900 no-underline hover:bg-neutral-50 dark:border-neutral-600 dark:bg-neutral-900 dark:text-neutral-100 dark:hover:bg-neutral-800"
        >
          Sign in to workspace
        </Link>
      </div>
    </section>
  );
}

/** One-line teaser under the hero — caps length for marketing hero layout. */
function trimLeadDescription(desc: string | undefined | null): string {
  const t = (desc ?? "").trim();

  if (t.length === 0) {
    return "Sample output for a finalized architecture analysis — manifest, artifacts, and review trail.";
  }

  return t.length <= 80 ? t : `${t.slice(0, 77)}…`;
}

function keyDriversFromPayload(payload: DemoCommitPagePreviewResponse): string[] {
  const raw = payload.runExplanation?.explanation?.keyDrivers;

  if (!Array.isArray(raw)) {
    return [];
  }

  return raw
    .filter((x): x is string => typeof x === "string" && x.trim().length > 0)
    .map((s) => s.trim())
    .slice(0, 4);
}

/** Served when preview API responds with an error — still renders curated demo data. */
function ShowcaseApiUnavailableBanner(): ReactElement {
  return (
    <div
      className="mt-4 rounded border border-amber-200 bg-amber-50 px-3 py-2 text-xs text-amber-950 dark:border-amber-800 dark:bg-amber-950/50 dark:text-amber-100"
      role="status"
      data-testid="showcase-api-unavailable-banner"
    >
      <span className="font-semibold">Live preview unavailable.</span> Showing curated sample results instead —{" "}
      <span className="font-medium">sample output generated from curated demo data.</span>
    </div>
  );
}

function ShowcaseLoadFailed(): ReactElement {
  return (
    <p className="mt-3 text-sm text-neutral-600 dark:text-neutral-400">
      This showcase could not be loaded right now. Please try again later.
    </p>
  );
}

/** Shared banner when browsing static baked-in demo preview (API not reachable). Hidden in demo mode — body shows DemoStatusBanner. */
function ShowcaseStaticDemoBanner(): ReactElement | null {
  if (process.env.NEXT_PUBLIC_DEMO_MODE === "true") {
    return null;
  }

  return (
    <div
      className="mt-4 rounded border border-sky-200 bg-sky-50 px-3 py-2 text-xs text-sky-950 dark:border-sky-800 dark:bg-sky-950/50 dark:text-sky-100"
      role="status"
      data-testid="showcase-static-demo-banner"
    >
      <span className="font-semibold">Static demo preview.</span> Viewing curated sample results —{" "}
      <span className="font-medium">sample output generated from curated demo data.</span>
    </div>
  );
}

export async function generateMetadata(props: PageProps): Promise<Metadata> {
  const { runId } = await props.params;

  return {
    title: `ArchLucid · ${showcaseTitleForRunId(runId)}`,
    description: "Completed architecture output — manifest, findings, artifacts, and review trail.",
    robots: { index: true, follow: true },
  };
}

async function fetchShowcasePayload(
  url: string,
): Promise<{ kind: "ok"; payload: DemoCommitPagePreviewResponse } | { kind: "bad_json" } | { kind: "missing" } | { kind: "not_found" } | { kind: "http_error" } | { kind: "invalid" }> {
  try {
    const response = await fetch(url, { next: { revalidate: 300 } });

    if (response.status === 404)
      return { kind: "not_found" };

    if (!response.ok)
      return { kind: "http_error" };

    let payload: DemoCommitPagePreviewResponse;

    try {
      payload = (await response.json()) as DemoCommitPagePreviewResponse;
    } catch {
      return { kind: "bad_json" };
    }

    if (payload == null || typeof payload !== "object" || payload.run == null || payload.manifest == null)
      return { kind: "invalid" };

    return { kind: "ok", payload };
  } catch {
    return { kind: "missing" };
  }
}

/** Public marketing projection of finalized run preview (dynamic API path; static fallback when no API URL). */
export default async function MarketingShowcasePage(props: PageProps) {
  const { runId } = await props.params;
  const decodedRunId = decodeURIComponent(runId);
  const base = resolveShowcaseApiBase();

  if (!base) {
    const payload = getShowcaseStaticDemoPayload(decodedRunId);

    return (
      <main className="mx-auto max-w-5xl px-4 py-10">
        <ShowcaseHero runId={runId} />

        <ShowcaseLead>{trimLeadDescription(payload.run.description)}</ShowcaseLead>

        <div className="mt-6">
          <ShowcaseWhatThisProves
            scenarioBullets={keyDriversFromPayload(payload)}
            outcomeSnapshot={showcaseOutcomeSnapshotFromPayload(payload)}
          />
        </div>

        <ShowcaseStaticDemoBanner />

        <div className="mt-6">
          <DemoPreviewMarketingBody payload={payload} />
        </div>

        <ShowcaseBottomCTA />
      </main>
    );
  }

  const encoded = encodeURIComponent(runId);
  const url = `${base}/v1/marketing/showcase/${encoded}`;
  const bundle = await fetchShowcasePayload(url);

  switch (bundle.kind) {
    case "not_found": {
      return (
        <main className="mx-auto max-w-5xl px-4 py-10">
          <ShowcaseHero runId={runId} />

          <div className="mt-6">
            <DemoPreviewNotAvailable />
          </div>

          <ShowcaseBottomCTA />
        </main>
      );
    }

    case "ok":
      return (
        <main className="mx-auto max-w-5xl px-4 py-10">
          <ShowcaseHero runId={runId} />

          <ShowcaseLead>{trimLeadDescription(bundle.payload.run.description)}</ShowcaseLead>

          <div className="mt-6">
            <ShowcaseWhatThisProves
              scenarioBullets={keyDriversFromPayload(bundle.payload)}
              outcomeSnapshot={showcaseOutcomeSnapshotFromPayload(bundle.payload)}
            />
          </div>

          <div className="mt-6">
            <DemoPreviewMarketingBody payload={bundle.payload} />
          </div>

          <ShowcaseBottomCTA />
        </main>
      );

    case "bad_json": {
      return (
        <main className="mx-auto max-w-5xl px-4 py-10">
          <ShowcaseHero runId={runId} />

          <ShowcaseLoadFailed />

          <p className="mt-2 text-sm text-neutral-600 dark:text-neutral-400">
            The server returned data this page could not read.
          </p>

          <ShowcaseBottomCTA />
        </main>
      );
    }

    case "invalid": {
      return (
        <main className="mx-auto max-w-5xl px-4 py-10">
          <ShowcaseHero runId={runId} />

          <div className="mt-6">
            <DemoPreviewNotAvailable />
          </div>

          <ShowcaseBottomCTA />
        </main>
      );
    }

    case "http_error":
    case "missing": {
      const fallbackPayload = getShowcaseStaticDemoPayload(decodedRunId);

      return (
        <main className="mx-auto max-w-5xl px-4 py-10">
          <ShowcaseHero runId={runId} />

          <ShowcaseLead>{trimLeadDescription(fallbackPayload.run.description)}</ShowcaseLead>

          <div className="mt-6">
            <ShowcaseWhatThisProves
              scenarioBullets={keyDriversFromPayload(fallbackPayload)}
              outcomeSnapshot={showcaseOutcomeSnapshotFromPayload(fallbackPayload)}
            />
          </div>

          <ShowcaseApiUnavailableBanner />

          <div className="mt-6">
            <DemoPreviewMarketingBody payload={fallbackPayload} />
          </div>

          <ShowcaseBottomCTA />
        </main>
      );
    }
  }
}
