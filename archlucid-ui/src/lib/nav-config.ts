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

import type { NavTier } from "@/lib/nav-tier";

export type NavLinkItem = {
  href: string;
  label: string;
  title: string;
  /** Progressive disclosure: essential always; extended after “Show more”; advanced after gear toggle. */
  tier: NavTier;
  /** Registry combo for `aria-keyshortcuts`, e.g. `alt+n` */
  keyShortcut?: string;
  /** Optional icon for sidebar and mobile drawer. */
  icon?: LucideIcon;
};

export type NavGroupConfig = {
  id: string;
  label: string;
  links: NavLinkItem[];
};

function navTitleWithShortcut(baseTitle: string, registryCombo: string): string {
  const aria = registryKeyToAriaKeyShortcuts(registryCombo);

  return `${baseTitle} (${aria})`;
}

/** Canonical operator shell navigation — sidebar, command palette, and mobile drawer. */
export const NAV_GROUPS: NavGroupConfig[] = [
  {
    id: "runs-review",
    label: "Runs & review",
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
        tier: "extended",
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
        tier: "essential",
      },
      {
        href: "/compare",
        label: "Compare two runs",
        title: navTitleWithShortcut("Diff two runs (base vs target)", "alt+c"),
        keyShortcut: "alt+c",
        icon: GitCompare,
        tier: "extended",
      },
      {
        href: "/replay",
        label: "Replay a run",
        title: navTitleWithShortcut("Re-validate authority chain for one run", "alt+p"),
        keyShortcut: "alt+p",
        icon: Play,
        tier: "extended",
      },
    ],
  },
  {
    id: "qa-advisory",
    label: "Q&A & advisory",
    links: [
      {
        href: "/ask",
        label: "Ask",
        title: navTitleWithShortcut("Natural language ask against architecture context", "alt+a"),
        keyShortcut: "alt+a",
        icon: MessageSquare,
        tier: "essential",
      },
      { href: "/search", label: "Search", title: "Search indexed architecture content", icon: Search, tier: "advanced" },
      { href: "/advisory", label: "Advisory", title: "Advisory scans and architecture digests", icon: Activity, tier: "extended" },
      {
        href: "/recommendation-learning",
        label: "Recommendation learning",
        title: "Recommendation learning profiles",
        icon: Sparkles,
        tier: "extended",
      },
      {
        href: "/product-learning",
        label: "Pilot feedback",
        title: "Pilot feedback rollups and triage (58R)",
        icon: ClipboardList,
        tier: "extended",
      },
      { href: "/planning", label: "Planning", title: "Improvement themes and prioritized plans (59R)", icon: BarChart3, tier: "advanced" },
      {
        href: "/evolution-review",
        label: "Evolution candidates",
        title: "Candidate simulations and before/after review (60R)",
        icon: GitBranch,
        tier: "advanced",
      },
      { href: "/advisory-scheduling", label: "Schedules", title: "Advisory scan schedules", icon: Wrench, tier: "advanced" },
      { href: "/digests", label: "Digests", title: "Architecture digests", icon: FileSearch, tier: "advanced" },
      { href: "/digest-subscriptions", label: "Subscriptions", title: "Digest email subscriptions", icon: Mail, tier: "advanced" },
    ],
  },
  {
    id: "alerts-governance",
    label: "Alerts & governance",
    links: [
      {
        href: "/alerts",
        label: "Alerts",
        title: navTitleWithShortcut("Open and acknowledged alerts", "alt+l"),
        keyShortcut: "alt+l",
        icon: Bell,
        tier: "essential",
      },
      { href: "/alert-rules", label: "Alert rules", title: "Configure alert rules", icon: Tags, tier: "advanced" },
      { href: "/alert-routing", label: "Alert routing", title: "Alert routing subscriptions", icon: Mail, tier: "advanced" },
      { href: "/composite-alert-rules", label: "Composite rules", title: "Composite alert rules", icon: Tags, tier: "advanced" },
      { href: "/alert-simulation", label: "Alert simulation", title: "Simulate alert evaluation", icon: Activity, tier: "advanced" },
      { href: "/alert-tuning", label: "Alert tuning", title: "Alert noise and threshold tuning", icon: Wrench, tier: "advanced" },
      { href: "/policy-packs", label: "Policy packs", title: "Policy packs and versions", icon: Shield, tier: "extended" },
      {
        href: "/governance-resolution",
        label: "Governance resolution",
        title: "Effective governance resolution",
        icon: Scale,
        tier: "extended",
      },
      {
        href: "/governance/dashboard",
        label: "Dashboard",
        title: navTitleWithShortcut(
          "Cross-run governance dashboard — pending approvals and policy changes",
          "alt+g",
        ),
        keyShortcut: "alt+g",
        icon: LayoutDashboard,
        tier: "extended",
      },
      {
        href: "/governance",
        label: "Governance workflow",
        title: "Approval, promotion, and activation workflow",
        icon: GitBranch,
        tier: "advanced",
      },
      { href: "/audit", label: "Audit log", title: "Search and filter audit events", icon: FileSearch, tier: "advanced" },
    ],
  },
];

/** Flat list for command palette search (value = href) — all tiers; progressive disclosure does not apply. */
export function flattenNavLinks(): NavLinkItem[] {
  return NAV_GROUPS.flatMap((g) => g.links);
}
