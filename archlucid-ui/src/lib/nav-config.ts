import type { LucideIcon } from "lucide-react";
import {
  Activity,
  BarChart3,
  Bell,
  BookOpen,
  ClipboardList,
  FileSearch,
  GitBranch,
  GitCompare,
  GitGraph,
  Home,
  LayoutDashboard,
  ListOrdered,
  Mail,
  MessageSquare,
  Play,
  Rocket,
  Scale,
  Search,
  Shield,
  Sparkles,
  Tags,
  Wrench,
} from "lucide-react";

import { registryKeyToAriaKeyShortcuts } from "@/lib/shortcut-registry";

import type { RequiredAuthority } from "@/lib/nav-authority";
import type { NavTier } from "@/lib/nav-tier";

export type NavLinkItem = {
  href: string;
  label: string;
  title: string;
  /** Progressive disclosure: essential always; extended after “Show more”; advanced after gear toggle. */
  tier: NavTier;
  /**
   * Minimum API policy tier this destination assumes (see `ArchLucidPolicies` on the server).
   * **Core Pilot essentials** omit this (broad default path). **Advanced** and **Enterprise** links in `NAV_GROUPS` set it — see the module **Authority** section.
   * Enforced after **`tier`** in **`nav-shell-visibility.ts`** (`filterNavLinksForOperatorShell`).
   */
  requiredAuthority?: RequiredAuthority;
  /** Registry combo for `aria-keyshortcuts`, e.g. `alt+n` */
  keyShortcut?: string;
  /** Optional icon for sidebar and mobile drawer. */
  icon?: LucideIcon;
};

export type NavGroupConfig = {
  id: string;
  label: string;
  /** One line under the group title — what this layer is for (see docs/OPERATOR_DECISION_GUIDE.md). */
  caption?: string;
  links: NavLinkItem[];
};

function navTitleWithShortcut(baseTitle: string, registryCombo: string): string {
  const aria = registryKeyToAriaKeyShortcuts(registryCombo);

  return `${baseTitle} (${aria})`;
}

/**
 * Canonical operator shell navigation — sidebar, command palette, and mobile drawer.
 *
 * **API vs. UI:** `tier` and `requiredAuthority` describe how the shell **should** present routes. **Computed visibility**
 * is **`filterNavLinksForOperatorShell`** in **`nav-shell-visibility.ts`** (**tier → authority**; empty groups dropped).
 * **`[Authorize(Policy = …)]`** on **ArchLucid.Api** is **authoritative** (`401`/`403`); nav omission or soft-disabled
 * controls never imply a safe POST or deep link.
 *
 * **Four shaping surfaces (this file owns #1 metadata only):** (1) **Shell link inclusion** — `tier` + `requiredAuthority`
 * here + composition in **`nav-shell-visibility.ts`**; (2) **mutation soft-enable** — **`useEnterpriseMutationCapability()`**
 * (not declared in this file); (3) **`LayerHeader`** strip — **`layer-guidance.ts`**; (4) **inline rank hints** —
 * **`EnterpriseControlsContextHints`**. Do not merge (1) with (2)–(4). Enumeration: **docs/PRODUCT_PACKAGING.md** §3
 * *Four UI shaping surfaces*.
 *
 * Nav groups map to product packaging layers (see docs/PRODUCT_PACKAGING.md):
 *   runs-review    → Core Pilot        (request · run · commit · review)
 *   qa-advisory    → Advanced Analysis (compare, replay, graph, provenance, advisory)
 *   alerts-governance → Enterprise Controls (governance, audit, policy, compliance)
 *
 * **Drift guard:** When adding or moving a route, follow the **ordered checklist** in **docs/PRODUCT_PACKAGING.md** §3
 *   *Contributor drift guard* (API policy → this file → `layer-guidance` / `LayerHeader` → Enterprise mutation hook →
 *   packaging doc). Verify **C#** `[Authorize(Policy = …)]` still matches each link’s **`requiredAuthority`** string.
 *   **Cross-module Vitest:** `authority-seam-regression.test.ts` — e.g. **`/governance`** must stay **`ExecuteAuthority`**
 *   so Reader-ranked callers do not see it under Enterprise nav (deep-link still hits API policy); every **`ExecuteAuthority`**
 *   row under **`qa-advisory`** and **`alerts-governance`** stays absent from Read-tier filtered nav; Core Pilot essential
 *   hrefs stay visible for Reader with default tier toggles; **caller rank `0`** stays stricter than Read for **`ReadAuthority`** links;
 *   **`/alerts`** stays **`essential`** tier; filtered link order and **`listNavGroupsVisibleInOperatorShell`** group order stay aligned with this file;
 *   Enterprise href sets grow **monotonically** Read→Execute→Admin under **`filterNavLinksByAuthority`** alone; default Reader shell keeps **Advanced** to **`/ask`** only (tier before authority); **`/governance`** appears only when **extended and advanced** are on for **Execute** rank (**`filterNavLinksForOperatorShell`**). **`OperatorNavAuthorityProvider.test.tsx`** —
 *   **`useNavCallerAuthorityRank`** stays Read during JWT **`/me`** refetch so stale Execute rank does not flash in nav or hooks.
 *   **`EnterpriseControlsContextHints.authority.test.tsx`** — rank-gated Enterprise sidebar/page cues share the same
 *   **`ExecuteAuthority`** numeric floor as mutation hooks (governance resolution, audit log, **Alerts inbox**, **governance
 *   dashboard** reader cue, alert tooling). **`authority-execute-floor-regression.test.ts`** — same **boolean** for a synthetic
 *   **`ExecuteAuthority`** row vs **`enterpriseMutationCapabilityFromRank`**; **`alerts-governance`** monotonicity Reader→Admin.
 *   **`nav-shell-visibility.test.ts`** also locks **Core Pilot** extended **Execute**
 *   links (e.g. **`/replay`**) behind **Show more** — tier before rank. **`current-principal.test.ts`** locks **`maxAuthority`**
 *   vs **`requiredAuthorityFromRank`** and **`hasEnterpriseOperatorSurfaces`** vs mutation capability.
 *   **`nav-config.structure.test.ts`** — duplicate **`href`**s; **Core Pilot** essentials omit **`requiredAuthority`**;
 *   **Advanced/Enterprise** **`ExecuteAuthority`** links must not use **`essential`** tier (progressive disclosure + rank story).
 *
 * **`layer-guidance.ts` / `LayerHeader`:** Enterprise route families use **`LAYER_PAGE_GUIDANCE`** rows with **`enterpriseFootnote`**
 * (see **`authority-seam-regression.test.ts`** — Enterprise vs Advanced footnote contract). That strip is **cognitive packaging only**;
 * it does not replace **`requiredAuthority`** here or **`[Authorize]`** on the API.
 *
 * **`requiredAuthority` vs Enterprise POSTs:** this field shapes **nav / palette visibility** after tier filtering only
 * (higher **caller rank** does **not** bypass **`tier`** — e.g. Enterprise **extended** hrefs stay hidden until “Show more”;
 * **`nav-shell-visibility.test.ts`**). In-page **POST / toggle** soft-enable on Enterprise-heavy routes uses
 * **`useEnterpriseMutationCapability()`** — same **`AUTHORITY_RANK.ExecuteAuthority`** floor as **`ExecuteAuthority`**
 * links here; keep both aligned with C# policies. **Audit CSV export** is a documented exception: gated on **`/me`** roles (**Auditor** or **Admin**) on the audit page, not this nav field alone.
 *
 * **Authority (`requiredAuthority`) — first-pass map (UI hint only; API still 401/403):**
 *
 * - **Omit** on Core Pilot *essentials* (home, onboarding, new run, runs) so Reader-signed-in pilots keep the default path.
 * - **Core Pilot · extended:** inspection/diff surfaces that are `ReadAuthority` on the API (`GraphController`,
 *   `AuthorityCompareController`) use **`ReadAuthority`**. **Replay** stays **`ExecuteAuthority`**
 *   (`AuthorityReplayController`).
 * - **Advanced Analysis:** every link sets **`requiredAuthority`**. Read/analytics pages → **`ReadAuthority`** unless the
 *   API primary workflow is Execute-class (planning, evolution candidates, advisory **schedules**, digest **subscriptions** → **`ExecuteAuthority`**).
 *   Link `title` strings use **“Label — short description”** for tooltips (same convention as Enterprise).
 * - **Enterprise Controls:** **inbox / dashboards / audit / policy pack browsing / alert tooling** whose controllers
 *   are class-scoped **`ReadAuthority`** → **`ReadAuthority`**. **Governance workflow** (mutations) → **`ExecuteAuthority`**.
 *   Do not use **`AdminAuthority`** on nav entries: Admin-only actions (e.g. policy pack create) are enforced on POST;
 *   the UI page is still reachable at Read for list/effective views.
 *
 * ### UI shaping vs API authorization (boundary)
 *
 * `requiredAuthority` drives **shell visibility** after **`nav-shell-visibility`** tier filtering — not whether HTTP writes
 * succeed. **`[Authorize(Policy = …)]`** on **ArchLucid.Api** is authoritative (**401/403**). Keep policy **names** aligned
 * with C# when moving routes. **Vitest:** `nav-config.structure.test.ts` (graph invariants); **`authority-execute-floor-regression.test.ts`**
 * (Execute-class nav row vs mutation capability; Enterprise **`alerts-governance`** Reader vs Execute href sets); **`src/app/(operator)/enterprise-authority-ui-shaping.test.tsx`**
 * (representative Enterprise pages: **`useEnterpriseMutationCapability`** → **`disabled`** on primary actions).
 *
 * Omitting `requiredAuthority` is used only for **Core Pilot essentials** (default path for any authenticated rank).
 * Every **Enterprise Controls** link in this file sets `requiredAuthority`. Composed with tiers in `@/lib/nav-shell-visibility`.
 *
 * Group IDs are intentionally stable (used as localStorage keys); only labels are user-visible.
 */
export const NAV_GROUPS: NavGroupConfig[] = [
  {
    id: "runs-review",
    // Product layer: Core Pilot
    label: "Core Pilot",
    caption: "Default path — request through commit and artifact review.",
    links: [
      {
        href: "/",
        label: "Home",
        title: navTitleWithShortcut("Home — V1 checklist and quick links", "alt+h"),
        keyShortcut: "alt+h",
        icon: Home,
        tier: "essential",
      },
      {
        href: "/onboarding",
        label: "Onboarding",
        title: "Guided operator onboarding checklist",
        icon: BookOpen,
        tier: "essential",
      },
      {
        href: "/runs/new",
        label: "New run",
        title: navTitleWithShortcut(
          "Guided first-run wizard — system identity through pipeline tracking",
          "alt+n",
        ),
        keyShortcut: "alt+n",
        icon: Rocket,
        tier: "essential",
      },
      {
        href: "/runs?projectId=default",
        label: "Runs",
        title: navTitleWithShortcut("Runs list — open detail, manifest, artifacts, exports", "alt+r"),
        keyShortcut: "alt+r",
        icon: ListOrdered,
        tier: "essential",
      },
      {
        href: "/graph",
        label: "Graph",
        title: navTitleWithShortcut("Provenance or architecture graph for one run ID", "alt+y"),
        keyShortcut: "alt+y",
        icon: GitGraph,
        // Graph is a useful inspection tool but is not part of the Core Pilot path
        // (create → run → commit → review). It surfaces under "Show more links".
        tier: "extended",
        requiredAuthority: "ReadAuthority",
      },
      {
        href: "/compare",
        label: "Compare two runs",
        title: navTitleWithShortcut("Diff two runs (base vs target)", "alt+c"),
        keyShortcut: "alt+c",
        icon: GitCompare,
        tier: "extended",
        requiredAuthority: "ReadAuthority",
      },
      {
        href: "/replay",
        label: "Replay a run",
        title: navTitleWithShortcut("Re-validate authority chain for one run", "alt+p"),
        keyShortcut: "alt+p",
        icon: Play,
        tier: "extended",
        requiredAuthority: "ExecuteAuthority",
      },
    ],
  },
  {
    id: "qa-advisory",
    // Product layer: Advanced Analysis
    label: "Advanced Analysis",
    caption: "When Core Pilot cannot answer your question (diff, replay, graph, Q&A).",
    links: [
      {
        href: "/ask",
        label: "Ask",
        title: navTitleWithShortcut("Ask — natural language Q&A over architecture context", "alt+a"),
        keyShortcut: "alt+a",
        icon: MessageSquare,
        tier: "essential",
        requiredAuthority: "ReadAuthority",
      },
      {
        href: "/search",
        label: "Search",
        title: "Search — indexed architecture content",
        icon: Search,
        tier: "advanced",
        requiredAuthority: "ReadAuthority",
      },
      {
        href: "/advisory",
        label: "Advisory",
        title: "Advisory — scans and architecture digests",
        icon: Activity,
        tier: "extended",
        requiredAuthority: "ReadAuthority",
      },
      {
        href: "/recommendation-learning",
        label: "Recommendation learning",
        title: "Recommendation learning — profiles and ranking signals",
        icon: Sparkles,
        tier: "extended",
        requiredAuthority: "ReadAuthority",
      },
      {
        href: "/product-learning",
        label: "Pilot feedback",
        title: "Pilot feedback — rollups and triage (58R)",
        icon: ClipboardList,
        tier: "extended",
        requiredAuthority: "ReadAuthority",
      },
      {
        href: "/planning",
        label: "Planning",
        title: "Planning — improvement themes and prioritized plans (59R)",
        icon: BarChart3,
        tier: "advanced",
        requiredAuthority: "ExecuteAuthority",
      },
      {
        href: "/evolution-review",
        label: "Evolution candidates",
        title: "Evolution candidates — simulations and before/after review (60R)",
        icon: GitBranch,
        tier: "advanced",
        requiredAuthority: "ExecuteAuthority",
      },
      {
        href: "/advisory-scheduling",
        label: "Schedules",
        title: "Schedules — advisory scan windows",
        icon: Wrench,
        tier: "advanced",
        requiredAuthority: "ExecuteAuthority",
      },
      {
        href: "/digests",
        label: "Digests",
        title: "Digests — generated architecture digests",
        icon: FileSearch,
        tier: "advanced",
        requiredAuthority: "ReadAuthority",
      },
      {
        href: "/digest-subscriptions",
        label: "Subscriptions",
        title: "Subscriptions — digest email delivery",
        icon: Mail,
        tier: "advanced",
        requiredAuthority: "ExecuteAuthority",
      },
    ],
  },
  {
    id: "alerts-governance",
    // Product layer: Enterprise Controls
    label: "Enterprise Controls",
    caption:
      "Operator/admin layer—governance, audit, policy packs, and alert tooling. Typically governance or platform operators; not required for Core Pilot.",
    links: [
      {
        href: "/alerts",
        label: "Alerts",
        title: navTitleWithShortcut("Alerts — open and acknowledged operational inbox", "alt+l"),
        keyShortcut: "alt+l",
        icon: Bell,
        tier: "essential",
        requiredAuthority: "ReadAuthority",
      },
      {
        href: "/alert-rules",
        label: "Alert rules",
        title: "Alert rules — metric thresholds evaluated on advisory scans",
        icon: Tags,
        tier: "advanced",
        requiredAuthority: "ReadAuthority",
      },
      {
        href: "/alert-routing",
        label: "Alert routing",
        title: "Alert routing — delivery subscriptions when new alerts fire",
        icon: Mail,
        tier: "advanced",
        requiredAuthority: "ReadAuthority",
      },
      {
        href: "/composite-alert-rules",
        label: "Composite rules",
        title: "Composite rules — multi-metric AND/OR alert conditions",
        icon: Tags,
        tier: "advanced",
        requiredAuthority: "ReadAuthority",
      },
      {
        href: "/alert-simulation",
        label: "Alert simulation",
        title: "Alert simulation — evaluate rules against recent runs (what-if)",
        icon: Activity,
        tier: "advanced",
        requiredAuthority: "ReadAuthority",
      },
      {
        href: "/alert-tuning",
        label: "Alert tuning",
        title: "Alert tuning — threshold recommendations from simulation",
        icon: Wrench,
        tier: "advanced",
        requiredAuthority: "ReadAuthority",
      },
      {
        href: "/policy-packs",
        label: "Policy packs",
        title: "Policy packs — versions, effective content, and assignments",
        icon: Shield,
        tier: "extended",
        requiredAuthority: "ReadAuthority",
      },
      {
        href: "/governance-resolution",
        label: "Governance resolution",
        title: "Governance resolution — effective policy for this scope (read view)",
        icon: Scale,
        tier: "extended",
        requiredAuthority: "ReadAuthority",
      },
      {
        href: "/governance/dashboard",
        label: "Dashboard",
        title: navTitleWithShortcut(
          "Governance dashboard — cross-run approvals, decisions, and policy signals (governance operators)",
          "alt+g",
        ),
        keyShortcut: "alt+g",
        icon: LayoutDashboard,
        tier: "extended",
        requiredAuthority: "ReadAuthority",
      },
      {
        href: "/governance",
        label: "Governance workflow",
        title: "Governance workflow — approvals, promotions, and environment activation",
        icon: GitBranch,
        tier: "advanced",
        requiredAuthority: "ExecuteAuthority",
      },
      {
        href: "/audit",
        label: "Audit log",
        title: "Audit log — search and export scoped audit events",
        icon: FileSearch,
        tier: "advanced",
        requiredAuthority: "ReadAuthority",
      },
    ],
  },
];

/**
 * Flat list of configured nav links (sidebar + palette source of truth).
 * Shell UIs use **`listNavGroupsVisibleInOperatorShell`** (tier → authority, omit empty groups); per-link filtering is **`filterNavLinksForOperatorShell`**.
 */
export function flattenNavLinks(): NavLinkItem[] {
  return NAV_GROUPS.flatMap((g) => g.links);
}
