> **Scope:** ArchLucid FAQ — getting started, security, integrations, pricing pointer, product; V1-accurate; no live pricing numbers (link only).

# Frequently asked questions

## Getting started

**What is ArchLucid?**  
ArchLucid is an architecture authority platform: multi-agent analysis, manifest lifecycle, governance workflows, and audit trails for enterprise architecture decisions.

**How do I try it locally?**  
Follow **[CONTRIBUTOR_QUICK_START.md](CONTRIBUTOR_QUICK_START.md)** — build, fast tests, optional SQL, run **ArchLucid.Api** and **archlucid-ui**.

**Do I need Azure OpenAI?**  
Not for all paths: simulator/deterministic modes exist for engineering. Real LLM analysis uses Azure OpenAI per deployment configuration (see **[FIRST_REAL_VALUE.md](FIRST_REAL_VALUE.md)**).

**Where is V1 scope documented?**  
**[V1_SCOPE.md](V1_SCOPE.md)** — in-scope features, gates, and deferred items.

## Security and data

**How do I report a security vulnerability?**  
Email **security@archlucid.net**; do not file public issues for undisclosed vulnerabilities. See **[../SECURITY.md](../SECURITY.md)**.

**Where does tenant data live?**  
Production-style deployments use Azure-native storage and SQL with **row-level security**; see **[../security/MULTI_TENANT_RLS.md](../security/MULTI_TENANT_RLS.md)**.

**Can I get a support bundle for troubleshooting?**  
Yes — operators can generate a support bundle via CLI (see pilot guide / operator quickstart). Include **API `GET /version`**, **`X-Correlation-ID`**, and policy-safe logs when opening issues.

## Integration

**What APIs exist?**  
Versioned REST under **`/v1/*`** with OpenAPI; AsyncAPI where published for workers. Contracts live under **`ArchLucid.Contracts`**.

**Is SCIM supported?**  
SCIM 2.0 is **in V1 scope**; validate against your IdP and staging tenant per **[V1_SCOPE.md](V1_SCOPE.md)**.

**Can I connect ITSM in V1?**  
Core ITSM connectors are **deferred** (see **[V1_DEFERRED.md](V1_DEFERRED.md)**). Events, GitHub, Azure DevOps, and Teams patterns exist today.

## Pricing

**Where is pricing explained?**  
Commercial philosophy and packaging context: **[../go-to-market/PRICING_PHILOSOPHY.md](../go-to-market/PRICING_PHILOSOPHY.md)** — no quoted dollar amounts in this FAQ; sales-led quotes apply.

## Product

**What are architecture “runs”?**  
A run ingests your request and context, executes agents (topology, cost, compliance, critic, etc.), and can produce/commit a golden manifest with governance controls.

**What is a golden manifest?**  
A durable, versioned architecture artifact (manifest) produced after successful authority pipeline stages and optional governance approval.

**Who is a pilot for?**  
Organizations evaluating ArchLucid under controlled staging/production-like setups — see **[PILOT_GUIDE.md](PILOT_GUIDE.md)** and **[OPERATOR_QUICKSTART.md](OPERATOR_QUICKSTART.md)**.

**How do I get help as a pilot?**  
Email **support@archlucid.net** for product/support questions during pilots; keep security issues on **security@archlucid.net**. See **[../SECURITY.md](../SECURITY.md)**.

**Is there accessibility support?**  
Report accessibility barriers to **accessibility@archlucid.net** (see **[../SECURITY.md](../SECURITY.md)**).
