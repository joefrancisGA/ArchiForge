using ArchLucid.Contracts.Common;
using ArchLucid.Contracts.Requests;

namespace ArchLucid.Application.Templates;

/// <summary>
///     Pre-built <see cref="ArchitectureRequest" /> payloads aligned with <c>POST /v1/architecture/request</c>.
///     Each template records <c>templateId</c> as the first inline document named <c>ArchLucid.TemplateId</c> (
///     <c>text/plain</c>)
///     so clients can track catalog selection without extending the core request contract.
/// </summary>
public static class ArchitectureRequestTemplates
{
    private const string TemplateIdDocumentName = "ArchLucid.TemplateId";

    /// <summary>Summaries for <c>GET /v1/architecture/templates</c> (fixed catalog).</summary>
    public static IReadOnlyList<ArchitectureRequestTemplateSummary> Summaries
    {
        get;
    } =
    [
        new(
            "microservices-web-platform",
            "Microservices web platform",
            "API gateway plus user, order, and notification services on Kubernetes with PostgreSQL, Redis, and HTTPS between services."),
        new(
            "monolith-migration-assessment",
            "Monolith migration assessment",
            "Legacy .NET Framework monolith on SQL Server: scaling pain, deployment coupling, and service decomposition options."),
        new(
            "event-driven-processing-pipeline",
            "Event-driven processing pipeline",
            "Hub-style ingestion, stream processing, multiple consumers, delivery semantics, and dead-letter handling."),
        new(
            "cloud-native-migration-azure",
            "Cloud-native migration (Azure)",
            "On-premises VMs to Azure (App Service, Azure SQL, Blob) with security and compliance guardrails."),
        new(
            "regulated-healthcare-hipaa",
            "Regulated healthcare (HIPAA)",
            "Patient-data system: HIPAA constraints, auditability, encryption, access control, and data residency.")
    ];

    public static ArchitectureRequest MicroservicesWebPlatform(string? requestId = null)
    {
        return Build(
            "microservices-web-platform",
            requestId,
            "Microservices web platform",
            """
            Design a baseline microservices platform for customer-facing web workloads. The north-south edge is an API gateway;
            core domains are expressed as independently deployable services. All east-west traffic must use TLS.
            Produce a target topology, data ownership boundaries, and operational concerns (observability, rollout, secrets).
            """,
            "MicroservicesWebPlatform",
            "prod",
            CloudProvider.Azure,
            [
                "Workspace and project scope are taken from the signed-in operator session (default workspace and project).",
                "Kubernetes is the preferred runtime; cloud control plane is Azure-aligned (Azure Kubernetes Service or equivalent).",
                "PostgreSQL is the system of record; Redis is used for caching and ephemeral coordination."
            ],
            [
                "East-west service calls must use HTTPS (TLS) — no cleartext on the mesh or cluster network.",
                "No more than four domain services in the first delivery increment (gateway + three domains as listed)."
            ],
            [
                "API gateway (ingress, authn delegation, rate limits)",
                "User, order, and notification domain services",
                "PostgreSQL and Redis",
                "Kubernetes deployment with rolling updates"
            ],
            [
                (
                    "Evidence — API Gateway",
                    """
                    **Component:** Edge API gateway (e.g. ingress controller + policy layer).

                    **Role:** TLS termination for external clients, request routing to domain services, authentication passthrough
                    or token validation, coarse rate limiting. Must not become a shared datastore or workflow orchestrator.
                    """
                ),
                (
                    "Evidence — User Service",
                    """
                    **Component:** User / identity profile service (bounded context).

                    **Role:** Owns user profiles, preferences, and account lifecycle events. Exposes HTTPS-only APIs;
                    persists authoritative user state in PostgreSQL; publishes integration events when profiles change.
                    """
                ),
                (
                    "Evidence — Order Service",
                    """
                    **Component:** Order fulfillment service.

                    **Role:** Owns order aggregates, pricing snapshots as referenced at order time, and fulfillment state transitions.
                    Uses PostgreSQL as source of truth; coordinates with notification service for async customer comms.
                    """
                ),
                (
                    "Evidence — Notification Service",
                    """
                    **Component:** Notification / outbound communications service.

                    **Role:** Consumes domain events (email/SMS/push adapters). At-least-once delivery acceptable with idempotent
                    handlers; dead-letter path for poison messages. No direct coupling to order tables — contract via events/API.
                    """
                ),
                (
                    "Evidence — Datastores and platform",
                    """
                    **PostgreSQL:** System of record for durable domain state; per-service schemas or databases with clear ownership.

                    **Redis:** Cache, session ephemeral state, or short-lived locks — not a substitute for transactional guarantees.

                    **Kubernetes:** Workload orchestration, horizontal scaling, secrets via platform integration; pod-to-pod traffic
                    encrypted (service mesh or equivalent) to satisfy HTTPS-between-services intent for internal calls.
                    """
                )
            ],
            ["microservices", "kubernetes", "api-gateway", "postgres", "redis", "tls-east-west"],
            ["tls-everywhere", "least-privilege-service-accounts", "no-cleartext-internal-rpc"]);
    }

    public static ArchitectureRequest MonolithMigrationAssessment(string? requestId = null)
    {
        return Build(
            "monolith-migration-assessment",
            requestId,
            "Monolith migration assessment",
            """
            Assess decomposition options for a legacy ASP.NET MVC / Web API monolith on .NET Framework backed by SQL Server.
            Current pain: scaling bottlenecks, deployment coupling (big-bang releases), and lack of team autonomy around modules.
            Deliver a migration path (strangler vs. big-bang), candidate service boundaries, data split risks, and test strategy.
            """,
            "LegacyMonolithAssessment",
            "prod",
            CloudProvider.Azure,
            [
                "Workspace and project scope are taken from the signed-in operator session (default workspace and project).",
                "The monolith remains authoritative until cutover; dual-write is only introduced with explicit approval.",
                "Teams are organized around business capabilities, not technical layers."
            ],
            [
                "Preserve regulatory and audit trails currently stored in SQL Server until a verified migration is complete.",
                "Avoid breaking existing public interfaces during incremental extraction."
            ],
            [
                "Read scaling for high-traffic read models without rewriting the entire monolith on day one",
                "Independent deployment units for at least two candidate bounded contexts",
                "SQL Server integration and eventual per-service data ownership"
            ],
            [
                (
                    "Evidence — Legacy monolith",
                    """
                    **Stack:** .NET Framework monolith hosting HTTP workloads and batch-style jobs in-process.

                    **Pain:** Shared database schema across modules; release trains require full regression of unrelated features.
                    """
                ),
                (
                    "Evidence — SQL Server footprint",
                    """
                    **Data:** Central SQL Server instance with cross-module foreign keys and shared reporting views.

                    **Risk:** Splitting ownership without transaction boundary analysis causes consistency bugs.
                    """
                ),
                (
                    "Evidence — Scaling and coupling",
                    """
                    **Symptoms:** Vertical scaling limits hit during seasonal peaks; hot modules cannot scale independently.

                    **Deployment:** Single artifact means low-risk fixes wait behind large features.
                    """
                ),
                (
                    "Evidence — Team autonomy gap",
                    """
                    **Organization:** Multiple squads commit to the same solution; merge conflicts and coordination tax dominate.

                    **Goal:** Define service APIs and data ownership so squads can ship on independent cadence.
                    """
                )
            ],
            ["strangler-fig", "domain-aligned-services", "legacy-sql-server", "incremental-extraction"],
            ["audit-retention", "least-privilege-db-access"]);
    }

    public static ArchitectureRequest EventDrivenProcessingPipeline(string? requestId = null)
    {
        return Build(
            "event-driven-processing-pipeline",
            requestId,
            "Event-driven processing pipeline",
            """
            Architect a high-throughput event pipeline: ingestion from producers through a durable log (Kafka-style or cloud event hub),
            stream processing, and fan-out to multiple consumers. Address ordering, replay, idempotency, exactly-once *effects*
            (end-to-end guarantees), poison-message handling, and observability across stages.
            """,
            "EventProcessingPipeline",
            "prod",
            CloudProvider.Azure,
            [
                "Workspace and project scope are taken from the signed-in operator session (default workspace and project).",
                "Cross-region disaster recovery is a later phase unless stated in constraints.",
                "Consumers may be owned by different teams with independent release cycles."
            ],
            [
                "Dead-letter queues or topics must exist for every subscription with automated replay tooling defined.",
                "Sensitive payloads must be encrypted at rest in the log and access-controlled via IAM."
            ],
            [
                "Ordered partitions where the business key requires ordering",
                "At-least-once delivery to consumers with idempotent handlers",
                "Exactly-once or effectively-once side effects for financial adjacency (where required)",
                "Stream aggregation windows for near-real-time metrics"
            ],
            [
                (
                    "Evidence — Ingestion bus",
                    """
                    **Ingestion:** Durable partitioned log (e.g. Kafka, Azure Event Hubs). Producers publish keyed events for affinity.

                    **Ops:** Throughput sizing, retention policy, and compaction strategy documented.
                    """
                ),
                (
                    "Evidence — Stream processors",
                    """
                    **Processing:** Stateful stream jobs (windows, joins) with checkpointed offsets; replay from last committed state.

                    **Failure:** Job restarts must not duplicate monetary side effects without compensating controls.
                    """
                ),
                (
                    "Evidence — Consumer fleet",
                    """
                    **Consumers:** Multiple subscriber groups / applications with heterogeneous SLAs.

                    **Back-pressure:** Slow consumers must not block the log — scale independently; monitor consumer lag.
                    """
                ),
                (
                    "Evidence — Delivery semantics",
                    """
                    **Semantics:** Document per use case: at-most-once, at-least-once with idempotency keys, or transactional outbox
                    patterns bridging DB commits and event publication.

                    **Reconciliation:** Periodic audit jobs compare source-of-truth vs. projections.
                    """
                ),
                (
                    "Evidence — Dead-letter handling",
                    """
                    **DLQ:** Malformed or repeatedly failing messages routed to a DLQ with alerting, manual triage UI, and replay API.

                    **Poison detection:** Threshold-based circuit breaking to protect shared dependencies.
                    """
                )
            ],
            ["event-sourcing-adjacent", "cqrs-read-models", "partitioned-log", "consumer-groups"],
            ["encrypt-events-at-rest", "fine-grained-publish-subscribe-iam"]);
    }

    public static ArchitectureRequest CloudNativeMigration(string? requestId = null)
    {
        return Build(
            "cloud-native-migration-azure",
            requestId,
            "Cloud-native migration to Azure",
            """
            Plan migration of an on-premises VM-hosted application stack to Azure. Target reference: App Service for compute,
            Azure SQL for relational data, Azure Blob Storage for object artifacts. Include network boundaries (private access
            where possible), identity (Entra ID / managed identity), backup/DR, cost controls, and compliance attestations.
            """,
            "VmToAzureMigration",
            "prod",
            CloudProvider.Azure,
            [
                "Workspace and project scope are taken from the signed-in operator session (default workspace and project).",
                "Lift-and-shift to IaaS is an interim option only if PaaS blockers are documented with expiry dates.",
                "Existing RTO/RPO targets must be met or revised explicitly in the plan."
            ],
            [
                "No public SQL endpoints — private connectivity or approved exceptions only.",
                "Customer-owned encryption keys required for regulated blob containers where applicable."
            ],
            [
                "Azure App Service hosting with deployment slots",
                "Azure SQL with automated backups and geo-redundant options",
                "Blob storage for static assets and integration file drops",
                "Entra ID–backed authentication and managed identities for service calls"
            ],
            [
                (
                    "Evidence — Current on-premises footprint",
                    """
                    **As-is:** Multi-tier app on Windows/Linux VMs, load balancer, and attached SAN/NAS for files.

                    **Dependencies:** Scheduled jobs, file shares, and integration endpoints with partner systems.
                    """
                ),
                (
                    "Evidence — Target Azure topology",
                    """
                    **To-be:** App Service plans sized for workload; Azure SQL logical server with private link; Blob containers with
                    lifecycle tiers (hot/cool/archive) per retention policy.
                    """
                ),
                (
                    "Evidence — Security and compliance",
                    """
                    **Controls:** Conditional Access, PIM for break-glass, Key Vault for secrets, Defender for Cloud baseline.

                    **Compliance:** Data residency region locked; logging to Log Analytics; alert routing to SOC queue.
                    """
                ),
                (
                    "Evidence — Migration waves",
                    """
                    **Approach:** Strangler for edge traffic, database DMA / online migration tooling, blob sync for large file sets.

                    **Validation:** Parallel run window with reconciliation dashboards before DNS cutover.
                    """
                )
            ],
            ["azure-paas", "app-service", "azure-sql", "private-link", "blob-storage"],
            ["managed-identity", "private-endpoints", "defender-for-cloud"]);
    }

    public static ArchitectureRequest RegulatedHealthcareSystem(string? requestId = null)
    {
        return Build(
            "regulated-healthcare-hipaa",
            requestId,
            "Regulated healthcare information system",
            """
            Design or review a patient data processing system subject to HIPAA. Cover minimum necessary access, auditability
            (who accessed what PHI and when), encryption in transit and at rest, breach notification hooks, BAA-covered flows,
            and residency constraints for PHI storage and processing.
            """,
            "HealthcarePhiPlatform",
            "prod",
            CloudProvider.Azure,
            [
                "Workspace and project scope are taken from the signed-in operator session (default workspace and project).",
                "Business Associate Agreements cover all subprocessors in the critical path for PHI.",
                "Clinical workflows require sub-second reads for certain hot paths — document latency SLOs."
            ],
            [
                "PHI must remain in approved geographic regions; cross-border replication disabled unless legally cleared.",
                "All PHI access paths must emit tamper-evident audit records with 7-year (or policy-defined) retention."
            ],
            [
                "Role-based and attribute-based access control for clinician vs. operational roles",
                "Full-disk and database TDE; TLS 1.2+ for all API traffic",
                "Break-glass access with post-incident review workflow",
                "Disaster recovery without PHI leakage to non-production environments"
            ],
            [
                (
                    "Evidence — HIPAA scope",
                    """
                    **Scope:** Electronic PHI ingestion (HL7/FHIR/API), storage, transformation, and disclosure to authorized apps.

                    **Administrative safeguards:** Workforce training, sanctions policy, BA breach notification timelines.
                    """
                ),
                (
                    "Evidence — Audit and monitoring",
                    """
                    **Audit:** Centralized logging of authentication, CRUD on PHI tables, bulk exports, and admin configuration changes.

                    **Monitoring:** Anomaly detection on unusual access patterns; SIEM integration.
                    """
                ),
                (
                    "Evidence — Encryption posture",
                    """
                    **At rest:** TDE on databases; CMK option for blob and key vault keys with rotation runbooks.

                    **In transit:** mTLS or TLS for internal service hops where PHI traverses the network.
                    """
                ),
                (
                    "Evidence — Access control",
                    """
                    **Identity:** Entra ID with phishing-resistant MFA for administrators; JIT elevation for infrastructure roles.

                    **Application:** Consent screens, session timeouts, and purpose-of-use enforcement on APIs.
                    """
                ),
                (
                    "Evidence — Data residency",
                    """
                    **Residency:** Primary and backup regions pinned; backups and replicas verified to stay in-policy.

                    **Testing:** Synthetic PHI only in non-prod; production-like datasets masked or tokenized.
                    """
                )
            ],
            ["hipaa-aligned-azure", "phi-partitioning", "audit-everything", "cmk-option"],
            [
                "hipaa-security-rule",
                "nist-800-66r2-aligned",
                "break-glass-audited",
                "minimum-necessary-access"
            ]);
    }

    private static ArchitectureRequest Build(
        string templateId,
        string? requestId,
        string title,
        string descriptionBody,
        string systemName,
        string environment,
        CloudProvider cloudProvider,
        List<string> assumptions,
        List<string> constraints,
        List<string> requiredCapabilities,
        IReadOnlyList<(string Name, string Content)> evidenceDocuments,
        List<string> topologyHints,
        List<string> securityBaselineHints)
    {
        if (string.IsNullOrWhiteSpace(templateId))
            throw new ArgumentException("Template id is required.", nameof(templateId));

        List<ContextDocumentRequest> docs =
        [
            new() { Name = TemplateIdDocumentName, ContentType = "text/plain", Content = templateId }
        ];

        docs.AddRange(evidenceDocuments.Select(doc =>
            new ContextDocumentRequest { Name = doc.Name, ContentType = "text/markdown", Content = doc.Content }));

        string description = $"{title}\n\n{descriptionBody}".Trim();

        return new ArchitectureRequest
        {
            RequestId = string.IsNullOrWhiteSpace(requestId) ? Guid.NewGuid().ToString("N") : requestId.Trim(),
            Description = description,
            SystemName = systemName,
            Environment = environment,
            CloudProvider = cloudProvider,
            Assumptions = assumptions,
            Constraints = constraints,
            RequiredCapabilities = requiredCapabilities,
            Documents = docs,
            TopologyHints = topologyHints,
            SecurityBaselineHints = securityBaselineHints,
            InlineRequirements = [],
            PolicyReferences = [],
            InfrastructureDeclarations = [],
            PriorManifestVersion = null
        };
    }
}
