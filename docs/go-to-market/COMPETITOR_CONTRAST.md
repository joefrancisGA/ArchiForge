> **Scope:** Competitor contrast (honest positioning) - full detail, tables, and links in the sections below.

# Competitor contrast (honest positioning)

**Audience:** Sales engineers, architects pitching sponsors. **Not** a feature matrix for RFP checkbox wars.

## 1. Homegrown EA (Confluence + ADRs + spreadsheets)

**Where it wins:** Zero license cost; teams already know the wiki; political neutrality (“we don’t buy tools”).

**Where ArchLucid wins:** **Versioned manifests** tied to **runs**, **replay/compare**, **LLM-assisted structuring** with **faithfulness and provenance hooks** — the wiki cannot enforce “what changed between these two decisions” without heroic manual work.

**Where ArchLucid does *not* win:** Organizations that only need **lightweight documentation** and will never pay for Azure footprint or LLM usage. If the problem is “people don’t write ADRs,” ArchLucid doesn’t fix culture by itself.

## 2. Diagramming + office suite (Visio, draw.io, PowerPoint packs)

**Where it wins:** Visual polish for steering committees; offline friendly; ubiquitous.

**Where ArchLucid wins:** **Executable workflow**: agents + decision engine produce **structured outputs** (manifests, findings, traces) that can be **governed, replayed, and metered** — not only pictures.

**Where ArchLucid does *not* win:** Buyer wants **slides-only** engagement with **no API or SQL**. That’s a valid motion; ArchLucid’s ROI is diluted.

## 3. Enterprise GRC / ITSM suites (ServiceNow GRC, Jira Align at policy layer, etc.)

**Where it wins:** Established **workflow**, **approvals**, **audit language**; CIO comfort; existing integrations.

**Where ArchLucid wins:** **Architecture-specific** manifest merge, **cross-run diff**, **advisory-style findings** aligned to **architecture artifacts** — not generic tickets. Integration **out** via **integration events and webhooks** keeps ArchLucid as **system of insight** feeding the GRC **system of record**.

**Where ArchLucid does *not* win:** The buyer insists on **one vendor** for *all* evidence lifecycle and will not allow a sidecar Azure deployment.

---

**Summary:** ArchLucid is strongest when the buyer admits **manual packaging and inconsistent decision evidence** are slowing releases — and will put a **bounded pilot** on **Core Pilot** success metrics ([BUYER_JOURNEY.md](BUYER_JOURNEY.md)).
