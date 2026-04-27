"use client";

import { useCallback, useEffect, useState } from "react";

import Link from "next/link";
import { useParams } from "next/navigation";

import { OperatorApiProblem } from "@/components/OperatorApiProblem";
import { OperatorEmptyState } from "@/components/OperatorShellMessage";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import { Separator } from "@/components/ui/separator";
import { getApprovalRequestLineage } from "@/lib/api";
import type { ApiLoadFailureState } from "@/lib/api-load-failure";
import { toApiLoadFailure } from "@/lib/api-load-failure";
import { formatIsoUtcForDisplay } from "@/lib/format-iso-utc";
import type { GovernanceLineageResult } from "@/types/governance-dashboard";

export default function GovernanceApprovalLineagePage() {
  const params = useParams<{ id: string }>();
  const approvalRequestId = typeof params?.id === "string" ? params.id : "";

  const [data, setData] = useState<GovernanceLineageResult | null>(null);
  const [failure, setFailure] = useState<ApiLoadFailureState | null>(null);
  const [loading, setLoading] = useState(true);

  const load = useCallback(async () => {
    if (!approvalRequestId) {
      setLoading(false);
      return;
    }

    setLoading(true);
    setFailure(null);

    try {
      const result = await getApprovalRequestLineage(approvalRequestId);
      setData(result);
    } catch (e) {
      setData(null);
      setFailure(toApiLoadFailure(e));
    } finally {
      setLoading(false);
    }
  }, [approvalRequestId]);

  useEffect(() => {
    void load();
  }, [load]);

  if (!approvalRequestId) {
    return (
      <OperatorEmptyState title="Missing approval id">
        <p className="text-sm">
          Open this page from a governance link that includes the approval request id.
        </p>
      </OperatorEmptyState>
    );
  }

  if (loading) {
    return (
      <div className="space-y-4" aria-busy="true" aria-label="Loading lineage">
        <div className="h-8 w-64 animate-pulse rounded-md bg-neutral-200 dark:bg-neutral-700" />
        <div className="h-40 animate-pulse rounded-md bg-neutral-200 dark:bg-neutral-700" />
      </div>
    );
  }

  if (failure) {
    return (
      <div className="space-y-4">
        <div className="flex flex-wrap gap-2">
          <Button type="button" size="sm" variant="outline" onClick={() => void load()}>
            Retry
          </Button>
          <Button variant="outline" size="sm" asChild>
            <Link href="/governance/findings">Findings</Link>
          </Button>
        </div>
        <OperatorApiProblem
          problem={failure.problem}
          fallbackMessage={failure.message}
          correlationId={failure.correlationId}
        />
      </div>
    );
  }

  if (!data) {
    return (
      <OperatorEmptyState title="No data">
        <p className="text-sm">Lineage could not be loaded.</p>
      </OperatorEmptyState>
    );
  }

  const a = data.approvalRequest;

  return (
    <div className="space-y-6">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <h1 className="text-2xl font-semibold tracking-tight">Approval lineage</h1>
          <p className="text-sm text-muted-foreground">
            Request <span className="font-mono text-xs">{a.approvalRequestId}</span>
          </p>
        </div>
        <Button variant="outline" size="sm" asChild>
          <Link href="/governance/findings">Back to findings</Link>
        </Button>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>Approval</CardTitle>
          <CardDescription>Status and reviewer context</CardDescription>
        </CardHeader>
        <CardContent className="grid gap-2 text-sm">
          <div className="flex flex-wrap items-center gap-2">
            <span className="text-muted-foreground">Status</span>
            <Badge variant="secondary">{a.status}</Badge>
            {data.riskPosture ? (
              <>
                <span className="text-muted-foreground">Risk posture</span>
                <Badge variant="outline">{data.riskPosture}</Badge>
              </>
            ) : null}
          </div>
          <div>
            <span className="text-muted-foreground">Run</span>{" "}
            <Link
              className="font-mono text-xs underline-offset-4 hover:underline"
              href={`/runs/${encodeURIComponent(a.runId)}`}
            >
              {a.runId}
            </Link>
          </div>
          <div>
            Manifest <span className="font-mono">{a.manifestVersion}</span> · {a.sourceEnvironment} →{" "}
            {a.targetEnvironment}
          </div>
          <div className="text-muted-foreground">
            Requested {formatIsoUtcForDisplay(a.requestedUtc)} by {a.requestedBy}
            {a.reviewedUtc ? (
              <>
                {" "}
                · Reviewed {formatIsoUtcForDisplay(a.reviewedUtc)}
                {a.reviewedBy ? ` by ${a.reviewedBy}` : ""}
              </>
            ) : null}
          </div>
        </CardContent>
      </Card>

      {data.run ? (
        <Card>
          <CardHeader>
            <CardTitle>Coordinator run</CardTitle>
            <CardDescription>Architecture run summary</CardDescription>
          </CardHeader>
          <CardContent className="text-sm">
            <div>Status {data.run.status}</div>
            <div>Created {formatIsoUtcForDisplay(data.run.createdUtc)}</div>
            {data.run.completedUtc ? (
              <div>Completed {formatIsoUtcForDisplay(data.run.completedUtc)}</div>
            ) : null}
            {data.run.currentManifestVersion ? (
              <div>Current manifest {data.run.currentManifestVersion}</div>
            ) : null}
          </CardContent>
        </Card>
      ) : null}

      {data.manifest ? (
        <Card>
          <CardHeader>
            <CardTitle>Reviewed manifest</CardTitle>
            <CardDescription>When the run id maps to a finalized manifest in scope</CardDescription>
          </CardHeader>
          <CardContent className="grid gap-1 text-sm">
            <div>Version {data.manifest.manifestVersion ?? "—"}</div>
            <div>Decisions {data.manifest.decisionCount}</div>
            <div>Unresolved issues {data.manifest.unresolvedIssueCount}</div>
            <div>Compliance gaps {data.manifest.complianceGapCount}</div>
          </CardContent>
        </Card>
      ) : null}

      <Card>
        <CardHeader>
          <CardTitle>Top findings</CardTitle>
          <CardDescription>Up to ten by severity when a findings snapshot exists</CardDescription>
        </CardHeader>
        <CardContent>
          {data.topFindings.length === 0 ? (
            <OperatorEmptyState title="No findings in lineage">
              <p className="text-sm">
                Findings are shown when the approval run id matches a run with a findings snapshot.
              </p>
            </OperatorEmptyState>
          ) : (
            <ul className="space-y-2 text-sm">
              {data.topFindings.map((f) => (
                <li key={f.findingId} className="rounded-md border p-2">
                  <div className="flex flex-wrap items-center gap-2">
                    <Badge variant="outline">{f.severity}</Badge>
                    <span className="font-medium">{f.title}</span>
                  </div>
                  <div className="text-muted-foreground text-xs">
                    {f.engineType} · trace completeness {(f.traceCompletenessRatio * 100).toFixed(0)}%
                  </div>
                </li>
              ))}
            </ul>
          )}
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>Promotions</CardTitle>
          <CardDescription>Recorded promotion history for this run</CardDescription>
        </CardHeader>
        <CardContent>
          {data.promotions.length === 0 ? (
            <p className="text-sm text-muted-foreground">No promotion records.</p>
          ) : (
            <ul className="space-y-2 text-sm">
              {data.promotions.map((p) => (
                <li key={p.promotionRecordId} className="rounded-md border p-2">
                  <div>
                    {p.sourceEnvironment} → {p.targetEnvironment} · {p.manifestVersion}
                  </div>
                  <div className="text-muted-foreground text-xs">
                    {formatIsoUtcForDisplay(p.promotedUtc)} · {p.promotedBy}
                  </div>
                </li>
              ))}
            </ul>
          )}
        </CardContent>
      </Card>

      <Separator />
      <p className="text-xs text-muted-foreground">
        Per-finding explainability (no LLM):{" "}
        <span className="font-mono">
          GET /v1/explain/runs/{"{runId}"}/findings/{"{findingId}"}/explainability
        </span>
      </p>
    </div>
  );
}
