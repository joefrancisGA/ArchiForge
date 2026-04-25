import type { Metadata } from "next";

import { WhyArchlucidMarketingView } from "./WhyArchlucidMarketingView";
import { BRAND_CATEGORY_LEGACY } from "@/lib/brand-category";
import { type WhyHardComparisonRow, WHY_COMPARISON_ROWS_SERIALIZED } from "@/lib/why-comparison";

const FRONT_DOOR_ROWS: readonly WhyHardComparisonRow[] = JSON.parse(
  WHY_COMPARISON_ROWS_SERIALIZED,
) as readonly WhyHardComparisonRow[];

export const metadata: Metadata = {
  title: "ArchLucid · Why ArchLucid",
  description:
    "How ArchLucid compares to common enterprise architecture tools — AI orchestration, governance, and audit evidence grounded in shipped V1 capabilities.",
  robots: { index: true, follow: true },
  // Hidden HTML <meta> carrying the legacy brand-category phrase for ~30 days
  // post-rebrand so SEO redirect handlers and analytics tag mappers can still
  // resolve historical inbound links / search-result snippets that reference
  // "AI Architecture Intelligence". Removed by the closing PR-7 of the rebrand
  // workstream — see docs/architecture/REBRAND_WORKSTREAM_2026_04_23.md.
  other: {
    "x-archlucid-brand-category-legacy": BRAND_CATEGORY_LEGACY,
  },
};

export default function WhyMarketingPage() {
  return <WhyArchlucidMarketingView frontDoorRows={FRONT_DOOR_ROWS} />;
}
