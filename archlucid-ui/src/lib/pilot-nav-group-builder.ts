import {
  AlertCircle,
  BarChart3,
  ClipboardList,
  Home,
  LifeBuoy,
  ListOrdered,
  Rocket,
} from "lucide-react";

import type { NavGroupConfig } from "@/lib/nav-config.types";

import { NavGroupBuilderBase } from "@/lib/nav-group-builder-base";

/** Pilot layer — default authenticated path; essentials omit `requiredAuthority` where invariant requires it. */
export class PilotNavGroupBuilder extends NavGroupBuilderBase {
  build(): NavGroupConfig {
    return {
      id: "pilot",
      label: "Pilot",
      surface: "review-workflow",
      caption: "Start a review — upload evidence, track progress, and review findings.",
      links: [
        {
          href: "/",
          label: "Home",
          title: this.shortcutTitle("Home — V1 checklist and quick links", "alt+h"),
          keyShortcut: "alt+h",
          icon: Home,
          tier: "essential",
          defaultVisibleInCollapsedSidebar: true,
        },
        {
          href: "/onboarding",
          label: "Onboarding",
          title: "Onboarding — checklist and milestones",
          tier: "essential",
          icon: ClipboardList,
          defaultVisibleInCollapsedSidebar: true,
        },
        {
          href: "/reviews/new",
          label: "New review",
          title: this.shortcutTitle(
            "Start a new architecture review — guided wizard through pipeline tracking",
            "alt+n",
          ),
          keyShortcut: "alt+n",
          icon: Rocket,
          tier: "essential",
          defaultVisibleInCollapsedSidebar: true,
        },
        {
          href: "/reviews?projectId=default",
          label: "Reviews",
          title: this.shortcutTitle("Reviews — open review detail, architecture package, artifacts, exports", "alt+r"),
          keyShortcut: "alt+r",
          icon: ListOrdered,
          tier: "essential",
          defaultVisibleInCollapsedSidebar: true,
        },
        {
          href: "/governance/findings",
          label: "Findings",
          title: this.shortcutTitle(
            "Findings — open risks from completed reviews, severity and recommended actions",
            "alt+f",
          ),
          keyShortcut: "alt+f",
          icon: AlertCircle,
          // extended so ReadAuthority does not break Pilot-essential invariant
          // (nav-config.structure.test.ts §"keeps requiredAuthority unset on Pilot essential-tier links").
          tier: "extended",
          requiredAuthority: "ReadAuthority",
        },
        {
          href: "/help",
          label: "Help",
          title: "Help — using ArchLucid and reference documentation",
          icon: LifeBuoy,
          tier: "essential",
          defaultVisibleInCollapsedSidebar: true,
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
    };
  }
}
