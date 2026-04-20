> **Scope:** ArchLucid — Product Datasheet - full detail, tables, and links in the sections below.

# ArchLucid — Product Datasheet

<!-- Layout: designed for PDF export at US Letter or A4. Keep under 2 pages rendered. -->

---

**ArchLucid** | AI Architecture Intelligence

*Architecture decisions you can explain, govern, and audit.*

---

## The problem

Architecture review in most enterprises is **slow, inconsistent, and undocumented**.

Reviews depend on a small team of senior architects who apply different standards across projects. Decisions happen in meetings and emails with no durable record. Compliance gaps surface in production — or during audits — long after the design was approved. When regulators ask "who reviewed this design and what did they find?", the answer is often "we are not sure."

## The solution

ArchLucid is an **AI Architecture Intelligence platform** that orchestrates specialized AI agents to analyze system designs, produce explainable findings, and enforce governance workflows — all with a durable audit trail.

Describe the system you want to build. ArchLucid's multi-agent pipeline analyzes it for topology, cost, compliance, and design quality — then produces a versioned manifest with every finding traced and explained. What used to take weeks of manual review now takes minutes, with full accountability.

---

## Key capabilities

| Capability | What it does |
|-----------|-------------|
| **AI Architecture Analysis** | Four specialized agents (Topology, Cost, Compliance, Critic) analyze architecture requests through a structured pipeline. 10 finding engines run in parallel. Multi-vendor LLM with automatic fallback. |
| **Explainable Decisions** | Every finding includes a structured `ExplainabilityTrace` — what was examined, what rules applied, what decisions were taken, and why. Provenance graph links evidence to decisions to artifacts. |
| **Enterprise Governance** | Policy packs define compliance rules. Pre-commit gates block manifests when findings exceed severity thresholds. Approval workflows enforce segregation of duties. SLA tracking with escalation. |
| **Architecture Drift Detection** | Compare two architecture iterations with structured deltas. Replay and verify mode detects drift between stored and regenerated outputs. Compliance drift trending over time. |
| **Export and Reporting** | Consulting-grade DOCX reports with embedded architecture diagrams. Markdown export. ZIP artifact bundles. Comparison replay in multiple formats. |
| **Durable Audit Trail** | 78 typed audit event types in an append-only SQL store. Paginated search, filtered export (JSON/CSV). CI-enforced event count guard. |

---

## Architecture

```
┌──────────┐     ┌──────────────┐     ┌─────────────────────────────────┐
│ Operator │────▶│ ArchLucid    │────▶│ AI Agent Pipeline               │
│ (UI/CLI/ │     │ API          │     │                                 │
│  CI/CD)  │◀────│ (.NET 10)    │     │ Context ─▶ Graph ─▶ Findings   │
└──────────┘     └──────┬───────┘     │ ─▶ Decisioning ─▶ Artifacts   │
                        │             └─────────────────────────────────┘
                        │
                 ┌──────┴───────┐
                 │ Azure SQL    │     ┌─────────────────┐
                 │ (+ RLS)      │     │ Azure OpenAI    │
                 └──────────────┘     │ (multi-vendor)  │
                                      └─────────────────┘
```

**Operator UI** — Next.js web console for runs, manifests, governance, graph exploration, and audit.
**CLI** — `archlucid new`, `run`, `commit`, `artifacts`, `doctor`, `support-bundle`, `trace`.

---

## Deployment options

| Option | Best for |
|--------|---------|
| **Azure Container Apps** | Production — Terraform modules provided for API, worker, SQL, blob, and identity |
| **Docker Compose** | Development and evaluation — full-stack profile with SQL, Redis, Azurite |
| **Self-hosted containers** | Any Docker or Kubernetes environment with SQL Server access |

---

## Security and compliance

| Area | Capability |
|------|-----------|
| **Identity** | Microsoft Entra ID (JWT), API key, RBAC (Admin / Operator / Reader / Auditor) |
| **Data isolation** | SQL row-level security (RLS) with `SESSION_CONTEXT` for multi-tenant isolation |
| **Network** | Private endpoints for SQL and blob storage; Azure Front Door with WAF |
| **Audit** | Append-only event store with `DENY UPDATE/DELETE`; export for compliance evidence |
| **Scanning** | OWASP ZAP baseline in CI, Schemathesis API fuzzing, CodeQL, Gitleaks, Trivy |
| **Threat model** | STRIDE threat model documented (`SYSTEM_THREAT_MODEL.md`) |

---

## Integration points

| Channel | Details |
|---------|---------|
| **REST API** | OpenAPI v1 spec with versioned routes (`/v1/...`), rate limiting, correlation ID |
| **CLI** | .NET global tool or `dotnet run` — full run lifecycle and diagnostics |
| **Webhooks** | HMAC-signed delivery with optional CloudEvents envelope |
| **Service Bus** | Azure Service Bus with transactional outbox for lifecycle events |
| **Events** | `com.archlucid.*` canonical event types with JSON Schema |
| **API Client** | .NET client library (`ArchLucid.Api.Client`) — SDKs for other languages on roadmap |

---

## Observability

30+ custom OpenTelemetry metrics, 8 activity sources, W3C trace propagation. Grafana dashboards committed in the repo (authority pipeline, SLO, LLM usage, run lifecycle). Business KPI metrics: runs created, findings by severity, agent quality scores, explanation cache effectiveness.

---

## Get started

1. **Quickest evaluation (Docker only):** Run `.\scripts\demo-start.ps1` (Windows) or `./scripts/demo-start.sh` (macOS/Linux), or `docker compose -f docker-compose.yml -f docker-compose.demo.yml --profile full-stack up -d --build` — see [DEMO_QUICKSTART.md](DEMO_QUICKSTART.md) for a five-minute walkthrough.
2. **Full-stack without demo overlay:** `docker compose --profile full-stack up -d --build` — same stack without automatic Contoso demo seed (see [CONTAINERIZATION.md](../CONTAINERIZATION.md)).
3. **First run:** Open `http://localhost:3000/runs/new` — seven-step guided wizard
4. **Pilot:** Follow the [Pilot Guide](../PILOT_GUIDE.md) for production-style deployment
5. **Learn more:** [docs/go-to-market/POSITIONING.md](POSITIONING.md) | [docs/go-to-market/COMPETITIVE_LANDSCAPE.md](COMPETITIVE_LANDSCAPE.md)

---

**ArchLucid** — *Every recommendation traced. Every decision governed.*

*Contact: [placeholder — add sales contact or URL]*
