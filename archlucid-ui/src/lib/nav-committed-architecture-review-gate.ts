import type { NavLinkItem } from "@/lib/nav-config";

/**
 * Sidebar/palette narrowing before the first committed golden-manifest review (`CurrentPrincipal.hasCommittedArchitectureReview`).
 * Allowed: Home, Reviews list/new, active review detail under `/reviews/...`.
 */
export function pathnameEligibleBeforeFirstCommittedArchitectureReview(pathWithoutQuery: string): boolean {
  if (pathWithoutQuery === "/" || pathWithoutQuery === "/reviews") {
    return true;
  }

  if (pathWithoutQuery === "/reviews/new") {
    return true;
  }

  if (pathWithoutQuery.startsWith("/reviews/")) {
    return true;
  }

  return false;
}

/** Outermost gate: shrink operator nav until the tenant has a committed architecture review. */
export function filterNavLinksByCommittedArchitectureReviewGate(
  links: ReadonlyArray<NavLinkItem>,
  hasCommittedArchitectureReview: boolean,
): NavLinkItem[] {
  if (hasCommittedArchitectureReview) {
    return [...links];
  }

  return links.filter((link) =>
    pathnameEligibleBeforeFirstCommittedArchitectureReview(link.href.split("?")[0] ?? ""),
  );
}
