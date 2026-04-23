> **Scope:** Onboarding wizards (operator UI) - full detail, tables, and links in the sections below.

> **Spine doc:** [Five-document onboarding spine](../FIRST_5_DOCS.md). Read this file only if you have a specific reason beyond those five entry documents.


# Onboarding wizards (operator UI)

> **Install order moved.** See [INSTALL_ORDER.md](../INSTALL_ORDER.md). This page describes in-product routes only (week-one tasks after install).

ArchLucid ships **two** complementary surfaces:

| Route | Purpose |
|-------|--------|
| **`/onboarding`** and **`/onboarding/start`** | General **first-run orientation** and trial-aligned flows (sample run links, education steps). |
| **`/onboard`** | **Core Pilot — first session** linear wizard: create architecture run → optional fake-result seed (non-Production) → commit golden manifest → hand-off link. Requires **`ExecuteAuthority`**. |

The **`/onboard`** path exists so pilot teams can complete a **first manifest commit** without navigating Advanced Analysis or Enterprise Controls. It calls the same authenticated API helpers as the main shell (`createArchitectureRun`, `seedFakeArchitectureRunResults`, `commitArchitectureRun`).

**Product metric:** the first successful manifest commit per tenant can increment **`archlucid_first_session_completed_total`** when SQL persistence and **`TenantOnboardingState`** are enabled (see [`docs/OBSERVABILITY.md`](OBSERVABILITY.md)).

**Navigation:** configured in `archlucid-ui/src/lib/nav-config.ts` under Core Pilot.
