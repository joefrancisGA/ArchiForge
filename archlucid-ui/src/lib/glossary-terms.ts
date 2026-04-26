/**
 * Short operator glossary entries (parallel to `docs/library/GLOSSARY.md`; do not edit that file from here).
 * Anchors match GitHub-style slugs for the library markdown.
 */
export type GlossaryTermEntry = {
  term: string;
  definition: string;
  /** Repo-relative doc path (browser may 404; useful from IDE and static hosts). */
  docLink?: string;
};

export const GLOSSARY_TERMS = {
  run: {
    term: "Architecture run",
    definition: "The top-level work unit: a request that flows through ingestion, graph, findings, decisioning, and artifacts, ending in a finalized golden manifest.",
    docLink: "/docs/library/GLOSSARY.md#architecture-run-run",
  },
  golden_manifest: {
    term: "Golden manifest",
    definition: "The versioned, finalized design record for a run—the source of truth for governance, comparison, and artifacts.",
    docLink: "/docs/library/GLOSSARY.md#golden-manifest",
  },
  findings: {
    term: "Finding",
    definition: "A structured observation from a finding engine about the architecture (policy gaps, cost, security, and similar).",
    docLink: "/docs/library/GLOSSARY.md#finding",
  },
  authority_pipeline: {
    term: "Authority run orchestrator",
    definition: "The in-process pipeline that runs ingestion → graph → findings → decisioning → artifact synthesis for one run, inside a SQL unit of work.",
    docLink: "/docs/library/GLOSSARY.md#authority-run-orchestrator",
  },
  context_snapshot: {
    term: "Context snapshot",
    definition: "A point-in-time capture of ingested context (declarations, requirements, topology) that feeds the knowledge graph.",
    docLink: "/docs/library/GLOSSARY.md#context-snapshot",
  },
  decision_trace: {
    term: "Decision trace",
    definition: "A structured log of decisioning for a run—rules, applied findings, and outcome—used for provenance and replay.",
    docLink: "/docs/library/GLOSSARY.md#decision-trace",
  },
  effective_governance: {
    term: "Effective governance",
    definition: "The merged policy content for a scope (project → workspace → tenant) used for alerts, compliance, and advisories.",
    docLink: "/docs/library/GLOSSARY.md#effective-governance",
  },
  policy_pack: {
    term: "Policy pack",
    definition: "A versioned document that bundles rules, advisories, and alert wiring; assigned to scopes and merged at evaluation time.",
    docLink: "/docs/library/GLOSSARY.md#policy-pack",
  },
  knowledge_graph: {
    term: "Knowledge graph",
    definition: "A typed graph of nodes and edges built from a context snapshot—used by finding engines and the graph UI.",
    docLink: "/docs/library/GLOSSARY.md#knowledge-graph",
  },
  artifact_bundle: {
    term: "Artifact bundle",
    definition: "A ZIP of artifacts for a run (diagrams, documents, JSON). Large bundles may be stored in blob storage.",
    docLink: "/docs/library/GLOSSARY.md#artifact-bundle",
  },
  scope: {
    term: "Scope",
    definition: "Tenant / workspace / project identifiers that partition data; carried in claims or headers and enforced in SQL (RLS when enabled).",
    docLink: "/docs/library/GLOSSARY.md#scope-tenant--workspace--project",
  },
  comparison_replay: {
    term: "Comparison replay",
    definition: "Re-running comparison logic on stored output without re-invoking agents, to see deltas under new rules.",
    docLink: "/docs/library/GLOSSARY.md#comparison-replay",
  },
  hosting_role: {
    term: "Hosting role",
    definition: "Whether a process runs API, worker, or combined—controls which services and background jobs are active.",
    docLink: "/docs/library/GLOSSARY.md#hosting-role",
  },
  outbox: {
    term: "Transactional outbox",
    definition: "SQL tables that enqueue work in the same transaction as the change; workers publish or process rows reliably after commit.",
    docLink: "/docs/library/GLOSSARY.md#outbox-transactional-outbox",
  },
  finding_engine: {
    term: "Finding engine",
    definition: "A pluggable component that reads context/graph state and returns findings; multiple engines run in the orchestrated pipeline.",
    docLink: "/docs/library/GLOSSARY.md#finding-engine",
  },
  audit_event: {
    term: "Audit event",
    definition: "One row in the tenant audit log: who did what, when, with optional correlation, run, and detail payload JSON.",
  },
  governance_workflow: {
    term: "Governance workflow",
    definition: "The structured path to request, review, and activate manifest changes for a run, with approver and evidence trail.",
  },
  manifest_diff: {
    term: "Manifest diff",
    definition: "A field-level comparison between two finalized golden manifests (or their persisted projection), used in Compare to see what changed between runs.",
    docLink: "/docs/library/COMPARISON_REPLAY.md",
  },
  comparison_record: {
    term: "Comparison record",
    definition: "A persisted result of a compare (legacy and/or structured paths) you can re-open, replay, or reason about without re-running agents.",
    docLink: "/docs/library/COMPARISON_REPLAY.md",
  },
  approval_request: {
    term: "Approval request",
    definition: "A governance row asking approvers to promote, reject, or activate a change for a run, with segregation of duties and audit trail.",
    docLink: "/docs/library/GLOSSARY.md#governance-workflow",
  },
  governance_resolution: {
    term: "Governance resolution",
    definition: "The operator workflow that applies policy, reconciles risk, and routes outcomes after findings or compliance signals—before or instead of a formal approval in some tenants.",
    docLink: "/docs/library/GOVERNANCE.md",
  },
} as const satisfies Readonly<Record<string, GlossaryTermEntry>>;

export type GlossaryTermKey = keyof typeof GLOSSARY_TERMS;
