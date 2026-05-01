import { notFound, permanentRedirect } from "next/navigation";
import type { ReactNode } from "react";

import { canonicalizeDemoRunId, demoRunUrlRequiresCanonicalRedirect } from "@/lib/demo-run-canonical";
import { isInvalidGuidOrSlugRouteToken } from "@/lib/route-dynamic-param";

export default async function ExecutiveReviewRunLayout({
  children,
  params,
}: {
  children: ReactNode;
  params: Promise<{ runId: string }>;
}) {
  const { runId } = await params;

  if (isInvalidGuidOrSlugRouteToken(runId)) {
    notFound();
  }

  if (demoRunUrlRequiresCanonicalRedirect(runId)) {
    permanentRedirect(`/executive/reviews/${encodeURIComponent(canonicalizeDemoRunId(runId))}`);
  }

  return children;
}
