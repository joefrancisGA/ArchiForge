import { LAYER_PAGE_GUIDANCE, type LayerGuidancePageKey } from "@/lib/layer-guidance";

export type LayerHeaderProps = {
  pageKey: LayerGuidancePageKey;
  className?: string;
};

/**
 * Compact route-level reminder of which product layer the page belongs to and
 * when to use it. Keeps copy short per docs/OPERATOR_DECISION_GUIDE.md.
 */
export function LayerHeader({ pageKey, className }: LayerHeaderProps) {
  const block = LAYER_PAGE_GUIDANCE[pageKey];

  return (
    <aside
      className={
        className ??
        "mb-4 max-w-3xl border-l-4 border-teal-700 py-1 pl-3 dark:border-teal-500"
      }
      aria-label={`${block.layerBadge}: ${block.headline}`}
    >
      <p className="m-0 text-[11px] font-semibold uppercase tracking-wide text-teal-900 dark:text-teal-200">
        {block.layerBadge}
      </p>
      <p className="m-0 mt-0.5 text-sm font-medium text-neutral-900 dark:text-neutral-100">{block.headline}</p>
      <p className="m-0 mt-1 text-sm leading-snug text-neutral-600 dark:text-neutral-400">{block.useWhen}</p>
      {block.firstPilotNote ? (
        <p className="m-0 mt-1.5 text-xs text-neutral-500 dark:text-neutral-500">{block.firstPilotNote}</p>
      ) : null}
      {block.enterpriseFootnote ? (
        <p className="m-0 mt-1.5 text-xs font-medium text-neutral-700 dark:text-neutral-300">
          {block.enterpriseFootnote}
        </p>
      ) : null}
    </aside>
  );
}
