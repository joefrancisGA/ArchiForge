import type { Metadata } from "next";

// Public marketing home is `/welcome` (not `app/(marketing)/page.tsx`) because `app/(operator)/page.tsx` already owns `/`.
import { WelcomeMarketingPage } from "@/components/marketing/WelcomeMarketingPage";
import { BRAND_CATEGORY, BRAND_CATEGORY_LEGACY } from "@/lib/brand-category";

export const metadata: Metadata = {
  title: "Welcome",
  description: `ArchLucid ${BRAND_CATEGORY} — trial signup and product overview.`,
  other: {
    "x-archlucid-brand-category-legacy": BRAND_CATEGORY_LEGACY,
  },
};

export default function WelcomePage() {
  return (
    <main>
      <WelcomeMarketingPage />
    </main>
  );
}
