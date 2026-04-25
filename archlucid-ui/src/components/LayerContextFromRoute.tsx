"use client";

import { usePathname } from "next/navigation";

import { getLayerForRoute } from "@/lib/getLayerForRoute";

import { LayerContextStrip } from "./LayerContextStrip";

/** Client bridge: `usePathname()` → `getLayerForRoute()` → `LayerContextStrip` (App Router operator shell). */
export function LayerContextFromRoute() {
  const pathname = usePathname() ?? "/";
  return <LayerContextStrip layerId={getLayerForRoute(pathname)} />;
}
