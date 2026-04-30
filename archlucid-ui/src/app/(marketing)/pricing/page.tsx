import type { Metadata } from "next";

import { MarketingPricingPublicCutoverNotice } from "@/components/marketing/MarketingPricingPublicCutoverNotice";
import { MarketingPricingQuotePanel } from "@/components/marketing/MarketingPricingQuotePanel";
import { MarketingTierPricingSection } from "@/components/marketing/MarketingTierPricingSection";
import { buildPricingSignupHref } from "@/lib/marketing/pricing-signup-href";
import { BRAND_CATEGORY, BRAND_CATEGORY_LEGACY } from "@/lib/brand-category";

export const metadata: Metadata = {
  title: "Pricing",
  description: `ArchLucid ${BRAND_CATEGORY} — packaging and pricing overview — start a free trial.`,
  other: {
    "x-archlucid-brand-category-legacy": BRAND_CATEGORY_LEGACY,
  },
};

type PricingPageProps = {
  searchParams: Promise<Record<string, string | string[] | undefined>>;
};

export default async function PricingPage(props: PricingPageProps) {
  const searchParams = await props.searchParams;
  const signupHref = buildPricingSignupHref(searchParams);

  return (
    <main className="mx-auto max-w-5xl px-4 py-10">
      <p
        className="mb-6 max-w-3xl text-sm leading-relaxed text-neutral-700 dark:text-neutral-300"
        data-testid="pricing-brand-category-paragraph"
      >
        ArchLucid is an {BRAND_CATEGORY} offering. Compare Standard, Professional, and Enterprise capability bundles
        below, then start a trial or contact sales for a formal quote.
      </p>
      <MarketingTierPricingSection
        sectionHeadingId="pricing-page-heading"
        sectionTitle="Pricing"
        sectionIntro="Tier summaries reflect the current published price list. Your account team can confirm licensing and any volume discounts."
        signupHref={signupHref}
        signupCallToActionLabel="Start trial"
      />
      <MarketingPricingPublicCutoverNotice />
      <MarketingPricingQuotePanel />
    </main>
  );
}
