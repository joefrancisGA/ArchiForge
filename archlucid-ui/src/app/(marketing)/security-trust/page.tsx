import type { Metadata } from "next";
import type { ReactNode } from "react";

import { MarketingSecurityTrustView } from "@/components/marketing/MarketingSecurityTrustView";

export const metadata: Metadata = {
  title: "Security & trust",
  description:
    "Engagement metadata for ArchLucid's most recent assurance activity. Pen-test redacted summaries are NDA-only.",
};

export default function MarketingSecurityTrustPage(): ReactNode {
  return <MarketingSecurityTrustView />;
}
