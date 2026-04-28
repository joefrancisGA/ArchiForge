# Sample policy pack — TOGAF ADM gate mapping

**Purpose:** Starter **policy pack + compliance rules** that label how common TOGAF ADM checkpoints can appear as ArchLucid policy controls. This is **not** a complete TOGAF certification pack; it is a template for linking ADM conversational gates to persisted architecture evidence (`manifest`, `findings`, `audit`).

## Files

| File | Role |
|------|------|
| `policy-pack.json` | Pack metadata and `complianceRuleKeys[]` referencing `compliance-rules.json`. |
| `compliance-rules.json` | Executable-style rules with illustrative `ruleId`s you can remap to your authoring tool. |

## How to adopt

1. Copy the folder beside your vertical packs under `templates/policy-packs/`.
2. Replace short descriptions with your organization’s mandated controls and RACI checkpoints.
3. Publish through the Operator **Policy packs** UI or your CI pipeline that already promotes JSON policy artifacts.

Cross-links: **`docs/library/GOVERNANCE.md`** (effective governance merges), **`docs/library/GLOSSARY.md`**, ADM primer in your EA practice manual.
