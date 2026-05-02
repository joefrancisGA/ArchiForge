/**
 * Pre-built architecture briefs for the quick-start wizard (common enterprise scenarios).
 * Briefs stay generic ("Organization") and align with `WizardFormValues.description` limits (max 4000 chars).
 */

export type ArchitectureReviewTemplateCategory =
  | "migration"
  | "greenfield"
  | "security"
  | "compliance"
  | "optimization";

export type ArchitectureReviewTemplate = {
  id: string;
  name: string;
  description: string;
  briefText: string;
  suggestedTitle: string;
  category: ArchitectureReviewTemplateCategory;
};

/** Stable PascalCase system slug from template id (e.g. `cloud-migration-assessment` → `CloudMigrationAssessment`). */
export function suggestedSystemNameFromTemplateId(templateId: string): string {
  return templateId
    .split("-")
    .filter(Boolean)
    .map((segment) => segment.charAt(0).toUpperCase() + segment.slice(1).toLowerCase())
    .join("");
}

export const architectureReviewTemplates: ArchitectureReviewTemplate[] = [
  {
    id: "cloud-migration-assessment",
    name: "Cloud migration assessment",
    description:
      "On-premises .NET monolith moving to Azure with App Service, Azure SQL, and managed identity for auth and data access.",
    category: "migration",
    suggestedTitle: "Organization — brownfield migration to Azure",
    briefText: `Organization operates a brownfield .NET monolith that currently runs on premises: IIS-hosted ASP.NET Core APIs, Windows services for batch jobs, and SQL Server as the system of record. Traffic is modest but spiky during month-end close. The platform team wants to migrate to Microsoft Azure without a full rewrite.

Target landing zone uses Azure App Service for the API tier (Linux containers or .NET 8 stack), Azure SQL Database (General Purpose, zone-redundant where available) with Entra ID–authenticated access, and Azure Key Vault for secrets. Application code should use managed identity from App Service to reach SQL and blob storage; legacy SQL logins must not persist in production.

Non-functional goals: RPO under 15 minutes for relational data during cutover, ability to roll back to on-premises for two release cycles, and observability via Application Insights with correlation from HTTP to SQL. Integrations include a partner file drop (SFTP today) and an internal SOAP endpoint that will become a private REST call over a VNet-integrated App Service.

Please assess topology, security boundaries (public vs private endpoints), identity flow (managed identity vs user-delegated), operational runbooks gaps, and cost posture for dev/test vs production SKUs. Highlight risks around connection string migration, firewall rules, and data residency for the primary database region.`,
  },
  {
    id: "microservices-architecture-review",
    name: "Microservices architecture review",
    description:
      "Event-driven microservices on Azure Container Apps, Azure Service Bus, and a shared Azure SQL instance with clear ownership boundaries.",
    category: "greenfield",
    suggestedTitle: "Organization — event-driven services platform",
    briefText: `Organization is building a new internal platform of event-driven microservices to replace a set of overlapping CRUD apps. Workloads are moderate: hundreds of events per second peak, strict ordering required for financial adjustment workflows only on a dedicated topic. The team chose Azure Container Apps for compute (consumption plan with KEDA scale rules), Azure Service Bus (topics and subscriptions) for messaging, and a single Azure SQL logical server with one database per bounded context for now (not ideal long-term but acceptable for MVP).

Services are written in .NET 8, share a corporate container registry, and deploy via GitHub Actions. Cross-cutting concerns include distributed tracing (OpenTelemetry to Application Insights), structured logging with tenant and correlation identifiers, and a shared policy library for input validation. One service still holds a synchronous HTTP dependency on a legacy COBOL bridge; the team plans to wrap it behind a circuit breaker and async outbox pattern.

Please review service boundaries, messaging reliability (dead-letter handling, idempotent consumers), data consistency between services, security of secrets and connection strings, and whether the shared SQL pattern creates unacceptable blast radius. Call out missing pieces for disaster recovery, blue/green or revision-based rollouts, and cost controls on Container Apps replicas during idle periods.`,
  },
  {
    id: "security-posture-review",
    name: "Security posture review",
    description:
      "Customer-facing web application using Entra ID, an API gateway, private endpoints to backends, and defense-in-depth patterns.",
    category: "security",
    suggestedTitle: "Organization — customer portal security posture",
    briefText: `Organization runs a customer-facing web application and companion BFF/API layer in Azure. End users authenticate with Microsoft Entra ID (workforce and selected B2B partners). A regional Azure API Management instance fronts the APIs, applies JWT validation, rate limits, and IP restrictions for admin routes. App Services and Azure Functions connect to Azure SQL and Azure Storage only over private endpoints within a hub-spoke VNet; public ingress is limited to Front Door → APIM → internal backends.

The team encrypts data at rest with platform-managed keys today and plans customer-managed keys for regulated datasets next year. Secrets live in Key Vault; release pipelines use workload identity from GitHub Actions. Static application security testing runs on pull requests; dynamic scanning is weekly.

Please evaluate the end-to-end trust chain (token lifetime, refresh, BFF cookie vs bearer patterns), segmentation between internet-facing and internal subnets, logging and audit coverage for privileged actions, supply-chain risks in container builds, and gaps versus zero-trust assumptions. Identify concrete control improvements for session fixation, SSRF from the BFF to internal URLs, and abuse of high-privilege service principals in CI.

The team is also piloting a read-only data replica exposed via a second APIM product for analytics partners; that path must not broaden blast radius to production write APIs. Penetration tests last year found medium issues in dependency versions; Dependabot is enabled but some security updates slip when regression suites are slow.`,
  },
  {
    id: "compliance-gap-analysis-hipaa",
    name: "Compliance gap analysis (HIPAA-aligned)",
    description:
      "Healthcare-adjacent workload needing HIPAA-aligned administrative, technical, and physical safeguards mapped to Azure controls.",
    category: "compliance",
    suggestedTitle: "Organization — HIPAA-aligned controls review",
    briefText: `Organization processes limited electronic protected health information (ePHI) for care coordination: appointment scheduling, referrals, and secure messaging between covered-entity staff and business associates. The workload runs on Azure (App Service, SQL, Blob for document artifacts) in a single US region. Contracts with downstream vendors are in progress; BAAs are executed for Azure as a processor but not yet for all SaaS integrations.

Technical stack: TLS 1.2+ everywhere, encryption at rest enabled, SQL TDE on, no PHI in application logs after redaction pipeline, and role-based access with quarterly access reviews on paper (moving to automated reports). Backup retention is 35 days on SQL; long-term archive is cold blob with immutability flags under evaluation.

Please perform a HIPAA-aligned architecture review at the program level: identify gaps in access control, auditability, integrity, transmission security, and contingency planning. Map major components to reasonable safeguard categories and note where Organization must rely on organizational policies (training, workforce clearance) versus technical controls. Flag high-risk patterns such as shared admin accounts, broad SQL firewall openings, or PHI in non-production environments.

Disaster recovery today is manual runbook restore from SQL geo-redundant backup with an untested quarterly drill. Incident response playbooks reference on-call pages but do not yet spell out breach notification timelines by jurisdiction. A vendor OCR service processes inbound fax images: image data is minimized but metadata might still imply patient identity—this integration needs explicit risk treatment.`,
  },
  {
    id: "cost-optimization-review",
    name: "Cost optimization review",
    description:
      "Over-provisioned Azure footprint: many App Service plans, premium SQL tiers, and idle capacity suitable for rightsizing.",
    category: "optimization",
    suggestedTitle: "Organization — Azure cost reduction initiative",
    briefText: `Organization inherited a large Azure footprint after several acquisitions. There are more than twenty App Service Plans across subscriptions, many on Premium v3 SKUs with low CPU averages. Several production databases use Business Critical tiers while monitoring shows P95 DTU well below 40% baseline. Three environments (dev, test, staging) each mirror production SKUs even though test data is synthetic and traffic is negligible weekends.

Networking uses multiple ExpressRoute circuits with overlapping usage; some regional pairs were never consolidated. Blob storage holds years of developer uploads without lifecycle policy; cool and archive tiers are unused. Reservations were purchased ad hoc and do not match actual VMSS or SQL capacity.

Please review the architecture for cost-efficiency without sacrificing production SLOs: suggest consolidation patterns for App Service plans, SQL tier and replica strategy, dev/test scheduling or auto-shutdown, storage lifecycle, and reserved capacity opportunities. Highlight reliability trade-offs of each cut (e.g., moving dev SQL to serverless). Include observability metrics that finance and engineering can share for chargeback discussions.

FinOps tooling exists at subscription level but labels for cost center and application id are inconsistent, which blocks show-back reporting. Leadership wants a phased plan: quick wins in ninety days, structural changes in two quarters, and guardrails so new workloads default to approved SKUs via Azure Policy.`,
  },
];
