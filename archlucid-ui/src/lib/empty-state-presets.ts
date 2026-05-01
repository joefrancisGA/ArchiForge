import { BarChart3, Bell, FileText, GitCompareArrows, Network, Shield } from "lucide-react";

import type { EmptyStateProps } from "@/components/EmptyState";

export { SEARCH_EMPTY } from "./search-empty-preset";

export const RUNS_EMPTY: EmptyStateProps = {
  icon: FileText,
  title: "No architecture runs yet",
  description:
    "Create a request to generate your first architecture manifest, surfaced findings, and exportable artifact bundle. You can also submit via the CLI or API.",
  actions: [
    { label: "Create request", href: "/reviews/new" },
    { label: "Getting started", href: "/getting-started", variant: "outline" },
  ],
  helpTopicPath: "creating-runs",
};

export const ALERTS_EMPTY_FILTERED: EmptyStateProps = {
  icon: Bell,
  title: "No alerts for this filter",
  description:
    "Try All or another status, or refresh after a scan window. New alerts appear when scheduled architecture-risk checks fire and dedupe rules allow a row.",
  actions: [
    { label: "Configure alert rules", href: "/alerts?tab=rules" },
    { label: "View reviews list", href: "/reviews?projectId=default", variant: "outline" },
  ],
  helpTopicPath: "alerts",
};

export const GRAPH_IDLE: EmptyStateProps = {
  icon: Network,
  title: "No graph on screen yet",
  description:
    "Choose a review above, keep Review trail graph selected for the default story, then use Load graph.",
  actions: [{ label: "View reviews list", href: "/reviews?projectId=default", variant: "outline" }],
};

export const COMPARE_WAITING: EmptyStateProps = {
  icon: GitCompareArrows,
  title: "Waiting for both review IDs",
  description:
    "Enter a base and target review ID before comparing. Query parameters leftRunId and rightRunId prefill these fields. Get IDs from Reviews or the Compare shortcut on review detail.",
  actions: [{ label: "View reviews list", href: "/reviews?projectId=default", variant: "outline" }],
};

export const PLANNING_EMPTY: EmptyStateProps = {
  icon: BarChart3,
  title: "No themes or plans in this scope yet",
  description:
    "When 59R themes and improvement plans are persisted for the current tenant / workspace / project, they will appear here. Scope follows the operator shell defaults unless you configure proxy scope overrides.",
  actions: [{ label: "View pilot feedback", href: "/product-learning", variant: "outline" }],
};

export const GOVERNANCE_WORKFLOW_IDLE: EmptyStateProps = {
  icon: Shield,
  title: "Load a review to see workflow rows",
  description:
    "Under Approval requests for this review, choose or type a review id, then click Load to fetch approval requests, promotions, and activations.",
  actions: [
    { label: "Governance findings", href: "/governance/findings", variant: "outline" },
    { label: "View reviews list", href: "/reviews?projectId=default", variant: "outline" },
  ],
  helpTopicPath: "governance",
};

/** Idle state when the principal is below Execute: inspection-first copy (mutations stay API-gated). */
export const GOVERNANCE_WORKFLOW_IDLE_READER: EmptyStateProps = {
  icon: Shield,
  title: "Inspect review-scoped workflow",
  description:
    "Under Approval requests for this review, choose or type a review and click Load to review approvals, promotions, and activations. Submitting, reviewing, promoting, or activating requires operator-level access where your tenant expects it.",
  actions: [
    { label: "Governance findings", href: "/governance/findings", variant: "outline" },
    { label: "View reviews list", href: "/reviews?projectId=default", variant: "outline" },
  ],
  helpTopicPath: "governance",
};
