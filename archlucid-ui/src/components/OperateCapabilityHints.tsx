"use client";

import type { ReactNode } from "react";

import { useNavCallerAuthorityRank } from "@/components/OperatorNavAuthorityProvider";
import {
  alertOperatorToolingOperatorRankLine,
  alertOperatorToolingReaderRankLine,
  alertsInboxRankOperatorLine,
  alertsInboxRankReaderLine,
  auditLogRankOperatorLine,
  auditLogRankReaderLine,
  enterpriseExecutePageHintReaderRank,
  enterpriseNavHintOperatorRank,
  enterpriseNavHintReaderRank,
  governanceDashboardReaderActionLine,
  governanceResolutionRankOperatorLine,
  governanceResolutionRankReaderLine,
} from "@/lib/enterprise-controls-context-copy";
import { AUTHORITY_RANK } from "@/lib/nav-authority";
import { cn } from "@/lib/utils";

const pageCueClassName =
  "mb-2 max-w-3xl text-xs leading-snug text-neutral-600 dark:text-neutral-400";

/**
 * Second line under the **Operate** nav group caption (governance slice — sidebar + mobile drawer).
 * Explains omission for readers vs responsibility framing for operator+.
 */
export function OperateCapabilityNavGroupHint(): ReactNode {
  const rank = useNavCallerAuthorityRank();

  const text =
    rank < AUTHORITY_RANK.ExecuteAuthority ? enterpriseNavHintReaderRank : enterpriseNavHintOperatorRank;

  if (text.length === 0) {
    return null;
  }

  return (
    <span className="mt-0.5 block max-w-[14rem] text-[10px] font-normal normal-case leading-snug tracking-normal text-neutral-500 dark:text-neutral-500">
      {text}
    </span>
  );
}

export type OperateExecutePageHintProps = {
  className?: string;
};

/**
 * One line for alert/governance **mutation** pages when the resolved principal is below Execute
 * (e.g. Reader bookmarked the URL). Hidden for operator/admin to avoid clutter.
 */
export function OperateExecutePageHint({ className }: OperateExecutePageHintProps): ReactNode {
  const rank = useNavCallerAuthorityRank();

  if (rank >= AUTHORITY_RANK.ExecuteAuthority) {
    return null;
  }

  return (
    <p className={cn(pageCueClassName, className)} role="note">
      {enterpriseExecutePageHintReaderRank}
    </p>
  );
}

/**
 * Governance resolution: rank-aware second line (read evidence vs where operators change policy).
 */
export function GovernanceResolutionRankCue({ className }: { className?: string }): ReactNode {
  const rank = useNavCallerAuthorityRank();

  const text =
    rank < AUTHORITY_RANK.ExecuteAuthority ? governanceResolutionRankReaderLine : governanceResolutionRankOperatorLine;

  return <p className={cn(pageCueClassName, className)} role="note">{text}</p>;
}

/**
 * Alerts inbox: reader view vs operator triage (mutations still API-gated).
 */
export function AlertsInboxRankCue({ className }: { className?: string }): ReactNode {
  const rank = useNavCallerAuthorityRank();

  const text = rank < AUTHORITY_RANK.ExecuteAuthority ? alertsInboxRankReaderLine : alertsInboxRankOperatorLine;

  return <p className={cn(pageCueClassName, className)} role="note">{text}</p>;
}

/**
 * Audit log: reader evidence framing vs operator investigation framing.
 */
export function AuditLogRankCue({ className }: { className?: string }): ReactNode {
  const rank = useNavCallerAuthorityRank();

  const text = rank < AUTHORITY_RANK.ExecuteAuthority ? auditLogRankReaderLine : auditLogRankOperatorLine;

  return <p className={cn(pageCueClassName, className)} role="note">{text}</p>;
}

/**
 * Alert rules, routing, simulation, tuning, composite rules: one rank-aware line (read vs operator/admin framing).
 */
export function AlertOperatorToolingRankCue({ className }: { className?: string }): ReactNode {
  const rank = useNavCallerAuthorityRank();

  const text =
    rank < AUTHORITY_RANK.ExecuteAuthority ? alertOperatorToolingReaderRankLine : alertOperatorToolingOperatorRankLine;

  return <p className={cn(pageCueClassName, className)} role="note">{text}</p>;
}

/**
 * Governance dashboard: clarifies that in-product approvals still need execute on the API when rank is below operator.
 */
export function GovernanceDashboardReaderActionCue({ className }: { className?: string }): ReactNode {
  const rank = useNavCallerAuthorityRank();

  if (rank >= AUTHORITY_RANK.ExecuteAuthority) {
    return null;
  }

  return <p className={cn(pageCueClassName, className)} role="note">{governanceDashboardReaderActionLine}</p>;
}

export type OperateExecutePlusPageCueProps = {
  /** One line from `enterprise-controls-context-copy` */
  message: string;
  className?: string;
};

/**
 * Single muted line for operator/admin visitors on mutation-heavy Operate pages (hidden for Reader to avoid stacking with `OperateExecutePageHint`).
 */
export function OperateExecutePlusPageCue({ message, className }: OperateExecutePlusPageCueProps): ReactNode {
  const rank = useNavCallerAuthorityRank();

  if (rank < AUTHORITY_RANK.ExecuteAuthority) {
    return null;
  }

  return <p className={cn(pageCueClassName, className)} role="note">{message}</p>;
}
