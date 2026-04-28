/**
 * SSR loading state for marketing showcase — avoids indefinite blank shell while `/v1/marketing/showcase/...` fetches during build or cold start.
 */
export default function MarketingShowcaseLoading() {
  return (
    <main className="mx-auto max-w-5xl px-4 py-10">
      <div
        aria-busy
        aria-label="Loading showcase"
        className="rounded-lg border border-neutral-200 bg-white/80 px-6 py-10 text-center dark:border-neutral-800 dark:bg-neutral-950/80"
      >
        <p className="m-0 text-sm font-medium text-neutral-800 dark:text-neutral-200">Loading showcase…</p>
        <p className="mt-2 m-0 text-xs text-neutral-500 dark:text-neutral-400">
          Preparing the read-only completed example preview.
        </p>
      </div>
    </main>
  );
}
