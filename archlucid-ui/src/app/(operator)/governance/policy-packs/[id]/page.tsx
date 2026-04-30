import { notFound } from "next/navigation";

import { isInvalidDynamicRouteToken } from "@/lib/route-dynamic-param";

import { PolicyPackDetailClient } from "./PolicyPackDetailClient";

export default async function PolicyPackDetailPage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = await params;

  if (isInvalidDynamicRouteToken(id)) {
    notFound();
  }

  return <PolicyPackDetailClient policyPackId={id.trim()} />;
}
