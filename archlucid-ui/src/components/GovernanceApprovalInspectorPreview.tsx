"use client";

import Link from "next/link";

import { StatusPill } from "@/components/StatusPill";
import { Button } from "@/components/ui/button";
import { formatIsoUtcForDisplay } from "@/lib/format-iso-utc";
import { formatRelativeTime } from "@/lib/relative-time";
import type { GovernanceApprovalRequest } from "@/types/governance-workflow";

export type GovernanceApprovalInspectorPreviewProps = {
  request: GovernanceApprovalRequest;
};

/** One-line title for tables and inspector chrome (environments only — no invented fields). */
export function approvalRequestPrimaryLabel(row: GovernanceApprovalRequest): string {
  return `${row.sourceEnvironment} → ${row.targetEnvironment}`;
}

/**
 * Read-only approval request summary for the governance dashboard inspector (dashboard payload only).
 */
export function GovernanceApprovalInspectorPreview({ request }: GovernanceApprovalInspectorPreviewProps) {
  const requestedLabel = formatIsoUtcForDisplay(request.requestedUtc);
  const reviewedUtcRaw = request.reviewedUtc;
  const reviewedLabel =
    reviewedUtcRaw !== null && reviewedUtcRaw.length > 0 ? formatIsoUtcForDisplay(reviewedUtcRaw) : null;

  return (
    <div
      className="space-y-4 text-sm text-neutral-800 dark:text-neutral-200"
      data-testid="governance-approval-inspector-preview"
    >
      <div className="flex flex-wrap items-center gap-2">
        <StatusPill status={request.status} domain="governance" ariaLabel={`Governance status: ${request.status}`} />
      </div>

      <dl className="m-0 grid gap-2 sm:grid-cols-[minmax(5rem,auto)_1fr] sm:gap-x-3">
        <dt className="text-xs font-medium uppercase tracking-wide text-neutral-500 dark:text-neutral-400">Run</dt>
        <dd className="m-0 min-w-0">
          <Link
            href={`/reviews/${encodeURIComponent(request.runId)}`}
            className="break-all font-mono text-xs font-medium text-teal-800 underline dark:text-teal-300"
          >
            {request.runId}
          </Link>
        </dd>
        <dt className="text-xs font-medium uppercase tracking-wide text-neutral-500 dark:text-neutral-400">
          Manifest
        </dt>
        <dd className="m-0 font-mono text-xs">{request.manifestVersion}</dd>
        <dt className="text-xs font-medium uppercase tracking-wide text-neutral-500 dark:text-neutral-400">
          Requested
        </dt>
        <dd className="m-0" title={requestedLabel}>
          <span className="block">{formatRelativeTime(request.requestedUtc)}</span>
          <span className="mt-0.5 block text-xs text-neutral-500 dark:text-neutral-400">{requestedLabel}</span>
        </dd>
        <dt className="text-xs font-medium uppercase tracking-wide text-neutral-500 dark:text-neutral-400">
          Requested by
        </dt>
        <dd className="m-0">{request.requestedBy}</dd>
        {reviewedLabel !== null && reviewedUtcRaw !== null ? (
          <>
            <dt className="text-xs font-medium uppercase tracking-wide text-neutral-500 dark:text-neutral-400">
              Reviewed
            </dt>
            <dd className="m-0" title={reviewedLabel}>
              <span className="block">{formatRelativeTime(reviewedUtcRaw)}</span>
              <span className="mt-0.5 block text-xs text-neutral-500 dark:text-neutral-400">{reviewedLabel}</span>
            </dd>
          </>
        ) : null}
        {request.reviewedBy !== null && request.reviewedBy.length > 0 ? (
          <>
            <dt className="text-xs font-medium uppercase tracking-wide text-neutral-500 dark:text-neutral-400">
              Reviewed by
            </dt>
            <dd className="m-0">{request.reviewedBy}</dd>
          </>
        ) : null}
        {request.requestComment !== null && request.requestComment.trim().length > 0 ? (
          <>
            <dt className="text-xs font-medium uppercase tracking-wide text-neutral-500 dark:text-neutral-400">
              Request comment
            </dt>
            <dd className="m-0 whitespace-pre-wrap">{request.requestComment}</dd>
          </>
        ) : null}
        {request.reviewComment !== null && request.reviewComment.trim().length > 0 ? (
          <>
            <dt className="text-xs font-medium uppercase tracking-wide text-neutral-500 dark:text-neutral-400">
              Review comment
            </dt>
            <dd className="m-0 whitespace-pre-wrap">{request.reviewComment}</dd>
          </>
        ) : null}
      </dl>

      <div className="border-t border-neutral-200 pt-3 dark:border-neutral-700">
        <Button size="sm" variant="outline" className="w-full sm:w-auto" asChild>
          <Link href={`/governance/approval-requests/${encodeURIComponent(request.approvalRequestId)}/lineage`}>
            Open lineage
          </Link>
        </Button>
      </div>
    </div>
  );
}
