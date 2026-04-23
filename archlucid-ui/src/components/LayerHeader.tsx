"use client";

import { type LayerGuidancePageKey } from "@/lib/layer-guidance";
import { useNavSurface } from "@/lib/use-nav-surface";
import { cn } from "@/lib/utils";

export type LayerHeaderProps = {
  pageKey: LayerGuidancePageKey;
  className?: string;
};

/**
 * Compact route-level reminder of which **buyer layer** the page belongs to (**Pilot** vs **Operate**) and when to use it.
 * Copy lives in **`layer-guidance.ts`** (`LayerGuidancePageKey` per route family). **`useNavSurface()`** composes **Visibility**
 * (this strip + nav tier rules) separately from **Capability** (`useOperateCapability` on each route).
 *
 * **Operate · governance** rows (non-null **`enterpriseFootnote`**): typography matches the governance slice; an **Execute+**
 * rank cue line is composed only when **`callerAuthorityRank >= AUTHORITY_RANK.ExecuteAuthority`** (**UI only** — API **`[Authorize]`** wins).
 * **Operate · analysis** rows omit the footnote and do not show the Execute cue strip here.
 *
 * @see `LayerHeader.test.tsx`
 * @see `authority-seam-regression.test.ts` — **`LAYER_PAGE_GUIDANCE`** Operate slice contract.
 * @see `operate-authority-ui-shaping.test.tsx` — mutation hook → **`disabled`** / **`readOnly`** on representative pages.
 */
export function LayerHeader({ pageKey, className }: LayerHeaderProps) {
  const surface = useNavSurface(pageKey);
  const block = surface.layerGuidance;
  const operateExecuteRankCue = surface.contextHints.layerHeaderEnterpriseRankCue;
  const usesOperateGovernanceFootnote =
    block.enterpriseFootnote !== null && block.enterpriseFootnote !== undefined;

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
      <p
        className={cn(
          "m-0 mt-1 leading-snug",
          usesOperateGovernanceFootnote
            ? "text-xs text-neutral-500 dark:text-neutral-400"
            : "text-sm text-neutral-600 dark:text-neutral-400",
        )}
      >
        {block.useWhen}
      </p>
      {block.firstPilotNote ? (
        <p className="m-0 mt-1.5 text-xs text-neutral-500 dark:text-neutral-500">{block.firstPilotNote}</p>
      ) : null}
      {block.enterpriseFootnote ? (
        <p className="m-0 mt-1.5 text-xs font-medium text-neutral-700 dark:text-neutral-300">
          {block.enterpriseFootnote}
        </p>
      ) : null}
      {operateExecuteRankCue ? (
        <p
          className="m-0 mt-1 text-xs text-neutral-600 dark:text-neutral-400"
          data-testid="layer-header-operate-execute-rank-cue"
          role="note"
        >
          {operateExecuteRankCue}
        </p>
      ) : null}
    </aside>
  );
}
