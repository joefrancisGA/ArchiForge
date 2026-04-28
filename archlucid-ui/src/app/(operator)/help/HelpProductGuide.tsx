import Link from "next/link";

/**
 * Static, immediately-rendered product help (no fetch). Developer doc index is secondary in HelpDocsClient.
 */
export function HelpProductGuide() {
  return (
    <section
      className="rounded-lg border border-teal-200/80 bg-white/90 p-5 shadow-sm dark:border-teal-900/50 dark:bg-neutral-950/80"
      aria-labelledby="help-product-guide-heading"
    >
      <h2
        id="help-product-guide-heading"
        className="m-0 text-lg font-semibold text-neutral-900 dark:text-neutral-100"
      >
        Using ArchLucid
      </h2>
      <p className="mt-2 text-sm text-neutral-600 dark:text-neutral-400">
        Guides for architects: request, run, manifest, findings, and artifacts. Terminology below matches the product
        (finalize, not commit, for the reviewed manifest).
      </p>
      <ol className="mt-4 list-decimal space-y-3 pl-5 text-sm text-neutral-800 dark:text-neutral-200">
        <li>
          <strong>First request</strong> — Use{" "}
          <Link className="text-teal-700 underline dark:text-teal-300" href="/runs/new">
            New request
          </Link>{" "}
          to describe your system and start the pipeline. You can use an industry starter or a blank brief.
        </li>
        <li>
          <strong>Requests and runs</strong> — Each submission creates a <strong>run</strong>. Track progress on the run
          detail page until the manifest is ready to finalize.
        </li>
        <li>
          <strong>Manifests</strong> — After you <strong>finalize</strong>, the run has a versioned, reviewed{" "}
          <strong>manifest</strong> (architecture record). Open it from the run or the manifests list.
        </li>
        <li>
          <strong>Findings</strong> — Issues and recommendations are surfaced with severity, rationale, and suggested
          actions. Resolve or accept them in context on the run or governance views.
        </li>
        <li>
          <strong>Artifacts and review trail</strong> — Generated outputs and provenance (review trail) show what was
          produced and how decisions were recorded.
        </li>
        <li>
          <strong>Troubleshooting</strong> — If a page fails to load, refresh once; for sign-in issues, return to{" "}
          <Link className="text-teal-700 underline dark:text-teal-300" href="/auth/signin">
            Sign in
          </Link>
          .
        </li>
      </ol>
    </section>
  );
}
