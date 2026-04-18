# ArchLucid Operator Decision Guide

**Audience:** pilot operators, architecture reviewers, governance operators, and customer teams who need to know which ArchLucid layer to use next without relying on founder-level interpretation.

**Status:** Practical V1 usage guidance. This document explains **when to stay on the Core Pilot path, when to expand into Advanced Analysis, and when Enterprise Controls are worth using**.

**Related:** [CORE_PILOT.md](CORE_PILOT.md) · [PRODUCT_PACKAGING.md](PRODUCT_PACKAGING.md) · [PILOT_ROI_MODEL.md](PILOT_ROI_MODEL.md) · [operator-shell.md](operator-shell.md)

---

## 1. The default rule

Start with **Core Pilot**.

Do **not** move into Advanced Analysis or Enterprise Controls just because those features exist. Move only when a real question appears that the Core Pilot path does not answer well enough.

That keeps the product easier to operate and makes pilot value easier to judge.

---

## 2. Which layer should I use?

| Situation | Stay in Core Pilot | Move to Advanced Analysis | Move to Enterprise Controls |
|---|---|---|---|
| You need a reviewable architecture package from a request | Yes | No | No |
| You need to compare two architecture outputs | No | Yes | No |
| You need to explain what changed between runs | No | Yes | No |
| You need a provenance or architecture graph | No | Yes | No |
| You need approval workflow, policy control, or audit evidence | No | No | Yes |
| You need alert routing, governance dashboard, or audit log export | No | No | Yes |
| You are still proving basic pilot value | Yes | Only if needed | Usually no |
| A sponsor is asking whether the product saved time or reduced manual effort | Yes | Optional | Optional |

---

## 3. Core Pilot — use this unless you have a reason not to

Use **Core Pilot** when the question is:

> Can we go from architecture request to committed manifest and reviewable artifacts faster, with less manual assembly and better evidence?

### Use Core Pilot when you need to:

- create a run,
- execute a run,
- commit a manifest,
- review artifacts,
- export a package,
- judge whether the first pilot created value.

### Ignore these for now unless you need them:

- Compare
- Replay
- Graph
- Ask
- Advisory
- Pilot feedback
- Governance dashboard
- Policy packs
- Audit log
- Alert routing and tuning

If you are still trying to prove the first pilot, staying in Core Pilot is usually the right choice.

---

## 4. Advanced Analysis — use this when the next question is analytical

Use **Advanced Analysis** when the question is:

> What changed, why did it change, or what does the architecture/provenance picture look like in more detail?

### Move to Advanced Analysis when you need to:

- compare two runs,
- replay a run or comparison,
- inspect provenance or architecture graph views,
- ask follow-up questions against architecture context,
- collect richer product-learning signals.

### Do not move here just because it looks interesting

Advanced Analysis is useful when there is a real review, debugging, or architecture-learning question.

If your real goal is still simply to prove that ArchLucid speeds up architecture packaging and review, you can usually ignore this layer for the first pass.

---

## 5. Enterprise Controls — use this when the next question is governance or trust

Use **Enterprise Controls** when the question is:

> How do we govern, audit, approve, monitor, and operationalize architecture decisions at scale?

### Move to Enterprise Controls when you need to:

- require approvals,
- enforce policy packs,
- use a pre-commit governance gate,
- export audit events,
- review compliance drift,
- configure alert rules, routing, or simulation,
- support governance, audit, or security stakeholders directly.

### Do not move here too early

Enterprise Controls are valuable, but they are not required to prove the first Core Pilot result.

If you have not yet shown that ArchLucid improves speed, packaging effort, or evidence quality, start there first.

---

## 6. What to do next after a successful Core Pilot

Use this order unless you have a strong reason to change it:

1. **Core Pilot** — prove the product can produce a reviewable package and save effort.
2. **Advanced Analysis** — answer change, replay, or graph questions if they become relevant.
3. **Enterprise Controls** — add governance, audit, and compliance features when the organization is ready to operationalize the workflow.

This is the safest path for most pilots.

---

## 7. Fast decision rules

### Stay in Core Pilot if:

- the pilot is still proving basic value,
- the team mainly needs a reviewable output,
- the sponsor mainly cares about speed, evidence, and reduced manual effort.

### Use Advanced Analysis if:

- reviewers are asking what changed,
- architecture teams need replay or graph visibility,
- you are comparing alternatives or tracking evolution.

### Use Enterprise Controls if:

- governance teams are now involved,
- audit evidence matters,
- approvals or policy enforcement are becoming part of the real workflow,
- security or compliance stakeholders need product-level support.

---

## 8. What still requires judgment

This guide reduces ambiguity, but it does not remove all judgment.

You still need to decide:

- which use case is the best pilot candidate,
- when a sponsor question is important enough to justify moving beyond Core Pilot,
- and when governance depth is truly needed versus merely interesting.

The goal is not to eliminate judgment. The goal is to make the default path obvious enough that expert interpretation is no longer required for routine decisions.
