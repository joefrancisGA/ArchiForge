> **Scope:** Copy-paste architecture request templates for `POST /v1/architecture/request` — same JSON shape as the repo-root example file.

# Architecture request templates

JSON files in this directory are **ready-to-send** payloads (or wizard import paste bodies) aligned with [`ArchitectureRequest`](../../../ArchLucid.Contracts/Requests/ArchitectureRequest.cs): `requestId`, `systemName`, `description`, `environment`, `cloudProvider`, plus optional lists such as `constraints`, `topologyHints`, and `securityBaselineHints`.

| Template | Description | File |
|----------|-------------|------|
| **Enterprise RAG (reference)** | Secure Azure AI Search + SQL RAG pattern — canonical sample used in tests | [`../../../enterprise-rag-request.json`](../../../enterprise-rag-request.json) |
| **Cloud migration assessment** | 3‑tier lift/replatform — App Service, Azure SQL, Redis, private endpoints, SLA and compliance | [`cloud-migration-assessment.json`](cloud-migration-assessment.json) |
| **Microservices review** | Five services — Service Bus, Cosmos DB, API Management, boundaries, observability | [`microservices-review.json`](microservices-review.json) |
| **Security architecture (financial)** | Entra ID, Key Vault, private endpoints, WAF, DDoS, PCI-DSS framing | [`security-architecture-assessment.json`](security-architecture-assessment.json) |
| **Greenfield SaaS design** | Multi-tenant SaaS — isolation, identity, billing, CI/CD, monitoring | [`greenfield-saas-design.json`](greenfield-saas-design.json) |

## How to use

1. **HTTP API:** `POST /v1/architecture/request` with `Content-Type: application/json` and a template body (replace `requestId` with your own stable id if desired).
2. **Operator wizard — step 1:** Choose **Architecture request templates** and select a starter; the wizard merges it with validated defaults (`requestId` is regenerated).
3. **Import paste:** Open **Import prepared request** on the wizard, paste JSON, then **Apply import**.

Related: [`../../library/API_CONTRACTS.md`](../../library/API_CONTRACTS.md), [`../../../enterprise-rag-request.json`](../../../enterprise-rag-request.json).
