import {
  Activity,
  BarChart3,
  ClipboardList,
  FileSearch,
  FileText,
  GitBranch,
  GitCompare,
  GitGraph,
  LineChart,
  MessageSquare,
  Play,
  Search,
  Sparkles,
} from "lucide-react";

import type { NavGroupConfig } from "@/lib/nav-config.types";

import { NavGroupBuilderBase } from "@/lib/nav-group-builder-base";

/** Operate · analysis — every link sets `requiredAuthority`. */
export class OperateAnalysisNavGroupBuilder extends NavGroupBuilderBase {
  build(): NavGroupConfig {
    return {
      id: "operate-analysis",
      label: "Analysis",
      surface: "review-workflow",
      caption: "Compare, replay, graph, architecture advisory, and deeper questions after Pilot proof.",
      links: [
        {
          href: "/graph",
          label: "Graph",
          title: this.shortcutTitle("Review-trail or architecture graph for one review", "alt+y"),
          keyShortcut: "alt+y",
          icon: GitGraph,
          tier: "extended",
          requiredAuthority: "ReadAuthority",
        },
        {
          href: "/compare",
          label: "Compare two reviews",
          title: this.shortcutTitle("Diff two reviews (base vs target)", "alt+c"),
          keyShortcut: "alt+c",
          icon: GitCompare,
          tier: "extended",
          requiredAuthority: "ReadAuthority",
        },
        {
          href: "/replay",
          label: "Replay a review",
          title: this.shortcutTitle("Replay a review — re-validate stored pipeline output", "alt+p"),
          keyShortcut: "alt+p",
          icon: Play,
          tier: "extended",
          requiredAuthority: "ExecuteAuthority",
        },
        {
          href: "/ask",
          label: "Ask",
          title: this.shortcutTitle("Ask — natural language Q&A over architecture context", "alt+a"),
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
          href: "/value-report/pilot",
          label: "Pilot value report",
          title: "Pilot value report — committed-run metrics, governance signals, Markdown export",
          icon: FileText,
          tier: "extended",
          requiredAuthority: "ReadAuthority",
        },
        {
          href: "/value-report/roi",
          label: "ROI report",
          title: "ROI report — hours estimate from severities and pre-commit audit blocks",
          icon: LineChart,
          tier: "extended",
          requiredAuthority: "ReadAuthority",
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
    };
  }
}
