/**
 * Minimal JSON for unhandled ArchLucid API GETs during `capture-all-screenshots`
 * so operator pages render empty states instead of transport errors.
 */

const emptyPaged = { items: [], totalCount: 0, page: 1, pageSize: 20, hasMore: false };

const policyPackContent = {
  complianceRuleIds: [] as string[],
  complianceRuleKeys: [] as string[],
  alertRuleIds: [] as string[],
  compositeAlertRuleIds: [] as string[],
  advisoryDefaults: {} as Record<string, string>,
  metadata: {} as Record<string, string>,
};

const iso = "2025-01-01T00:00:00.000Z";

const productSummarySlice = {
  generatedUtc: iso,
  tenantId: "t1",
  workspaceId: "w1",
  projectId: "p1",
  totalSignalsInScope: 0,
  distinctRunsTouched: 0,
  topAggregateCount: 0,
  artifactTrendCount: 0,
  improvementOpportunityCount: 0,
  triageQueueItemCount: 0,
  summaryNotes: [] as string[],
};

export function getEmptyGraphViewModelJson(): unknown {
  return { nodes: [], edges: [], nodeCount: 0, edgeCount: 0, isEmpty: true };
}

export function isGraphUpstreamPath(pathname: string): boolean {
  return pathname.startsWith("/api/provenance/") || /^\/api\/graph\/runs\//.test(pathname);
}

/**
 * @param pathname — decoded request path (no query)
 * @param search — query string including `?` or empty
 */
export function getScreenshotMockFallbackGetJson(pathname: string, search: string): unknown {
  const u = new URL(`http://x${search}`);

  if (pathname === "/v1/governance/dashboard") {
    return { pendingApprovals: [], recentDecisions: [], recentChanges: [], pendingCount: 0 };
  }

  if (pathname === "/v1/governance/compliance-drift-trend") {
    return [];
  }

  if (pathname === "/v1/governance-resolution") {
    return {
      tenantId: "t1",
      workspaceId: "w1",
      projectId: "p1",
      effectiveContent: policyPackContent,
      decisions: [],
      conflicts: [],
      notes: [] as string[],
    };
  }

  const mLineage = /^\/v1\/governance\/approval-requests\/([^/]+)\/lineage$/.exec(pathname);

  if (mLineage) {
    return {
      approvalRequest: { approvalRequestId: mLineage[1] },
      run: null,
      manifest: null,
      topFindings: [] as unknown[],
      riskPosture: null,
      promotions: [] as unknown[],
    };
  }

  const mRat = /^\/v1\/governance\/approval-requests\/([^/]+)\/rationale$/.exec(pathname);

  if (mRat) {
    return { schemaVersion: 1, approvalRequestId: mRat[1], summary: "Mock", bullets: [] as string[] };
  }

  if (/^\/v1\/governance\/runs\/[^/]+\/approval-requests$/.test(pathname)) {
    return [];
  }

  if (/^\/v1\/governance\/runs\/[^/]+\/promotions$/.test(pathname) || /^\/v1\/governance\/runs\/[^/]+\/activations$/.test(pathname)) {
    return [];
  }

  if (pathname === "/v1/learning/summary") {
    return {
      generatedUtc: iso,
      themeCount: 0,
      planCount: 0,
      totalThemeEvidenceSignals: 0,
      maxPlanPriorityScore: null,
      totalLinkedSignalsAcrossPlans: 0,
    };
  }

  if (pathname === "/v1/learning/themes") {
    return { generatedUtc: iso, themes: [] as unknown[] };
  }

  const mPlan = /^\/v1\/learning\/plans\/([^/]+)$/.exec(pathname);

  if (mPlan) {
    return {
      planId: mPlan[1],
      themeId: "theme-1",
      title: "Mock plan",
      summary: "Mock",
      priorityScore: 0,
      status: "Open",
      createdUtc: iso,
      actionSteps: [] as unknown[],
      evidenceCounts: { linkedSignalCount: 0, linkedArtifactCount: 0, linkedArchitectureRunCount: 0 },
      theme: null,
    };
  }

  if (pathname === "/v1/learning/plans") {
    return { generatedUtc: iso, plans: [] as unknown[] };
  }

  if (pathname === "/v1/product-learning/summary") {
    return productSummarySlice;
  }

  if (pathname.startsWith("/v1/product-learning/improvement-opportunities")) {
    return { generatedUtc: iso, opportunities: [] as unknown[] };
  }

  if (pathname.startsWith("/v1/product-learning/artifact-outcome-trends")) {
    return { generatedUtc: iso, trends: [] as unknown[] };
  }

  if (pathname.startsWith("/v1/product-learning/triage-queue")) {
    return { generatedUtc: iso, items: [] as unknown[] };
  }

  if (pathname === "/v1/pilots/why-archlucid-snapshot") {
    return {
      generatedUtc: iso,
      demoRunId: "demo",
      runsCreatedTotal: 0,
      findingsProducedBySeverity: {} as Record<string, number>,
      auditRowCount: 0,
      auditRowCountTruncated: false,
    };
  }

  if (pathname === "/v1/tenant/measured-roi") {
    return {
      snapshot: {
        generatedUtc: iso,
        demoRunId: "demo",
        runsCreatedTotal: 0,
        findingsProducedBySeverity: {},
        auditRowCount: 0,
        auditRowCountTruncated: false,
      },
      monthlyCostEstimate: null,
      disclaimer: "Mock",
    };
  }

  if (pathname === "/v1/tenant/cost-estimate") {
    return { currency: "USD", monthlyTotalLow: 0, monthlyTotalHigh: 0, lineItems: [] as unknown[] };
  }

  if (pathname === "/v1/tenant/exec-digest-preferences") {
    return { digestCadence: "None", channels: [] as string[], topics: [] as string[] };
  }

  if (pathname.startsWith("/v1/integrations/teams/")) {
    if (pathname.includes("triggers")) {
      return [] as string[];
    }

    return { webhookUrl: null, isEnabled: false };
  }

  if (pathname === "/v1/retrieval/search") {
    return [] as unknown[];
  }

  if (pathname === "/v1/pilots/runs/recent-deltas") {
    return { deltas: [] as unknown[] };
  }

  if (pathname === "/v1/pilots/runs/roi-bulletin") {
    return { html: "<p>Mock</p>" };
  }

  if (/^\/v1\/authority\/projects\/[^/]+\/runs$/.test(pathname) && u.searchParams.has("take") && !u.searchParams.has("page")) {
    return [
      {
        runId: "e2e-fixture-run-001",
        projectId: u.searchParams.get("projectId") ?? "default",
        description: "Mock",
        createdUtc: iso,
        goldenManifestId: "e2e-fixture-manifest-001",
      },
    ];
  }

  if (/^\/v1\/architecture\/runs\/[^/]+\/provenance$/.test(pathname)) {
    const m = /^\/v1\/architecture\/runs\/([^/]+)\/provenance$/.exec(pathname);

    return { runId: m?.[1] ?? "r1", nodes: [] as unknown[], edges: [] as unknown[], timeline: [] as unknown[], traceabilityGaps: [] as string[] };
  }

  if (/^\/v1\/findings\/[^/]+\/inspect$/.test(pathname)) {
    const id = pathname.split("/")[3] ?? "f1";

    return {
      findingId: id,
      typedPayload: null,
      decisionRuleId: null,
      decisionRuleName: null,
      evidence: [] as unknown[],
      auditRowId: null,
      runId: "r1",
      manifestVersion: null,
    };
  }

  if (/\/v1\/explain\/runs\/[^/]+\/findings\/[^/]+\/explainability$/.test(pathname)) {
    return { runId: "r1", findingId: "f1", narrative: "Mock", trustDimensions: [] as unknown[] };
  }

  if (/\/evidence-chain$/.test(pathname) && pathname.includes("/explain/")) {
    return { runId: "r1", findingId: "f1", links: [] as unknown[] };
  }

  if (/\/llm-audit$/.test(pathname) && pathname.includes("/findings/")) {
    return { runId: "r1", findingId: "f1", entries: [] as unknown[] };
  }

  if (pathname.startsWith("/v1/alert-rules")) {
    if (u.searchParams.get("page")) {
      return emptyPaged;
    }

    return [] as unknown[];
  }

  if (pathname.startsWith("/v1/composite-alert-rules")) {
    return [] as unknown[];
  }

  if (pathname.startsWith("/v1/alert-routing-subscriptions")) {
    if (pathname.includes("attempts")) {
      return emptyPaged;
    }

    return [] as unknown[];
  }

  if (pathname === "/v1/digest-subscriptions" || pathname.startsWith("/v1/digest-subscriptions/")) {
    if (pathname.includes("attempts")) {
      return emptyPaged;
    }

    return [] as unknown[];
  }

  if (pathname.startsWith("/v1/advisory-scheduling/")) {
    if (pathname.includes("executions")) {
      return [] as unknown[];
    }

    if (pathname.match(/\/run$/)) {
      return { executionId: "e1" };
    }

    if (/\/digests\/[^/]+$/.test(pathname) && !pathname.endsWith("/digests")) {
      return { digestId: "d1", title: "Mock", bodyPreview: "Mock", createdUtc: iso };
    }

    if (pathname.includes("digests")) {
      return [] as unknown[];
    }

    if (pathname.includes("schedules")) {
      return [] as unknown[];
    }
  }

  if (pathname.startsWith("/v1/conversations/") || pathname.includes("/ask/")) {
    return [] as unknown[];
  }

  if (pathname === "/v1/recommendation-learning/latest") {
    return {
      tenantId: "t1",
      workspaceId: "w1",
      projectId: "p1",
      generatedUtc: iso,
      categoryStats: [] as unknown[],
      urgencyStats: [] as unknown[],
      signalTypeStats: [] as unknown[],
      categoryWeights: {},
      urgencyWeights: {},
      signalTypeWeights: {},
      notes: [] as string[],
    };
  }

  if (pathname.startsWith("/v1/evolution/")) {
    if (pathname.includes("/candidates")) {
      return { candidates: [] as unknown[] };
    }

    if (pathname.includes("/results/")) {
      return {
        candidate: {
          candidateChangeSetId: "c1",
          sourcePlanId: "p1",
          status: "Mock",
          title: "Mock",
          summary: "Mock",
          derivationRuleVersion: "1",
          createdUtc: iso,
        },
        planSnapshotJson: "{}",
        simulationRuns: [] as unknown[],
      };
    }
  }

  if (pathname.includes("/governance/") && pathname.includes("findings")) {
    return emptyPaged;
  }

  if (/\/v1\/explain\/runs\/.+\/detail$/.test(pathname)) {
    return { runId: "r1", sections: [] as unknown[] };
  }

  if (pathname.includes("/pipeline/timeline")) {
    return [] as unknown[];
  }

  if (pathname.includes("/traces") && pathname.includes("architecture")) {
    return { items: [] as unknown[], nextCursor: null };
  }

  if (pathname.includes("agent-evaluation")) {
    return { runId: "r1", summary: "Mock" };
  }

  if (pathname.startsWith("/v1/admin/")) {
    if (pathname.includes("users")) {
      return { users: [] as unknown[] };
    }

    if (pathname.includes("config-summary")) {
      return { entries: [] as unknown[] } as unknown;
    }

    if (pathname.includes("support-bundle")) {
      return { requestId: "mock" } as unknown;
    }

    if (pathname.includes("security-trust") || pathname.includes("publications")) {
      return { items: [] as unknown[] } as unknown;
    }

    return { status: "Unknown", entries: [] as unknown[] } as unknown;
  }

  if (pathname === "/v1/diagnostics/operator-task-success-rates" || pathname.startsWith("/v1/diagnostics/")) {
    if (pathname.includes("sponsor")) {
      return { show: false } as unknown;
    }

    return { windowNote: "Mock", firstRunCommittedTotal: 0, firstSessionCompletedTotal: 0, firstRunCommittedPerSessionRatio: 0 } as unknown;
  }

  if (pathname.startsWith("/v1/onboarding/")) {
    return { step: "Start", completed: false } as unknown;
  }

  if (pathname.startsWith("/v1/alert-tuning/") || pathname.startsWith("/v1/alert-simulation/")) {
    return { result: "mock" } as unknown;
  }

  if (pathname.startsWith("/v1/advisory/")) {
    if (pathname.includes("recommendations")) {
      return { items: [] as unknown[] } as unknown;
    }

    if (pathname.includes("improvements")) {
      return { items: [] as unknown[] } as unknown;
    }

    return [] as unknown[];
  }

  if (pathname.match(/^\/v1\/(replay|conversations|threads)/) || pathname.includes("/replay")) {
    return { runId: "r1", events: [] as unknown[], textExport: null } as unknown;
  }

  if (pathname.startsWith("/v1/")) {
    if (pathname.includes("search") || pathname.includes("items") || pathname.endsWith("/list")) {
      if (u.searchParams.get("page")) {
        return emptyPaged;
      }

      return [] as unknown[];
    }
  }

  return { items: [] as unknown[], totalCount: 0, page: 1, pageSize: 20, hasMore: false } as unknown;
}
