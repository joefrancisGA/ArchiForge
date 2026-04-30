"use client";

import Link from "next/link";
import type { ReactNode } from "react";

import { Button } from "@/components/ui/button";
import { MarketingTierPricingSection } from "@/components/marketing/MarketingTierPricingSection";
import { BRAND_CATEGORY } from "@/lib/brand-category";
import { DEFAULT_GITHUB_BLOB_BASE } from "@/lib/docs-public-base";

/** Homepage hero pitch — category label flows through `BRAND_CATEGORY`. */
const HERO_PITCH = `ArchLucid is an ${BRAND_CATEGORY} platform. You describe a system you want to build, and our AI agents analyze it for topology, cost, compliance, and design quality — then produce a versioned manifest with every finding traced and explained. Think of it as an AI-powered architecture review board that runs in minutes instead of weeks, with a full audit trail.`;

type WelcomeVerifyLink = {
  readonly label: string;
  readonly href: string;
};

type WelcomePillar = {
  readonly title: string;
  readonly body: string;
  readonly verify: readonly WelcomeVerifyLink[];
};

function WelcomePillarVerifyLinks(props: { readonly links: readonly WelcomeVerifyLink[] }): ReactNode {
  const { links } = props;

  return (
    <p className="mt-3 text-xs text-neutral-600 dark:text-neutral-400">
      <span className="font-semibold text-neutral-700 dark:text-neutral-300">Verify:</span>{" "}
      {links.map((link, index) => (
        <span key={`${link.href}|${link.label}`}>
          {index > 0 ? " · " : null}
          {link.href.startsWith("http") ? (
            <a
              className="text-teal-700 underline underline-offset-2 dark:text-teal-300"
              href={link.href}
              target="_blank"
              rel="noopener noreferrer"
            >
              {link.label}
            </a>
          ) : link.href.endsWith(".zip") ? (
            <a className="text-teal-700 underline underline-offset-2 dark:text-teal-300" href={link.href} download>
              {link.label}
            </a>
          ) : (
            <Link className="text-teal-700 underline underline-offset-2 dark:text-teal-300" href={link.href}>
              {link.label}
            </Link>
          )}
        </span>
      ))}
    </p>
  );
}

const PILLARS: readonly WelcomePillar[] = [
  {
    title: "AI-native architecture analysis",
    body: "ArchLucid is not an architecture documentation tool with AI bolted on. It was built from day one around a multi-agent pipeline — specialized agents analyze architecture requests through a structured flow from intake to evidence graph, findings, decisioning, and artifact synthesis. The result is a versioned, finalized architecture manifest with structured findings, not a chat conversation that disappears.",
    verify: [
      { label: "/see-it", href: "/see-it" },
      { label: "/demo/preview", href: "/demo/preview" },
      { label: "Product scope overview", href: `${DEFAULT_GITHUB_BLOB_BASE}/docs/library/V1_SCOPE.md` },
    ],
  },
  {
    title: "Auditable decision trail",
    body: "Every architecture recommendation ArchLucid produces is designed to ship with a chain of evidence: explainability metadata on findings records what was examined, what rules were applied, what decisions were taken, and why. Provenance and graph surfaces connect evidence to decisions and artifacts for investigation in the operator shell — not an anonymous “AI said so” reply.",
    verify: [
      { label: "/demo/explain", href: "/demo/explain" },
      { label: "Knowledge graph overview", href: `${DEFAULT_GITHUB_BLOB_BASE}/docs/library/KNOWLEDGE_GRAPH.md` },
    ],
  },
  {
    title: "Enterprise governance",
    body: "Operate-layer governance is configuration-driven: policy packs, approval workflows with segregation of duties, optional pre-commit gates that block commits when findings exceed thresholds, SLA tracking with webhook escalation on breach, and typed audit events in an append-only store with CSV export — all within the published V1 operator surface. That is the evidence profile buyers use for diligence; it is not a disposable chat log.",
    verify: [
      { label: "/trust", href: "/trust" },
      { label: "Evidence pack (ZIP)", href: "/v1/marketing/trust-center/evidence-pack.zip" },
      { label: "Audit event coverage", href: `${DEFAULT_GITHUB_BLOB_BASE}/docs/library/AUDIT_COVERAGE_MATRIX.md` },
      { label: "Pre-commit governance", href: `${DEFAULT_GITHUB_BLOB_BASE}/docs/library/PRE_COMMIT_GOVERNANCE_GATE.md` },
    ],
  },
];
/** Public marketing landing: hero, pillars, pricing cards from `/pricing.json`, primary CTA to `/see-it`. */
export function WelcomeMarketingPage() {
  return (
    <main className="mx-auto max-w-5xl px-4 py-10">
      <section aria-labelledby="hero-heading" className="mb-12 text-center">
        <p
          className="text-sm font-semibold uppercase tracking-wide text-teal-800 dark:text-teal-300"
          data-testid="welcome-brand-category-eyebrow"
        >
          {BRAND_CATEGORY}
        </p>
        <h1 id="hero-heading" className="mt-2 text-3xl font-bold tracking-tight text-neutral-900 dark:text-neutral-50 sm:text-4xl">
          Ship governed architecture decisions faster
        </h1>
        <p
          className="mx-auto mt-4 max-w-3xl text-left text-base leading-relaxed text-neutral-700 dark:text-neutral-300 sm:text-center"
          data-testid="welcome-brand-category-paragraph"
        >
          {HERO_PITCH}
        </p>
        <div className="mt-8 flex flex-wrap justify-center gap-3">
          <Button asChild variant="primary" size="lg">
            <Link href="/see-it">See it in 30 seconds</Link>
          </Button>
          <Button asChild variant="outline" size="lg">
            <Link href="/signup">Start free trial</Link>
          </Button>
          <Button asChild variant="secondary" size="lg">
            <Link href="/auth/signin">Sign in</Link>
          </Button>
        </div>
        <div className="mt-6 text-center">
          <p className="text-sm text-neutral-600 dark:text-neutral-400">
            Same finalized demo as{" "}
            <Link className="text-teal-700 underline underline-offset-2 dark:text-teal-300" href="/demo/preview">
              /demo/preview
            </Link>{" "}
            — full page, no signup.{" "}
            <Link className="text-teal-700 underline underline-offset-2 dark:text-teal-300" href="/demo/explain">
              See a live run (explain + provenance)
            </Link>
            .
          </p>
          <p className="mt-3 text-sm text-neutral-600 dark:text-neutral-400">
            <Link className="text-teal-700 underline underline-offset-2 dark:text-teal-300" href="/WORKED_EXAMPLE_ROI.pdf">
              See worked example (PDF)
            </Link>{" "}
            — Contoso sample ROI (fictional tenant). Ask your account team for the written walkthrough companion.
          </p>
          <p className="mt-3 text-xs text-neutral-500 dark:text-neutral-400">
            <span className="font-semibold text-neutral-600 dark:text-neutral-300">Verify:</span>{" "}
            <Link className="text-teal-700 underline underline-offset-2 dark:text-teal-300" href="/why">
              /why
            </Link>
            {" · "}
            <Link className="text-teal-700 underline underline-offset-2 dark:text-teal-300" href="/trust">
              /trust
            </Link>
            {" · "}
            <Link className="text-teal-700 underline underline-offset-2 dark:text-teal-300" href="/demo/explain">
              /demo/explain
            </Link>
          </p>
        </div>
      </section>

      <section aria-labelledby="pillars-heading" className="mb-14">
        <h2 id="pillars-heading" className="mb-6 text-2xl font-semibold text-neutral-900 dark:text-neutral-100">
          Three pillars
        </h2>
        <ul className="grid gap-6 md:grid-cols-3">
          {PILLARS.map((pillar) => (
            <li
              key={pillar.title}
              className="rounded-lg border border-neutral-200 bg-white p-5 shadow-sm dark:border-neutral-800 dark:bg-neutral-900"
            >
              <h3 className="text-lg font-semibold text-neutral-900 dark:text-neutral-100">{pillar.title}</h3>
              <p className="mt-2 text-sm leading-relaxed text-neutral-700 dark:text-neutral-300">{pillar.body}</p>
              <WelcomePillarVerifyLinks links={pillar.verify} />
            </li>
          ))}
        </ul>
      </section>

      <MarketingTierPricingSection
        sectionHeadingId="pricing-heading"
        sectionTitle="Packaging overview"
        sectionIntro="Tier summaries reflect the current published price list. Your account team can confirm licensing and any volume discounts."
        signupHref="/signup"
        showSignupCallToAction={false}
      />
    </main>
  );
}
