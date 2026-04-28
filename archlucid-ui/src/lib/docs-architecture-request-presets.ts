import type { WizardPreset } from "@/lib/wizard-presets";
import type { WizardFormValues } from "@/lib/wizard-schema";

/**
 * Mirrors `docs/templates/architecture-requests/*.json` for the new-run wizard step 1 (no `requestId` —
 * merges with fresh `buildDefaultWizardValues()` so each apply gets a unique id).
 */

function tplCloudMigration(): Partial<WizardFormValues> {
  return {
    systemName: "Contoso Order Management",
    environment: "Production",
    cloudProvider: "Azure",
    priorManifestVersion: "",
    description:
      "Assess a lift-and-shift and selective replatform of the Contoso Order Management 3-tier web application from on-premises datacenters to Azure. Current state: IIS / .NET workloads, SQL Server on-prem clustering, Redis-like session/cache tier, file shares for batch drops. Target: Azure App Service (Linux or Windows containers) for web and API tiers, Azure SQL Database (Business Critical or General Purpose with zone redundancy where approved), Azure Cache for Redis for session/cache, private connectivity via Virtual Network integration and Private Link to PaaS. Business requires 99.95% availability for the storefront path during cutover windows, predictable monthly spend under stakeholder-approved limits, GDPR-aligned retention for EU customer subsets, baseline PCI-DSS segmentation for payment-adjacent components, TLS 1.2+ everywhere, encryption at rest for SQL and Redis, centralized secrets in Key Vault, and auditable deployment and change records.",
    constraints: [
      "Minimum 99.95% SLA for storefront order placement path during phased migration windows",
      "Non-production aggregate monthly cloud spend capped per finance approval letter",
      "GDPR-aligned data minimization for EU storefront profiles; lawful basis documented",
      "PCI-DSS network segmentation — cardholder data environment isolated from merchandising analytics",
      "No public ingress to databases or Redis; Private Link or VNet injection only",
      "Blue/green or canary rollout with rollback under 30 minutes objective for critical SKU paths",
    ],
    requiredCapabilities: [
      "Azure App Service with autoscale and deployment slots",
      "Azure SQL Database with automated backups and geo-redundant backup where policy requires",
      "Azure Cache for Redis with persistence expectations documented",
      "Azure Key Vault for application secrets and connection strings",
      "Private endpoints or App Service VNet integration for SQL and Redis",
      "Azure Firewall or NGFW-approved patterns between hub spoke segments",
      "Application Insights distributed tracing correlated to business transaction ids",
    ],
    assumptions: [
      "Identity migrates from on-prem AD FS to Entra ID with staged app registrations",
      "Batch interfaces can tolerate bounded dual-write latency during coexistence phase",
      "Operations team adopts Infrastructure as Code (Bicep or Terraform) aligned to landing zone guardrails",
      "Read replicas or secondary pools acceptable for analytics after cutover stabilization",
    ],
    inlineRequirements: [
      "Cold start targets for storefront APIs documented with load test evidence before go-live",
      "Disaster recovery: RPO and RTO for order header tables agreed with business continuity owner",
      "Certificate lifecycle for custom domains and internal service-to-service TLS automated",
    ],
    policyReferences: ["Organizational cloud migration standard v3", "Data classification and retention schedule — retail BU"],
    topologyHints: [
      "Prefer single write plane for order master data during migration; avoid split-brain across SQL targets",
      "Isolate batch file ingress in a dedicated subnet with controlled SMB or SFTP replacement on Azure Storage",
      "Front Door or Application Gateway in front of App Service for WAF and TLS termination patterns",
    ],
    securityBaselineHints: [
      "Defender for Cloud recommendations triaged before production promotion",
      "SQL Auditing and threat detection aligned to security team severity matrix",
      "Managed identities for App Service to SQL and Key Vault; no long-lived passwords in configuration",
    ],
    documents: [],
    infrastructureDeclarations: [],
  };
}

function tplMicroservices(): Partial<WizardFormValues> {
  return {
    systemName: "Contoso Commerce Mesh",
    environment: "prod",
    cloudProvider: "Azure",
    priorManifestVersion: "",
    description:
      "Review and harden a proposed event-driven microservices decomposition for Contoso's commerce platform on Azure. Five bounded contexts: catalog, cart, checkout, fulfillment notifications, and loyalty points accrual. Inter-service communication prefers asynchronous messaging via Azure Service Bus topics and subscriptions with idempotent handlers; Cosmos DB serves cart and loyalty write models with tunable consistency; Azure API Management fronts external mobile and partner B2B APIs with JWT validation, quotas, and revision-managed OpenAPI specs. Goal: clarify service boundaries, data ownership per aggregate, transactional outbox versus saga patterns for checkout, observability baseline (correlation IDs, metrics, alerting), poison-message handling, and graceful degradation paths when Cosmos or Service Bus throttle.",
    constraints: [
      "At-least-once delivery on Service Bus with idempotent consumers and DLQ playbook",
      "Cosmos partitioning strategy must avoid hot partitions on regional flash-sale traffic",
      "API Management throttling and subscription keys rotated per partner segment",
      "No cross-database two-phase commit — eventual consistency explicitly accepted with compensations",
      "Cross-cutting authZ via Entra ID and app roles; service-to-service mTLS or managed identity where applicable",
    ],
    requiredCapabilities: [
      "Azure Service Bus (topics and subscriptions) with duplicate detection where needed",
      "Azure Cosmos DB with choice of API (SQL) and defined RU/s autoscale policies",
      "Azure API Management with named values in Key Vault and revision-based deployment",
      "Application Insights workspace with distributed tracing across APIM backends",
      "Schema or contract versioning for integration events surfaced to governance",
    ],
    assumptions: [
      "Peak catalog read traffic is cache-heavy; Cosmos point reads dominate write ratio for carts",
      "Loyalty accrual can lag seconds behind payment capture if fraud checks complete first",
      "Operators run Azure-native stacks; Kafka is out of scope for this review cycle",
    ],
    inlineRequirements: [
      "Define explicit aggregate roots and forbidden cross-database joins across contexts",
      "SLO table for synchronous versus asynchronous journeys with error budget alerts",
      "Back-pressure and shedding strategy when Cosmos RU exhaustion or SB quota approached",
    ],
    topologyHints: [
      "Outbound-only Service Bus connectivity from subnets; no inbound listener on workloads except APIM ingress",
      "Co-locate write-heavy Cosmos accounts in transaction-heavy regions with failover policy justified",
      "Prefer dedicated namespaces or topic hierarchy per bounded context with shared governance registry",
    ],
    securityBaselineHints: [
      "Secrets rotated via Key Vault; APIM backends use managed identity to Azure resources",
      "Defender CSPM alerts for overly permissive NSGs on spoke subnets hosting services",
      "PII minimized in telemetry; sampling rules documented for Checkout path",
    ],
    policyReferences: ["Azure Well-Architected — microservices pillar checklist", "API versioning standard — Contoso EA"],
    documents: [],
    infrastructureDeclarations: [],
  };
}

function tplSecurity(): Partial<WizardFormValues> {
  return {
    systemName: "Fabrikam Payments Insights",
    environment: "Production",
    cloudProvider: "Azure",
    priorManifestVersion: "",
    description:
      "Security-centered architecture assessment for Fabrikam's financial services analytics application on Azure handling payment-adjacent data and aggregated insights for fraud and collections teams. Requires Microsoft Entra ID for workforce and delegated partner access with conditional access policies aligned to MFA and compliant devices. Azure Key Vault for secrets, certificates, and key rotation workflows. Private endpoints for PaaS (SQL, Storage, Cognitive Services inference) with denial of public ingress to data planes. Azure Web Application Firewall on Application Gateway or Front Door in front of HTTPS APIs with OWASP CRS baseline and custom exclusions reviewed by security. Azure DDoS Protection Standard on edge. Data encryption at rest (TDE, storage SSE, Cosmos default) and in transit TLS 1.2 minimum. Objective: PCI-DSS scope reduction where possible via tokenization gateways, segmented networks, centralized logging toward SIEM, and evidence-ready configuration baselines.",
    constraints: [
      "PCI-DSS alignment — minimize cardholder environment scope via tokenization and partner-hosted payment pages where approved",
      "All database and sensitive storage reachable only via private endpoints from approved subnets",
      "Administrative access through PIM-privileged roles with justification and ticketing correlation",
      "Geographic residency for EU cohort in approved Azure regions with no discretionary replication to non-compliant regions",
      "Security monitoring and audit logs retained per regulated retention schedules",
    ],
    requiredCapabilities: [
      "Microsoft Entra ID with Conditional Access and PIM-eligible privileged roles",
      "Azure Key Vault with private endpoints and RBAC-enabled data plane access",
      "Web Application Firewall on internet-facing entry with managed rule sets",
      "Azure DDoS Protection Standard protecting public IPs or Front Door SKU",
      "Microsoft Defender for Cloud enabled at subscription landing zone enrollment",
      "Centralized immutable audit pipelines for privileged operations and firewall policy changes",
    ],
    assumptions: [
      "PAN data is tokenized before analytics lake landing; PAN never stored in analytic SQL pools",
      "Third-party payment processors maintain attested PCI perimeter for capture surfaces",
      "Security operations consumes alerts via SIEM connectors with SLA for critical severities",
    ],
    inlineRequirements: [
      "Document compensating controls for any CSP exception approvals on network rulesets",
      "Annual penetration testing scope aligned to cardholder adjacent paths with evidence trail",
      "Key and secret rotation playbook with blackout windows communicated to dependents",
    ],
    topologyHints: [
      "Separate spoke for CDE-aligned workloads versus analytics-only subnets with restrictive peering routes",
      "Ingress path: Front Door regional routing with WAF and origin limited to hardened APIM or App Service ingress",
      "Break-glass access via isolated Privileged Access Workstations documented in RACI",
    ],
    securityBaselineHints: [
      "CIS Azure Foundations benchmarks applied with exceptions recorded in centralized GRC tooling",
      "Encryption key hierarchy with customer-managed keys for sensitive analytics stores where mandated",
      "Vulnerability remediation SLAs tracked for internet-facing workloads and APIs",
    ],
    policyReferences: ["PCI-DSS v4 segmentation guidance — internal condensed", "FAB-INFOSEC-BASELINE-2026"],
    documents: [],
    infrastructureDeclarations: [],
  };
}

function tplGreenfieldSaas(): Partial<WizardFormValues> {
  return {
    systemName: "NorthWind Operations Cloud",
    environment: "prod",
    cloudProvider: "Azure",
    priorManifestVersion: "",
    description:
      "Greenfield architecture for a multi-tenant B2B SaaS operations platform on Azure. Strong tenant isolation via row-level constructs in shared SQL schema plus optional dedicated pools for enterprise tiers; SESSION_CONTEXT-compatible patterns for application-enforced partition keys. Microsoft Entra External ID (or B2B) for customer org onboarding with SCIM provisioning hooks into the product directory. Billing integration with Stripe for subscription lifecycle and usage meters for overage-aligned SKUs exported to finance. CI/CD on GitHub Actions with environment-gated deployments to staging and production AKS or Container Apps, signed containers, SBOM generation, and policy-as-code in Azure Policy. Comprehensive monitoring: Azure Monitor, structured logging, OpenTelemetry exporters, uptime synthetic probes, golden signals dashboards, incident paging via Logic Apps or PagerDuty webhooks tied to SLA tiers.",
    constraints: [
      "Tenant data isolation enforced at persistence and API layers with audited cross-tenant guardrails denied by default",
      "Production changes through approved pipelines — no drift from IaC-approved state without exception record",
      "SOC 2 preparedness for security and availability narratives even before formal audit",
      "Cost visibility per tenant and per feature flag for gross margin forecasting",
      "GDPR-aligned export and deletion paths for subscriber organizations",
    ],
    requiredCapabilities: [
      "Azure SQL or PostgreSQL-compatible PaaS with tenant partitioning strategy validated",
      "Container Apps or AKS cluster with ingress TLS, workload identity to Azure RBAC-backed services",
      "Stripe webhook integration behind verified signatures and replay protection",
      "Entra-backed authentication with tenant-bound issuer validation and JWKS caching",
      "Multi-stage GitHub Actions workflows with manual approvals and environment secrets in Key Vault",
      "Application Insights with custom metrics for signups, active tenants, and job queue depth",
    ],
    assumptions: [
      "Initial tenants are mid-market with shared infrastructure until scale triggers silo offering",
      "Background jobs use Azure Service Bus or queue storage with dead-letter triage",
      "Feature flags control risky modules without redeploying core platform",
    ],
    inlineRequirements: [
      "Per-tenant backup and restore RPO/RTO published in customer-facing trust documentation",
      "Rate limits and abuse detection on signup and API authentication endpoints",
      "Disaster recovery runbook for regional Azure outage with customer communication template",
    ],
    topologyHints: [
      "Hub-and-spoke network with shared services in hub and tenant-specific secrets in dedicated Key Vaults or namespaces",
      "Separate runtime cluster or revision for internal admin plane versus tenant-serving data plane",
      "Outbox pattern for integration events to avoid dual-write anomalies between billing and product state",
    ],
    securityBaselineHints: [
      "OWASP ASVS-oriented review for public API surface with automated contract tests in CI",
      "Dependency scanning and base image refresh SLAs for container supply chain",
      "Penetration test scope includes tenant isolation and authZ bypass attempts",
    ],
    policyReferences: ["Multi-tenant SaaS reference architecture — internal", "NW-SOC2-READINESS-CHECKLIST"],
    documents: [],
    infrastructureDeclarations: [],
  };
}

/** Doc-sourced presets — keep aligned with `docs/templates/architecture-requests/*.json`. */
export const documentationArchitectureRequestWizardPresets: WizardPreset[] = [
  {
    id: "docs-architecture-requests-cloud-migration",
    label: "Cloud migration assessment",
    description:
      "Contoso Order Management — 3-tier App Service, Azure SQL, Redis, SLA, GDPR/PCI segmentation, private endpoints.",
    values: tplCloudMigration(),
  },
  {
    id: "docs-architecture-requests-microservices",
    label: "Microservices review",
    description:
      "Five bounded contexts — Service Bus, Cosmos DB, API Management, sagas/outbox, observability.",
    values: tplMicroservices(),
  },
  {
    id: "docs-architecture-requests-security-financial",
    label: "Security architecture (financial)",
    description:
      "Entra ID, Key Vault, private endpoints, WAF, DDoS, PCI-DSS-aligned scope reduction framing.",
    values: tplSecurity(),
  },
  {
    id: "docs-architecture-requests-greenfield-saas",
    label: "Greenfield SaaS design",
    description:
      "Multi-tenant isolation, Stripe billing hooks, CI/CD, monitoring — B2B platform baseline.",
    values: tplGreenfieldSaas(),
  },
];
