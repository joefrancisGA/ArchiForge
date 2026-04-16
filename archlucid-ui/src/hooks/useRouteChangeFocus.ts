"use client";

import { usePathname } from "next/navigation";
import { useEffect, useRef } from "react";

/**
 * After client-side navigations, moves focus to a landmark (e.g. <c>#main-content</c>) so keyboard users land in page content.
 */
export function useRouteChangeFocus(targetId: string): void {
  const pathname = usePathname();
  const previousPathname = useRef<string | null>(null);

  useEffect(() => {
    if (previousPathname.current === null) {
      previousPathname.current = pathname;

      return;
    }

    if (previousPathname.current === pathname) {
      return;
    }

    previousPathname.current = pathname;

    const timerId = window.setTimeout(() => {
      const target = document.getElementById(targetId);

      if (target !== null) {
        target.focus();
      }
    }, 100);

    return () => {
      window.clearTimeout(timerId);
    };
  }, [pathname, targetId]);
}
