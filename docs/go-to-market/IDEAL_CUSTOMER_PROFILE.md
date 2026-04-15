# ArchLucid — Ideal Customer Profile (ICP)

**Audience:** Sales, marketing, and product teams who need to qualify leads quickly and focus on high-probability opportunities.

**Last reviewed:** 2026-04-15

**Grounding:** Derived from [BUYER_PERSONAS.md](BUYER_PERSONAS.md), [ROI_MODEL.md](ROI_MODEL.md) (break-even at ~180 architect-hours/year), and [COMPETITIVE_LANDSCAPE.md](COMPETITIVE_LANDSCAPE.md).

---

## 1. Definition

The ICP describes the **company profile** where ArchLucid delivers **maximum value** and has the **highest win probability** in V1. Qualifying against the ICP prevents wasted effort on poor-fit prospects.

---

## 2. Firmographic criteria

| Criterion | Ideal range | Reasoning |
|-----------|------------|-----------|
| **Company size** | 500–10,000 employees | Large enough to have a formal architecture practice; small enough that ArchLucid can serve as the primary tool (not competing with entrenched EAM suites at 50K+ orgs) |
| **Industry verticals** | Financial services, technology, healthcare | Compliance pressure (FS, HC) drives governance adoption; technology companies value speed and consistency |
| **Geography** | English-speaking markets (US, UK, ANZ, Western Europe) | V1 is English-only; Azure presence is strong in these regions |
| **Architecture team size** | 3+ architects | Below 3, the ROI model break-even (180 hours/year) is difficult to reach; above 3, each additional architect multiplies savings |
| **Cloud posture** | Azure-primary or Azure-significant | V1 topology, cost, and compliance engines are Azure-focused; AWS/GCP-only organizations are a poor fit until multi-cloud support ships |
| **Architecture practice maturity** | Established (not aspirational) | Active reviews happening today — even if manual and inconsistent. ArchLucid improves existing practice; it does not create one |

---

## 3. Behavioral / situational criteria

| Signal | Why it matters |
|--------|---------------|
| **Active architecture review cadence** | If they review designs regularly, ArchLucid accelerates an existing workflow — highest value |
| **Compliance or audit pressure** | Governance workflow and audit trail are immediate differentiators; compliance-driven buyers have budget |
| **Growth or modernization initiative** | More architecture decisions = more reviews = more value from ArchLucid |
| **Pain from inconsistency** | "Different architects say different things" — multi-agent consistency is the hero feature |
| **Documentation debt** | "We don't document architecture decisions" — ArchLucid's artifacts fill the gap |

---

## 4. Disqualifiers (poor fit for V1)

| Disqualifier | Reason |
|-------------|--------|
| **AWS-only or GCP-only** | V1 finding engines and infrastructure are Azure-focused |
| **No established architecture practice** | ArchLucid accelerates reviews; it does not teach architecture from scratch |
| **Require air-gapped / on-premises deployment** | SaaS-only; no self-hosted option |
| **Fewer than 3 architects** | ROI threshold unlikely to be met per the model (< 180 architect-hours/year) |
| **Evaluating for EAM repository replacement** | ArchLucid is not a modeling/documentation tool (LeanIX, Ardoq competitor); it is an AI analysis platform |
| **No budget authority at architecture level** | If architecture tools require CIO-level approval and no champion exists, cycle will stall |

---

## 5. ICP scoring matrix

Use this to qualify leads in < 5 minutes.

| Criterion | Weight | 3 (Strong fit) | 2 (Moderate) | 1 (Weak) | 0 (Disqualifier) |
|-----------|--------|----------------|--------------|----------|-------------------|
| **Company size** | 2 | 500–10K | 200–500 or 10K–50K | < 200 or > 50K | — |
| **Industry** | 2 | FS, tech, healthcare | Other regulated | Consumer, media | — |
| **Architecture team** | 3 | 5+ architects | 3–4 architects | 1–2 architects | 0 architects |
| **Cloud posture** | 2 | Azure-primary | Azure + other | Multi-cloud no Azure | AWS/GCP only |
| **Review practice** | 3 | Active, > 10/year | Active, 5–10/year | Aspirational | None planned |
| **Compliance pressure** | 2 | Regulatory mandate | Internal audit | Optional | None |
| **Pain articulation** | 1 | Champion names specific pain | General interest | "Just exploring AI" | — |

**Maximum score: 45.** Qualification thresholds:

| Score | Qualification | Action |
|-------|--------------|--------|
| **35–45** | **Strong fit** | Prioritize; offer guided pilot |
| **25–34** | **Moderate fit** | Pursue if capacity allows; self-serve trial |
| **15–24** | **Weak fit** | Nurture; revisit when multi-cloud or other gaps close |
| **< 15** | **Poor fit** | Decline politely; suggest alternatives |

---

## 6. Persona mapping

| ICP firmographic | Primary champion | Economic buyer | Technical evaluator |
|-----------------|-----------------|----------------|---------------------|
| Mid-market (500–2K), compliance-driven | Persona 1 (Enterprise Architect) | CTO / VP Engineering | Persona 2 (Platform Eng Lead) |
| Tech company (200–5K), modernization | Persona 2 (Platform Eng Lead) | VP Engineering | Senior engineers |
| Large enterprise (5K–10K), governance mandate | Persona 1 (Enterprise Architect) | CTO | Persona 3 (CTO/VP peer review) |

See [BUYER_PERSONAS.md](BUYER_PERSONAS.md) for full persona detail.

---

## Related documents

| Doc | Use |
|-----|-----|
| [BUYER_PERSONAS.md](BUYER_PERSONAS.md) | Persona details |
| [ROI_MODEL.md](ROI_MODEL.md) | Break-even analysis grounding ICP thresholds |
| [COMPETITIVE_LANDSCAPE.md](COMPETITIVE_LANDSCAPE.md) | Market context |
| [PRICING_PHILOSOPHY.md](PRICING_PHILOSOPHY.md) | Tier alignment with ICP segments |
| [REFERENCE_NARRATIVE_TEMPLATE.md](REFERENCE_NARRATIVE_TEMPLATE.md) | Case study templates per ICP segment |
