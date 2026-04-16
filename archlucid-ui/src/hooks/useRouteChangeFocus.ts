"use client";

import { usePathname } from "next/navigation";
import { useLayoutEffect, useRef } from "react";

/**
 * After client-side navigations, moves focus to a landmark (e.g. <c>#main-content</c>) so keyboard users land in page content.
 * Uses <c>useLayoutEffect</c> so focus runs in the same commit as the new route (before paint), matching e2e and avoiding a delayed <c>setTimeout</c> race.
 */
export function useRouteChangeFocus(targetId: string): void {
  const pathname = usePathname();
  const previousPathname = useRef<string | null>(null);

  useLayoutEffect(() => {
    if (previousPathname.current === null) {
      previousPathname.current = pathname;

      return;
    }

    if (previousPathname.current === pathname) {
      return;
    }

    const target = document.getElementById(targetId);

    if (target !== null) {
      target.focus();
    }

    previousPathname.current = pathname;
  }, [pathname, targetId]);
}
