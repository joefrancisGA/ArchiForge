"use client";

import { useNavCallerAuthorityRank } from "@/components/OperatorNavAuthorityProvider";
import {
  layerHeaderEnterpriseOperatorRankLine,
  layerHeaderEnterpriseReaderRankLine,
} from "@/lib/enterprise-controls-context-copy";
import { LAYER_PAGE_GUIDANCE, type LayerGuidancePageKey } from "@/lib/layer-guidance";
import { AUTHORITY_RANK } from "@/lib/nav-authority";
import { cn } from "@/lib/utils";

export type LayerHeaderProps = {
  pageKey: LayerGuidancePageKey;
  className?: string;
};

/**
 * Compact route-level reminder of which **product packaging layer** the page belongs to and when to use it.
 * Copy lives in **`layer-guidance.ts`** (`LayerGuidancePageKey` per route family); keep keys in sync when adding pages.
 * Buyer-facing layer model: **docs/PRODUCT_PACKAGING.md** §1–3; operator “when to use” narrative: **docs/OPERATOR_DECISION_GUIDE.md**.
 *
 * **Enterprise Controls** (`layerBadge === "Enterprise Controls"`): adds a **rank-aware** line under the footnote
 * (reader vs operator+ framing) using `useNavCallerAuthorityRank()` — **UI shaping only**; writes still require matching
 * API policies. **Not** entitlements or billing (**docs/COMMERCIAL_BOUNDARY_HARDENING_SEQUENCE.md** Stage 1).
 */
export function LayerHeader({ pageKey, className }: LayerHeaderProps) {
  const block = LAYER_PAGE_GUIDANCE[pageKey];
  const callerAuthorityRank = useNavCallerAuthorityRank();
  const enterpriseRankCue =
    block.layerBadge === "Enterprise Controls"
      ? callerAuthorityRank < AUTHORITY_RANK.ExecuteAuthority
        ? layerHeaderEnterpriseReaderRankLine
        : layerHeaderEnterpriseOperatorRankLine
      : null;

  const isEnterpriseControls = block.enterpriseFootnote !== null && block.enterpriseFootnote !== undefined;

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
          isEnterpriseControls
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
      {enterpriseRankCue ? (
        <p
          className="m-0 mt-1 text-xs text-neutral-600 dark:text-neutral-400"
          data-testid="layer-header-enterprise-rank-cue"
          role="note"
        >
          {enterpriseRankCue}
        </p>
      ) : null}
    </aside>
  );
}
