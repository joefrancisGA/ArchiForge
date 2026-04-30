import type { Metadata } from "next";
import { notFound } from "next/navigation";
import type { ReactNode } from "react";

import { isInvalidManifestRouteId } from "@/lib/route-dynamic-param";

export const metadata: Metadata = {
  title: "Manifest",
};

export default async function ManifestLayout({
  children,
  params,
}: {
  children: ReactNode;
  params: Promise<{ manifestId: string }>;
}) {
  const { manifestId } = await params;

  if (isInvalidManifestRouteId(manifestId)) {
    notFound();
  }

  return children;
}
