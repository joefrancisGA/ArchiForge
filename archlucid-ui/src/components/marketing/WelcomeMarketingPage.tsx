"use client";

import Link from "next/link";

import { Button } from "@/components/ui/button";
import { MarketingTierPricingSection } from "@/components/marketing/MarketingTierPricingSection";
import { BRAND_CATEGORY } from "@/lib/brand-category";

/** §3 “30-second pitch” in `docs/go-to-market/POSITIONING.md` — category label flows through `BRAND_CATEGORY`. */
const HERO_PITCH = `ArchLucid is an ${BRAND_CATEGORY} platform. You describe a system you want to build, and our AI agents analyze it for topology, cost, compliance, and design quality — then produce a versioned manifest with every finding traced and explained. Think of it as an AI-powered architecture review board that runs in minutes instead of weeks, with a full audit trail.`;

const PILLARS: { title: string; body: string }[] = [
  {
    title: "AI-native architecture analysis",
    body: "ArchLucid is not an architecture documentation tool with AI bolted on. It was built from day one around a multi-agent pipeline — four specialized AI agents (Topology, Cost, Compliance, Critic) analyze architecture requests through a structured pipeline: context ingestion → knowledge graph → findings → decisioning → artifact synthesis. The result is a versioned golden manifest with structured findings, not a chat conversation that disappears.",
  },
  {
    title: "Auditable decision trail",
    body: "Every architecture recommendation ArchLucid produces comes with a complete chain of evidence. The ExplainabilityTrace on every finding records what was examined, what rules were applied, what decisions were taken, and why. The provenance graph connects evidence to decisions to manifest entries to artifacts. This is not \"AI said so\" — it is \"AI analyzed these inputs, applied these rules, and reached this conclusion, and here is the full trail.\"",
  },
  {
    title: "Enterprise governance",
    body: "Architecture decisions in ArchLucid are not just analyzed — they are governed. Policy packs define compliance rules. Approval workflows enforce segregation of duties. Pre-finalization gates block manifests when findings exceed severity thresholds. Approval SLAs track time-to-review and escalate breaches via webhooks. And 78 typed audit events in an append-only SQL store provide the evidence trail that regulators and auditors expect.",
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
            — full page, no signup.
          </p>
          <p className="mt-3 text-sm text-neutral-600 dark:text-neutral-400">
            <Link className="text-teal-700 underline underline-offset-2 dark:text-teal-300" href="/WORKED_EXAMPLE_ROI.pdf">
              See worked example (PDF)
            </Link>{" "}
            — Contoso sample ROI (fictional tenant); markdown companion in{" "}
            <code className="text-xs">docs/go-to-market/WORKED_EXAMPLE_ROI.md</code>.
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
            </li>
          ))}
        </ul>
      </section>

      <MarketingTierPricingSection
        sectionHeadingId="pricing-heading"
        sectionTitle="Packaging overview"
        sectionIntro="Figures are loaded from the published pricing document at build time — not hard-coded in the UI bundle."
        signupHref="/signup"
        showSignupCallToAction={false}
      />
    </main>
  );
}
