import type { Metadata } from "next";
import type { ReactNode } from "react";
import { notFound } from "next/navigation";

import { isInvalidDynamicRouteToken } from "@/lib/route-dynamic-param";

export const metadata: Metadata = {
  title: "Plan detail",
  description: "Read-only improvement plan: steps, priority, and evidence link counts (59R).",
};

export default async function PlanningPlanLayout({
  children,
  params,
}: {
  children: ReactNode;
  params: Promise<{ planId: string }>;
}) {
  const { planId } = await params;

  if (isInvalidDynamicRouteToken(planId)) {
    notFound();
  }

  return children;
}
