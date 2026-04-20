> **Scope:** Onboarding wizards (operator UI) - full detail, tables, and links in the sections below.

# Onboarding wizards (operator UI)

ArchLucid ships **two** complementary surfaces:

| Route | Purpose |
|-------|--------|
| **`/onboarding`** and **`/onboarding/start`** | General **getting started** and trial-aligned flows (sample run links, education steps). |
| **`/onboard`** | **Core Pilot — first session** linear wizard: create architecture run → optional fake-result seed (non-Production) → commit golden manifest → hand-off link. Requires **`ExecuteAuthority`**. |

The **`/onboard`** path exists so pilot teams can complete a **first manifest commit** without navigating Advanced Analysis or Enterprise Controls. It calls the same authenticated API helpers as the main shell (`createArchitectureRun`, `seedFakeArchitectureRunResults`, `commitArchitectureRun`).

**Product metric:** the first successful manifest commit per tenant can increment **`archlucid_first_session_completed_total`** when SQL persistence and **`TenantOnboardingState`** are enabled (see [`docs/OBSERVABILITY.md`](OBSERVABILITY.md)).

**Navigation:** configured in `archlucid-ui/src/lib/nav-config.ts` under Core Pilot.
