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
 * **`LayerGuidancePageKey`** is the UI counterpart to **docs/PRODUCT_PACKAGING.md** §3 *Code seams* (**`NAV_GROUPS[].id`**
 * → Core / Advanced / Enterprise); a new Enterprise route should add a key here and wire **`pageKey`** on the page.
 * **Contributor step:** **docs/PRODUCT_PACKAGING.md** §3 *Contributor drift guard* — **Guidance strip** (pair with **`nav-config`** / API policy when the route’s packaging story changes).
 *
 * **Doc map:** buyer layers — **docs/PRODUCT_PACKAGING.md** §1–2; operator “when to use” — **docs/OPERATOR_DECISION_GUIDE.md**;
 * contributor seam table — **docs/PRODUCT_PACKAGING.md** §3 *Code seams*; change checklist — §3 *Contributor drift guard*
 * (align **`nav-config.ts`** `requiredAuthority` with C# **`ArchLucidPolicies`**).
 *
 * **Enterprise Controls** (`layerBadge === "Enterprise Controls"`): rank-aware line under **`enterpriseFootnote`**
 * (`callerAuthorityRank < AUTHORITY_RANK.ExecuteAuthority` ⇒ reader line, else operator line). **Cognitive / UI shaping
 * only** — same **Execute** numeric floor as **`useEnterpriseMutationCapability()`** for this cue line only; this component
 * **does not call** **`useEnterpriseMutationCapability()`** (mutation gating stays on each route). **`[Authorize(Policy = …)]`**
 * on **ArchLucid.Api** is still authoritative (**401/403**). **Does not implement** sidebar **tier** or **nav** inclusion
 * (**`nav-shell-visibility.ts`**); pair **`LayerHeader`** with correct **`nav-config.ts`** / route policies when adding pages.
 * **Other read vs write UX** (e.g. audit **CSV** by **`/me`** Auditor/Admin, not Execute rank) stays on the route with
 * **`currentPrincipal`** — **`LayerHeader`** only reflects rank for Enterprise rank cue + packaging copy.
 * Not entitlements or billing — **docs/COMMERCIAL_BOUNDARY_HARDENING_SEQUENCE.md** §4.
 *
 * @see **docs/PRODUCT_PACKAGING.md** §3 (*Contributor drift guard* — *Guidance strip* step) when adding Enterprise keys.
 * @see `LayerHeader.test.tsx` — Enterprise footnotes + rank cue (incl. conservative caller rank **0**); **`aside`** **`aria-label`** (badge + headline).
 * @see `authority-seam-regression.test.ts` — **`LAYER_PAGE_GUIDANCE`** Enterprise vs Advanced **`enterpriseFootnote`** contract (packaging ↔ this component).
 * @see `authority-execute-floor-regression.test.ts` — **`AUTHORITY_RANK.ExecuteAuthority`** used the same way for **nav** and **mutation** booleans; this component’s rank cue shares that numeric line (**UI only**).
 * @see `authority-shaped-ui-regression.test.ts` — **`nav-config`** catalog **`ExecuteAuthority`** rows vs rank (packaging metadata ↔ this strip’s **Execute** floor).
 * @see `enterprise-authority-ui-shaping.test.tsx` — mutation hook → Enterprise **`disabled`** / governance submit **`readOnly`** (same story as rank cue; API still **`[Authorize]`**).
 * @see `authority-shaped-layout-regression.test.tsx` — read-tier **page** column order / hierarchy (this strip does not control layout).
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
