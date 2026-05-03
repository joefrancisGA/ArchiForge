import { useEffect, useState } from "react";

export function useBasicAdvancedToggle(storageKey: string) {
  const [isAdvanced, setIsAdvanced] = useState(false);

  useEffect(() => {
    try {
      const stored = window.localStorage.getItem(storageKey);
      if (stored === "true") {
        setIsAdvanced(true);
      }
    } catch {
      // ignore
    }
  }, [storageKey]);

  const toggle = () => {
    setIsAdvanced((prev) => {
      const next = !prev;
      try {
        window.localStorage.setItem(storageKey, String(next));
      } catch {
        // ignore
      }
      return next;
    });
  };

  return { isAdvanced, toggle };
}
