"use client";

import { usePathname } from "next/navigation";

import { getLayerForRoute } from "@/lib/getLayerForRoute";

import { LayerContextStrip } from "./LayerContextStrip";

/** Client bridge: `usePathname()` → `getLayerForRoute()` → `LayerContextStrip` (App Router operator shell). */
export function LayerContextFromRoute() {
  const pathname = usePathname() ?? "/";

  // Home already carries pilot context in the hero; avoid a second mission strip that reads like a weak breadcrumb.
  // New request is the primary create flow — keep the header uncluttered like home.
  if (pathname === "/" || pathname === "/reviews/new") {
    return null;
  }

  return <LayerContextStrip layerId={getLayerForRoute(pathname)} />;
}
