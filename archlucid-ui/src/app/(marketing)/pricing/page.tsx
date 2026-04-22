import type { Metadata } from "next";

import { MarketingPricingPublicCutoverNotice } from "@/components/marketing/MarketingPricingPublicCutoverNotice";
import { MarketingPricingQuotePanel } from "@/components/marketing/MarketingPricingQuotePanel";
import { MarketingTierPricingSection } from "@/components/marketing/MarketingTierPricingSection";
import { buildPricingSignupHref } from "@/lib/marketing/pricing-signup-href";

export const metadata: Metadata = {
  title: "Pricing",
  description: "ArchLucid packaging and pricing overview — start a free trial.",
};

type PricingPageProps = {
  searchParams: Promise<Record<string, string | string[] | undefined>>;
};

export default async function PricingPage(props: PricingPageProps) {
  const searchParams = await props.searchParams;
  const signupHref = buildPricingSignupHref(searchParams);

  return (
    <main className="mx-auto max-w-5xl px-4 py-10">
      <MarketingTierPricingSection
        sectionHeadingId="pricing-page-heading"
        sectionTitle="Pricing"
        sectionIntro="Figures are loaded from the published pricing document at build time — not hard-coded in the UI bundle."
        signupHref={signupHref}
        signupCallToActionLabel="Start trial"
      />
      <MarketingPricingPublicCutoverNotice />
      <MarketingPricingQuotePanel />
    </main>
  );
}
