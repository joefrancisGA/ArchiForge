> **Scope:** When ArchLucid is *not* a fit (blunt filter) - full detail, tables, and links in the sections below.

# When ArchLucid is *not* a fit (blunt filter)

**Purpose:** Save buyers and our team time. Disqualify early; do **not** promise roadmap to close bad-fit deals.

## Product / scope

- Teams that **only** need **diagrams** or **wiki pages** with **no** intention to adopt a **manifest-led** workflow.
- Organizations that **cannot** use **Azure** (hosting, identity, or data residency) for a pilot **and** will not accept a **bring-your-own-Azure** model aligned to [../FIRST_AZURE_DEPLOYMENT.md](../FIRST_AZURE_DEPLOYMENT.md).
- Buyers expecting **100% automated compliance sign-off** — ArchLucid produces **evidence and structured outputs**; **human accountability** remains (see [../EXECUTIVE_SPONSOR_BRIEF.md](../EXECUTIVE_SPONSOR_BRIEF.md)).

## Security / compliance posture

- **Unacceptable** tenant isolation (e.g. refusing scoped credentials, shared “god” SQL logins for all tenants in SaaS patterns).
- Requirements for **on-prem only** without a **documented** equivalent deployment story (fork must own **all** operational burden).
- **Mandatory SMB/SMB-on-internet** for primary artifacts — conflicts with product security stance (use private endpoints; see workspace security rule).

## Commercial / maturity

- **No named sponsor** and **no success metrics** for a pilot — success cannot be reviewed.
- Expectation of **full production HA** on **minimal pilot** budget — start with [../deployment/PILOT_PROFILE.md](../deployment/PILOT_PROFILE.md) *or* align spend before enterprise HA ([../REFERENCE_SAAS_STACK_ORDER.md](../REFERENCE_SAAS_STACK_ORDER.md)).
- Demands for **features outside V1** without acceptance of [../V1_SCOPE.md](../V1_SCOPE.md) and [../V1_DEFERRED.md](../V1_DEFERRED.md).

## When to re-open the conversation

- Sponsor assigned; **Core Pilot** metrics agreed (time-to-manifest, traceability, optional governance).
- Azure subscription + identity path accepted; security review **scheduled**, not vague “later”.
