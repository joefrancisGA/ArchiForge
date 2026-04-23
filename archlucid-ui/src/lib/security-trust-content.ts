/**
 * Engagement-metadata content for the public `/security-trust` marketing page.
 *
 * This module is the single source of truth for the marketing page; it MUST stay
 * structurally aligned with the "Recent assurance activity" table in
 * `docs/go-to-market/TRUST_CENTER.md`. The page intentionally publishes
 * **engagement metadata only** — never redacted findings, never customer names —
 * so the NDA wall stays intact while procurement gets a current, dated reference.
 */

export type AssuranceEngagementSummaryAccess = {
  readonly kind: "public" | "nda";
  readonly description: string;
  readonly href?: string;
};

export type AssuranceEngagementRow = {
  readonly id: string;
  readonly engagement: string;
  readonly vendor: string;
  readonly scope: string;
  readonly completedUtc: string;
  readonly summaryAccess: AssuranceEngagementSummaryAccess;
};

export const securityTrustEngagementRows: ReadonlyArray<AssuranceEngagementRow> = [
  {
    id: "pen-test-2026-q2-aeronova",
    engagement: "2026-Q2 third-party penetration test",
    vendor: "Aeronova Red Team LLC",
    scope: "API, operator UI, hosted SaaS data plane",
    completedUtc:
      "Engagement awarded 2026-04-21; kickoff 2026-05-06 (in flight)",
    summaryAccess: {
      kind: "nda",
      description:
        "NDA-only — email security@archlucid.com once assessor delivers redacted summary",
    },
  },
  {
    id: "owner-security-self-assessment-2026",
    engagement: "Internal owner security self-assessment",
    vendor: "ArchLucid (internal CISO ownership)",
    scope: "STRIDE-aligned control review across SOC 2 Common Criteria mapping",
    completedUtc: "2026-Q2 (latest revision tracked in SOC2_SELF_ASSESSMENT_2026.md)",
    summaryAccess: {
      kind: "public",
      description: "Public summary — SOC2_SELF_ASSESSMENT_2026.md",
      href: "https://github.com/joefrancisGA/ArchLucid/blob/main/docs/security/SOC2_SELF_ASSESSMENT_2026.md",
    },
  },
  {
    id: "accessibility-self-attestation-2026-04-22",
    engagement: "Accessibility self-attestation review",
    vendor:
      "ArchLucid (accessibility custodian, same operational team as security@)",
    scope:
      "WCAG 2.1 Level AA against operator UI top routes via axe-core + jsx-a11y",
    completedUtc: "2026-04-22 (annual cadence; next window 2027-04-22)",
    summaryAccess: {
      kind: "public",
      description: "Public — /accessibility marketing route",
      href: "/accessibility",
    },
  },
  {
    id: "chaos-game-day-quarterly-staging-2026",
    engagement: "Quarterly staging chaos exercise",
    vendor: "ArchLucid Platform / on-call",
    scope:
      "Staging-only fault injection (SQL pool exhaustion 2026-04-29; subsequent runs 2026-07-29, 2026-10-28) — production chaos out-of-scope per owner decision 2026-04-22 (PENDING_QUESTIONS.md item 34)",
    completedUtc:
      "Calendar published 2026-04-22; first run 2026-04-29 (staging)",
    summaryAccess: {
      kind: "public",
      description: "Public — closing reports under docs/quality/game-day-log/",
      href: "https://github.com/joefrancisGA/ArchLucid/blob/main/docs/quality/game-day-log/README.md",
    },
  },
];

export const SECURITY_TRUST_NDA_NOTICE =
  "Pen-test redacted summaries are available under NDA only. The public Trust Center records engagement existence and high-level posture. To request the most recent redacted summary, email security@archlucid.com.";

export const SECURITY_TRUST_REPO_TRUST_CENTER_URL =
  "https://github.com/joefrancisGA/ArchLucid/blob/main/docs/go-to-market/TRUST_CENTER.md";
