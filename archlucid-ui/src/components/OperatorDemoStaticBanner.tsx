import type { ReactElement } from "react";

/**
 * Inline notice when operator run/manifest content is served from the curated showcase bundle
 * because the upstream API returned an error and {@link isOperatorDemoStaticMode} is enabled.
 */
export function OperatorDemoStaticBanner(): ReactElement {
  return (
    <div
      className="rounded-md border border-amber-200 bg-amber-50 p-3 text-sm text-amber-950 dark:border-amber-800 dark:bg-amber-950 dark:text-amber-100"
      role="status"
      data-demo-static="true"
    >
      <strong className="font-medium">Demonstration content</strong> — the live API was unavailable for this view;
      showing curated demo data aligned with the Completed example showcase.
    </div>
  );
}
