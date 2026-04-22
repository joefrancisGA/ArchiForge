/** Static notice: public price list cutover vs Marketplace go-live (item 22). */
export function MarketingPricingPublicCutoverNotice() {
  return (
    <aside
      aria-label="Public price list and Marketplace go-live timing"
      className="mb-6 rounded-lg border border-neutral-200 bg-neutral-50 px-4 py-3 text-sm text-neutral-700 dark:border-neutral-800 dark:bg-neutral-900/40 dark:text-neutral-300"
    >
      These prices become the canonical public list at Azure Marketplace go-live (Q2 2026 — see{" "}
      <code className="rounded bg-white px-1 font-mono text-xs text-neutral-800 dark:bg-neutral-950 dark:text-neutral-200">
        docs/PENDING_QUESTIONS.md
      </code>{" "}
      item 22). Until then, list prices are subject to change without notice.
    </aside>
  );
}
