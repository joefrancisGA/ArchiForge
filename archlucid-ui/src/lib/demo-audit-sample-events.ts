import type { AuditEvent } from "@/lib/api";
import {
  SHOWCASE_STATIC_DEMO_MANIFEST_ID,
  SHOWCASE_STATIC_DEMO_RUN_ID,
} from "@/lib/showcase-static-demo";

/**
 * Six milestone events aligned with the static Claims Intake pipeline timeline — shown when demo mode search
 * returns zero rows so the audit surface matches manifest “review trail” messaging.
 */
export function getDemoSampleAuditTrailEvents(): AuditEvent[] {
  const runId = SHOWCASE_STATIC_DEMO_RUN_ID;

  return [
    {
      eventId: "demo-audit-run-started",
      occurredUtc: "2026-01-10T09:15:22.000Z",
      eventType: "RunStarted",
      actorUserId: "demo-jordan",
      actorUserName: "Jordan Lee",
      tenantId: "demo-tenant",
      workspaceId: "demo-workspace",
      projectId: "default",
      runId,
      manifestId: null,
      artifactId: null,
      dataJson: "{}",
      correlationId: "corr-intake-demo-request",
    },
    {
      eventId: "demo-audit-context",
      occurredUtc: "2026-01-14T21:42:10.000Z",
      eventType: "context.snapshot.created",
      actorUserId: "pipeline",
      actorUserName: "ArchLucid pipeline",
      tenantId: "demo-tenant",
      workspaceId: "demo-workspace",
      projectId: "default",
      runId,
      manifestId: null,
      artifactId: null,
      dataJson: "{}",
      correlationId: "corr-intake-demo-ctx",
    },
    {
      eventId: "demo-audit-graph",
      occurredUtc: "2026-01-14T21:51:33.000Z",
      eventType: "graph.snapshot.created",
      actorUserId: "pipeline",
      actorUserName: "ArchLucid pipeline",
      tenantId: "demo-tenant",
      workspaceId: "demo-workspace",
      projectId: "default",
      runId,
      manifestId: null,
      artifactId: null,
      dataJson: "{}",
      correlationId: "corr-intake-demo-graph",
    },
    {
      eventId: "demo-audit-findings",
      occurredUtc: "2026-01-14T22:03:18.000Z",
      eventType: "findings.snapshot.created",
      actorUserId: "pipeline",
      actorUserName: "ArchLucid pipeline",
      tenantId: "demo-tenant",
      workspaceId: "demo-workspace",
      projectId: "default",
      runId,
      manifestId: null,
      artifactId: null,
      dataJson: "{}",
      correlationId: "corr-intake-demo-findings",
    },
    {
      eventId: "demo-audit-manifest",
      occurredUtc: "2026-01-14T22:07:58.000Z",
      eventType: "finalize.run",
      actorUserId: "demo-taylor",
      actorUserName: "Taylor Morgan",
      tenantId: "demo-tenant",
      workspaceId: "demo-workspace",
      projectId: "default",
      runId,
      manifestId: SHOWCASE_STATIC_DEMO_MANIFEST_ID,
      artifactId: null,
      dataJson: "{}",
      correlationId: "corr-intake-demo-manifest",
    },
    {
      eventId: "demo-audit-bundle",
      occurredUtc: "2026-01-14T22:09:44.000Z",
      eventType: "artifact.bundle.created",
      actorUserId: "pipeline",
      actorUserName: "ArchLucid pipeline",
      tenantId: "demo-tenant",
      workspaceId: "demo-workspace",
      projectId: "default",
      runId,
      manifestId: SHOWCASE_STATIC_DEMO_MANIFEST_ID,
      artifactId: null,
      dataJson: "{}",
      correlationId: "corr-intake-demo-bundle",
    },
  ];
}

/** True when the UI should show curated sample rows instead of an empty search (demo builds only). */
export function shouldInjectDemoAuditSample(filters: {
  readonly eventType: string;
  readonly fromUtc: string;
  readonly toUtc: string;
  readonly correlationId: string;
  readonly actorUserId: string;
  readonly runId: string;
}): boolean {
  if (filters.eventType.trim().length > 0) {
    return false;
  }

  if (filters.fromUtc.trim().length > 0 || filters.toUtc.trim().length > 0) {
    return false;
  }

  if (filters.correlationId.trim().length > 0 || filters.actorUserId.trim().length > 0) {
    return false;
  }

  const runTrim = filters.runId.trim();

  if (runTrim.length > 0 && runTrim !== SHOWCASE_STATIC_DEMO_RUN_ID) {
    return false;
  }

  return true;
}