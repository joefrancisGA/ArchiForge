import type { LucideIcon } from "lucide-react";
import {
  Activity,
  AlertCircle,
  BarChart3,
  Bell,
  Building2,
  ClipboardList,
  FileSearch,
  FileText,
  GitBranch,
  GitCompare,
  GitGraph,
  HeartPulse,
  Home,
  LifeBuoy,
  ListOrdered,
  MessageSquare,
  Play,
  Rocket,
  Scale,
  Search,
  Shield,
  ShieldCheck,
  Sparkles,
  Users,
  Wallet,
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
   * **Pilot essentials** omit this (broad default path). **Operate** nav links in `NAV_GROUPS` set it — see the module **Authority** section.
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
 * **Two shaping surfaces (this file owns Visibility metadata only):** (1) **Visibility** — `tier` + `requiredAuthority`
 * here + **`nav-shell-visibility.ts`** + **`useNavSurface().layerGuidance`** / **`LayerHeader`**; (2) **Capability** —
 * **`useOperateCapability()`** + **`OperateCapabilityHints`** (Execute+ floor). Enumeration: **docs/PRODUCT_PACKAGING.md** §3.
 *
 * Nav groups map to buyer layers (see docs/PRODUCT_PACKAGING.md):
 *   pilot              → Pilot    (request · run · finalize · review)
 *   operate-analysis   → Operate  (analysis slice — compare, replay, graph, Q&A, advisory, …)
 *   operate-governance → Operate  (governance slice — policy, audit, alerts, trust — Execute+ for writes where noted)
 *
 * **Drift guard:** When adding or moving a route, follow the **ordered checklist** in **docs/PRODUCT_PACKAGING.md** §3
 *   *Contributor drift guard* (API policy → this file → `layer-guidance` / `LayerHeader` → **`useOperateCapability`** →
 *   packaging doc). Verify **C#** `[Authorize(Policy = …)]` still matches each link’s **`requiredAuthority`** string.
 *   **Cross-module Vitest:** `authority-seam-regression.test.ts` — e.g. **`/governance`** must stay **`ExecuteAuthority`**
 *   so Reader-ranked callers do not see it under Operate nav (deep-link still hits API policy); every **`ExecuteAuthority`**
 *   row under **`operate-analysis`** and **`operate-governance`** stays absent from Read-tier filtered nav; Pilot essential
 *   hrefs stay visible for Reader with default tier toggles; **caller rank `0`** stays stricter than Read for **`ReadAuthority`** links;
 *   **`/alerts`** stays **`essential`** tier; filtered link order and **`listNavGroupsVisibleInOperatorShell`** group order stay aligned with this file;
 *   Operate governance href sets grow **monotonically** Read→Execute→Admin under **`filterNavLinksByAuthority`** alone; default Reader shell keeps **Operate analysis** to **`/ask`** only (tier before authority); **`/governance`** appears only when **extended and advanced** are on for **Execute** rank (**`filterNavLinksForOperatorShell`**). **`OperatorNavAuthorityProvider.test.tsx`** —
 *   **`useNavCallerAuthorityRank`** stays Read during JWT **`/me`** refetch so stale Execute rank does not flash in nav or hooks.
 *   **`OperateCapabilityHints.authority.test.tsx`** — rank-gated Operate sidebar/page cues share the same
 *   **`ExecuteAuthority`** numeric floor as **`useOperateCapability`** (governance resolution, audit log, **Alerts inbox**, **governance
 *   dashboard** reader cue, alert tooling). **`authority-execute-floor-regression.test.ts`** — same **boolean** for a synthetic
 *   **`ExecuteAuthority`** row vs **`enterpriseMutationCapabilityFromRank`**; **`authority-shaped-ui-regression.test.ts`** —
 *   every catalog **`ExecuteAuthority`** link hidden at Read / visible at Execute (new rows cannot drift untested).
 *   **`operate-governance`** monotonicity Reader→Admin.
 *   **`nav-shell-visibility.test.ts`** also locks **Analysis** extended **Execute**
 *   links (e.g. **`/replay`**) behind **Show more** — tier before rank. **`current-principal.test.ts`** locks **`maxAuthority`**
 *   vs **`requiredAuthorityFromRank`** and **`hasEnterpriseOperatorSurfaces`** vs mutation capability.
 *   **`nav-config.structure.test.ts`** — duplicate **`href`**s; **Pilot** essentials omit **`requiredAuthority`**;
 *   **Operate** **`ExecuteAuthority`** links must not use **`essential`** tier (progressive disclosure + rank story).
 *   **`authority-shaped-layout-regression.test.tsx`** — **inspect-first** DOM when mutation hook is false (parallel to tier→authority story; still **UI only**).
 *
 * **`layer-guidance.ts` / `LayerHeader`:** **Operate · governance** route families use **`LAYER_PAGE_GUIDANCE`** rows with **`enterpriseFootnote`**
 * (see **`authority-seam-regression.test.ts`** — Operate analysis vs governance footnote contract). That strip is **cognitive packaging only**;
 * it does not replace **`requiredAuthority`** here or **`[Authorize]`** on the API.
 *
 * **`requiredAuthority` vs Operate POSTs:** this field shapes **nav / palette visibility** after tier filtering only
 * (higher **caller rank** does **not** bypass **`tier`** — e.g. Operate **extended** hrefs stay hidden until “Show more”;
 * **`nav-shell-visibility.test.ts`**). In-page **POST / toggle** soft-enable on Operate-heavy routes uses
 * **`useOperateCapability()`** (or deprecated **`useEnterpriseMutationCapability()`**) — same **`AUTHORITY_RANK.ExecuteAuthority`** floor as **`ExecuteAuthority`**
 * links here; keep both aligned with C# policies. **Audit CSV export** is a documented exception: gated on **`/me`** roles (**Auditor** or **Admin**) on the audit page, not this nav field alone.
 *
 * **Authority (`requiredAuthority`) — first-pass map (UI hint only; API still 401/403):**
 *
 * - **Omit** on Pilot *essentials* (home, getting-started, new run, runs) so Reader-signed-in pilots keep the default path.
 * - **Analysis · extended:** inspection/diff surfaces that are `ReadAuthority` on the API (`GraphController`,
 *   `AuthorityCompareController`) use **`ReadAuthority`**. **Replay** stays **`ExecuteAuthority`**
 *   (`AuthorityReplayController`).
 * - **Operate · analysis (`operate-analysis`):** every link sets **`requiredAuthority`**. Read/analytics pages → **`ReadAuthority`** unless the
 *   API primary workflow is Execute-class (planning, evolution candidates; advisory **schedules** and digest **subscriptions** are hub tabs under **`/advisory`** and **`/digests`** with in-page Execute gating).
 *   Link `title` strings use **“Label — short description”** for tooltips (same convention as governance slice).
 * - **Operate · governance (`operate-governance`):** **inbox / dashboards / audit / policy pack browsing / alert tooling** whose controllers
 *   are class-scoped **`ReadAuthority`** → **`ReadAuthority`**. **`/admin/health`** omits **`requiredAuthority`** so any authenticated session sees diagnostics (owner 2026-04-25). **Governance workflow** (mutations) → **`ExecuteAuthority`**.
 *   Do not use **`AdminAuthority`** on nav entries: Admin-only actions (e.g. policy pack create) are enforced on POST;
 *   the UI page is still reachable at Read for list/effective views.
 *
 * ### UI shaping vs API authorization (boundary)
 *
 * **`[Authorize(Policy = …)]`** on **ArchLucid.Api** is authoritative (**401/403**) for every route and POST — always.
 * `requiredAuthority` drives **shell visibility** after **`nav-shell-visibility`** tier filtering — not whether HTTP writes
 * succeed. Keep policy **names** aligned
 * with C# when moving routes. **Vitest:** `nav-config.structure.test.ts` (graph invariants); **`authority-execute-floor-regression.test.ts`**
 * (Execute-class nav row vs mutation capability; Operate **`operate-governance`** Reader vs Execute href sets); **`src/app/(operator)/operate-authority-ui-shaping.test.tsx`**
 * (representative Operate pages: **`useOperateCapability`** → **`disabled`** on primary actions).
 *
 * Omitting `requiredAuthority` is used only for **Pilot essentials** (default path for any authenticated rank).
 * Every **Operate** nav link in this file sets `requiredAuthority`. Composed with tiers in `@/lib/nav-shell-visibility`.
 *
 * Group IDs are intentionally stable (used as localStorage keys); only labels are user-visible.
 */
export const NAV_GROUPS: NavGroupConfig[] = [
  {
    id: "pilot",
    // Buyer layer: Pilot
    label: "Pilot",
    caption: "Start a review — upload evidence, track progress, and review findings.",
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
        href: "/runs/new",
        label: "New review",
        title: navTitleWithShortcut(
          "Start a new architecture review — guided wizard through pipeline tracking",
          "alt+n",
        ),
        keyShortcut: "alt+n",
        icon: Rocket,
        tier: "essential",
      },
      {
        href: "/runs?projectId=default",
        label: "Reviews",
        title: navTitleWithShortcut("Reviews — open review detail, architecture package, artifacts, exports", "alt+r"),
        keyShortcut: "alt+r",
        icon: ListOrdered,
        tier: "essential",
      },
      {
        href: "/governance/findings",
        label: "Findings",
        title: navTitleWithShortcut("Findings — open risks from completed reviews, severity and recommended actions", "alt+f"),
        keyShortcut: "alt+f",
        icon: AlertCircle,
        // extended so the ReadAuthority requirement does not break the Pilot-essential invariant
        // (nav-config.structure.test.ts §"keeps requiredAuthority unset on Pilot essential-tier links").
        // Findings appears by default after the first review is finalized and the user clicks "Show more".
        tier: "extended",
        requiredAuthority: "ReadAuthority",
      },
      {
        href: "/help",
        label: "Help",
        title: "Help — using ArchLucid and reference documentation",
        icon: LifeBuoy,
        tier: "essential",
      },
      {
        href: "/scorecard",
        label: "Scorecard",
        title: "Pilot scorecard — committed-run metrics and ROI baselines",
        icon: BarChart3,
        tier: "extended",
        requiredAuthority: "ReadAuthority",
      },
    ],
  },
  {
    id: "operate-analysis",
    label: "Analysis",
    caption: "Compare, replay, graph, architecture advisory, and deeper questions after Pilot proof.",
    links: [
      {
        href: "/graph",
        label: "Graph",
        title: navTitleWithShortcut("Review-trail or architecture graph for one run", "alt+y"),
        keyShortcut: "alt+y",
        icon: GitGraph,
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
        title: navTitleWithShortcut("Replay a run — re-validate stored pipeline output", "alt+p"),
        keyShortcut: "alt+p",
        icon: Play,
        tier: "extended",
        requiredAuthority: "ExecuteAuthority",
      },
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
        label: "Architecture advisory",
        title: "Architecture advisory — architecture scans and scan schedules",
        icon: Activity,
        tier: "extended",
        requiredAuthority: "ReadAuthority",
      },
      {
        href: "/recommendation-learning",
        label: "Recommendation tuning",
        title: "Recommendation tuning — profiles and ranking signals",
        icon: Sparkles,
        tier: "advanced",
        requiredAuthority: "ReadAuthority",
      },
      {
        href: "/product-learning",
        label: "Pilot feedback",
        title: "Pilot feedback — rollups and triage (58R)",
        icon: ClipboardList,
        tier: "advanced",
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
        href: "/digests",
        label: "Digests",
        title: "Digests — generated digests, subscriptions, and sponsor schedule",
        icon: FileSearch,
        tier: "advanced",
        requiredAuthority: "ReadAuthority",
      },
    ],
  },
  {
    id: "operate-governance",
    label: "Governance",
    caption: "Policy, audit, alerts, and trust controls.",
    links: [
      {
        href: "/admin/health",
        label: "System health",
        title: "System health — readiness, circuit breakers, onboarding funnel metrics",
        icon: HeartPulse,
        tier: "essential",
      },
      {
        href: "/alerts",
        label: "Alerts",
        title: navTitleWithShortcut("Alerts — inbox, rules, routing, simulation, and tuning", "alt+l"),
        keyShortcut: "alt+l",
        icon: Bell,
        tier: "essential",
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
      {
        href: "/workspace/security-trust",
        label: "Security & trust",
        title: "Security & trust — published assessments, CAIQ/SIG, trust-center links",
        icon: ShieldCheck,
        tier: "extended",
        requiredAuthority: "ReadAuthority",
      },
      {
        href: "/integrations/teams",
        label: "Teams notifications",
        title: "Teams notifications — Key Vault reference for incoming webhook fan-out",
        icon: MessageSquare,
        tier: "extended",
        requiredAuthority: "ReadAuthority",
      },
      {
        href: "/value-report",
        label: "Value report",
        title: "Value report — sponsor DOCX from ROI_MODEL-aligned tenant metrics",
        icon: FileText,
        tier: "advanced",
        requiredAuthority: "ExecuteAuthority",
      },
    ],
  },
  {
    id: "operator-admin",
    label: "Admin",
    caption: "Tenant cost, settings, support bundles, and user administration.",
    links: [
      {
        href: "/settings/tenant-cost",
        label: "Tenant cost",
        title: "Tenant cost — estimated monthly spend band (Standard+)",
        icon: Wallet,
        tier: "extended",
        requiredAuthority: "ReadAuthority",
      },
      {
        href: "/settings/baseline",
        label: "Baseline settings",
        title: "Baseline settings — ROI measurement inputs",
        icon: BarChart3,
        tier: "extended",
        requiredAuthority: "ExecuteAuthority",
      },
      {
        href: "/settings/tenant",
        label: "Tenant settings",
        title: "Tenant settings — trial, digest email, and request scope",
        icon: Building2,
        tier: "extended",
        requiredAuthority: "ExecuteAuthority",
      },
      {
        href: "/admin/support",
        label: "Support",
        title: "Support — download a redacted support bundle for tickets",
        icon: LifeBuoy,
        tier: "extended",
        requiredAuthority: "ExecuteAuthority",
      },
      {
        href: "/admin/users",
        label: "Users & roles",
        title: "Users & roles — directory and authority rank (administration UI; API policies still enforce writes)",
        icon: Users,
        tier: "extended",
        requiredAuthority: "AdminAuthority",
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
