import type { ReactElement } from "react";

/**
 * Inline notice when operator run/manifest content is served from the curated showcase bundle
 * because the upstream API returned an error and {@link isOperatorDemoStaticMode} is enabled.
 */
export function OperatorDemoStaticBanner(): ReactElement {
  const demoMode = process.env.NEXT_PUBLIC_DEMO_MODE === "true";

  return (
    <div
      className={
        demoMode
          ? "rounded-md border border-neutral-200 bg-neutral-50 p-3 text-sm text-neutral-800 dark:border-neutral-700 dark:bg-neutral-900/60 dark:text-neutral-100"
          : "rounded-md border border-amber-200 bg-amber-50 p-3 text-sm text-amber-950 dark:border-amber-800 dark:bg-amber-950 dark:text-amber-100"
      }
      role="status"
      data-demo-static="true"
    >
      <strong className="font-medium">{demoMode ? "Sample data" : "Demonstration content"}</strong>
      {" — "}
      {demoMode
        ? "Curated scenario aligned with the public completed example; connect a workspace for live data."
        : "The live API was unavailable for this view; showing curated demo data aligned with the Completed example showcase."}
    </div>
  );
}
