"use client";

import { useEffect, useState } from "react";

const QUERY = "(max-width: 1023px)";

/**
 * True when the viewport is below the `lg` Tailwind breakpoint (inspector uses sheet instead of docked column).
 */
export function useViewportNarrow(): boolean {
  const [narrow, setNarrow] = useState(false);

  useEffect(() => {
    if (typeof window.matchMedia !== "function") {
      setNarrow(false);

      return;
    }

    const mq = window.matchMedia(QUERY);

    function apply() {
      setNarrow(mq.matches);
    }

    apply();
    mq.addEventListener("change", apply);

    return () => {
      mq.removeEventListener("change", apply);
    };
  }, []);

  return narrow;
}
