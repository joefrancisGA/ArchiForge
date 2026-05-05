import {
  Bell,
  FileSearch,
  FileText,
  GitBranch,
  MessageSquare,
  Scale,
  Shield,
  ShieldCheck,
} from "lucide-react";

import type { NavGroupConfig } from "@/lib/nav-config.types";

import { NavGroupBuilderBase } from "@/lib/nav-group-builder-base";

/** Operate · governance — Read-class hubs vs Execute workflow vs Admin health. */
export class OperateGovernanceNavGroupBuilder extends NavGroupBuilderBase {
  build(): NavGroupConfig {
    return {
      id: "operate-governance",
      label: "Governance",
      surface: "review-workflow",
      caption: "Policy, audit, alerts, and trust controls.",
      links: [
        {
          href: "/alerts",
          label: "Alerts",
          title: this.shortcutTitle("Alerts — inbox, rules, routing, simulation, and tuning", "alt+l"),
          keyShortcut: "alt+l",
          icon: Bell,
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
    };
  }
}
