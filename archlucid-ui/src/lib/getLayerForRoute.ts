import { NAV_GROUPS } from "@/lib/nav-config";

/**
 * Product layer (buyer context) for operator shell — aligned with `NAV_GROUPS[].id` in `nav-config.ts` (read-only;
 * this module does not modify that file).
 */
export type LayerId = "pilot" | "operate-analysis" | "operate-governance";

function hrefToPathname(href: string): string {
  try {
    return new URL(href, "https://archlucid.invalid").pathname;
  } catch {
    return href.split("?")[0] ?? href;
  }
}

function pathMatchesPathname(pathname: string, linkPath: string): boolean {
  if (linkPath === "/") {
    return pathname === "/";
  }

  if (pathname === linkPath) {
    return true;
  }

  return pathname.startsWith(`${linkPath}/`);
}

type NavPathMatch = { groupId: LayerId; path: string; pathLength: number };

const LAYER_GROUP_ORDER: ReadonlyArray<LayerId> = [
  "pilot",
  "operate-analysis",
  "operate-governance"
];

const NAV_PATH_MATCHES: ReadonlyArray<NavPathMatch> = (() => {
  const rows: NavPathMatch[] = [];
  for (const g of NAV_GROUPS) {
    for (const link of g.links) {
      const p = hrefToPathname(link.href);
      if (g.id === "pilot" || g.id === "operate-analysis" || g.id === "operate-governance") {
        rows.push({ groupId: g.id, path: p, pathLength: p.length });
      }
    }
  }

  // Longest nav path wins so `/governance/dashboard` beats `/governance` and `/runs/new` beats `/runs`.
  return rows
    .slice()
    .sort(
      (a, b) =>
        b.pathLength - a.pathLength
        || LAYER_GROUP_ORDER.indexOf(a.groupId) - LAYER_GROUP_ORDER.indexOf(b.groupId)
    );
})();

/**
 * Resolves the operator shell’s current product layer from a pathname (no query string) by
 * taking the **longest** `NAV_GROUPS` link path that matches, then that link’s group id.
 * Unmatched pathnames fall back to `pilot` (the default Core Pilot layer).
 */
export function getLayerForRoute(pathname: string): LayerId {
  const normalized = pathname && pathname.length > 0 ? pathname : "/";
  for (const m of NAV_PATH_MATCHES) {
    if (pathMatchesPathname(normalized, m.path)) {
      return m.groupId;
    }
  }

  return "pilot";
}
