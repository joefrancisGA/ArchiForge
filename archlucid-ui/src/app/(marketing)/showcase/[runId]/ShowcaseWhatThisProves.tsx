import Link from "next/link";
import type { ReactElement } from "react";



import { DEFAULT_GITHUB_BLOB_BASE } from "@/lib/docs-public-base";
import type { DemoCommitPagePreviewResponse } from "@/types/demo-preview";



/** Fixed product-capability bullets for marketing showcase (complements scenario key drivers from API). */

export const SHOWCASE_ARCHLUCID_OUTPUT_BULLETS: readonly string[] = [

  "Governed manifest finalized with decision and warning counts",

  "Findings and narrative explanation surfaced for review",

  "Artifacts exported as a bundle and per-descriptor downloads",

  "Review trail and pipeline events recorded for traceability",

];



const sectionBoxClass =

  "rounded-lg border border-teal-200/80 bg-teal-50/60 p-4 shadow-sm dark:border-teal-900/40 dark:bg-teal-950/40";



export type ShowcaseOutcomeSnapshot = {

  readonly manifestCommitted: boolean;

  readonly findingCount: number | null;

  readonly artifactCount: number;

  readonly pipelineEventCount: number;

};



export function showcaseOutcomeSnapshotFromPayload(

  payload: DemoCommitPagePreviewResponse,

): ShowcaseOutcomeSnapshot {

  return {

    manifestCommitted: payload.manifest.status === "Committed",

    findingCount: payload.runExplanation?.findingCount ?? null,

    artifactCount: payload.artifacts.length,

    pipelineEventCount: payload.pipelineTimeline.length,

  };

}



export function ShowcaseOutcomeCards({ snapshot }: { readonly snapshot: ShowcaseOutcomeSnapshot }): ReactElement {

  return (

    <div

      className="grid gap-3 sm:grid-cols-2 lg:grid-cols-4"

      data-testid="showcase-outcome-cards"

      role="list"

      aria-label="Demonstration outputs at a glance"

    >

      {[

        {

          label: "Manifest",

          value: snapshot.manifestCommitted ? "Finalized" : "Pending",

        },

        {

          label: "Findings surfaced",

          value: snapshot.findingCount === null ? "—" : String(snapshot.findingCount),

        },

        {

          label: "Artifacts exported",

          value: String(snapshot.artifactCount),

        },

        {

          label: "Review trail recorded",

          value: String(snapshot.pipelineEventCount),

        },

      ].map((c) => (

        <div

          key={c.label}

          role="listitem"

          className="rounded-lg border border-teal-300/70 bg-white/70 p-3 shadow-sm dark:border-teal-800/60 dark:bg-neutral-950/40"

        >

          <p className="m-0 text-xs font-medium uppercase tracking-wide text-neutral-500 dark:text-neutral-400">{c.label}</p>

          <p className="m-0 mt-1 text-lg font-semibold text-neutral-900 dark:text-neutral-50">{c.value}</p>

        </div>

      ))}

    </div>

  );

}



type ShowcaseWhatThisProvesProps = {

  /** Business / scenario outcomes (e.g. key drivers from demo payload). */

  readonly scenarioBullets: readonly string[];

  readonly outcomeSnapshot?: ShowcaseOutcomeSnapshot | null;

  /** When false, omit outcome cards here so the parent can render them above the preview body. */
  readonly showOutcomeCards?: boolean;

};



/**

 * Outcome cards for scannable proof, then splits “what ArchLucid produced” vs “business scenario”.

 */

export function ShowcaseWhatThisProves({

  scenarioBullets,

  outcomeSnapshot,

  showOutcomeCards = true,

}: ShowcaseWhatThisProvesProps): ReactElement {

  const scenarioTrimmed = scenarioBullets.filter((s) => s.trim().length > 0);



  return (

    <div className="space-y-6" data-testid="showcase-what-this-proves">

      {outcomeSnapshot !== null && outcomeSnapshot !== undefined && showOutcomeCards ? (

        <section aria-labelledby="showcase-outcome-cards-heading" className="space-y-3">

          <h2

            id="showcase-outcome-cards-heading"

            className="m-0 text-base font-semibold text-neutral-900 dark:text-neutral-50"

          >

            At a glance

          </h2>

          <ShowcaseOutcomeCards snapshot={outcomeSnapshot} />

        </section>

      ) : null}



      <section aria-labelledby="showcase-archlucid-outputs-heading" className={sectionBoxClass}>

        <h2

          id="showcase-archlucid-outputs-heading"

          className="m-0 text-base font-semibold text-neutral-900 dark:text-neutral-50"

        >

          What ArchLucid produced

        </h2>

        <ul className="mt-3 list-disc space-y-1 pl-6 text-sm text-neutral-700 dark:text-neutral-200">

          {SHOWCASE_ARCHLUCID_OUTPUT_BULLETS.map((line, index) => (

            <li key={`product-${index}`}>{line}</li>

          ))}

        </ul>

        <p className="mt-3 text-xs text-neutral-600 dark:text-neutral-400">
          <span className="font-semibold text-neutral-700 dark:text-neutral-300">Verify shipped parity:</span>{" "}
          <Link className="text-teal-700 underline underline-offset-2 dark:text-teal-300" href="/demo/preview">
            /demo/preview
          </Link>
          ,{" "}
          <Link className="text-teal-700 underline underline-offset-2 dark:text-teal-300" href="/see-it">
            /see-it
          </Link>
          . Operator-only flows (exports after auth, compare replay) stay behind sign-in per{" "}
          <Link className="text-teal-700 underline underline-offset-2 dark:text-teal-300" href="/trust">
            /trust
          </Link>{" "}
          and{" "}
          <a
            className="text-teal-700 underline underline-offset-2 dark:text-teal-300"
            href={`${DEFAULT_GITHUB_BLOB_BASE}/docs/library/V1_SCOPE.md`}
            rel="noopener noreferrer"
            target="_blank"
          >
            V1_SCOPE.md
          </a>
          .
        </p>

      </section>



      {scenarioTrimmed.length > 0 ? (

        <section aria-labelledby="showcase-business-scenario-heading" className={sectionBoxClass}>

          <h2

            id="showcase-business-scenario-heading"

            className="m-0 text-base font-semibold text-neutral-900 dark:text-neutral-50"

          >

            Business scenario

          </h2>

          <ul className="mt-3 list-disc space-y-1 pl-6 text-sm text-neutral-700 dark:text-neutral-200">

            {scenarioTrimmed.map((line, index) => (

              <li key={`scenario-${index}`}>{line}</li>

            ))}

          </ul>

        </section>

      ) : null}

    </div>

  );

}

