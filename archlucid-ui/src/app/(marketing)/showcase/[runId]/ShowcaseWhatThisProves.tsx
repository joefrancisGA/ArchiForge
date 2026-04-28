import type { ReactElement } from "react";

/** Fixed product-capability bullets for marketing showcase (complements scenario key drivers from API). */
export const SHOWCASE_ARCHLUCID_OUTPUT_BULLETS: readonly string[] = [
  "Governed manifest finalized with decision and warning counts",
  "Findings and narrative explanation surfaced for review",
  "Artifacts exported as a bundle and per-descriptor downloads",
  "Review trail and pipeline events recorded for traceability",
];

const sectionBoxClass =
  "rounded-lg border border-teal-200/80 bg-teal-50/60 p-4 shadow-sm dark:border-teal-900/40 dark:bg-teal-950/40";

type ShowcaseWhatThisProvesProps = {
  /** Business / scenario outcomes (e.g. key drivers from demo payload). */
  readonly scenarioBullets: readonly string[];
};

/**
 * Split “what ArchLucid produced” vs “business scenario” so the page reads as product proof, not only outcomes copy.
 */
export function ShowcaseWhatThisProves({ scenarioBullets }: ShowcaseWhatThisProvesProps): ReactElement {
  const scenarioTrimmed = scenarioBullets.filter((s) => s.trim().length > 0);

  return (
    <div className="space-y-6" data-testid="showcase-what-this-proves">
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
