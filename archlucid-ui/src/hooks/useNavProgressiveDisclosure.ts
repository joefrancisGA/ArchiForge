"use client";

import { useCallback, useEffect, useState } from "react";

const STORAGE_SHOW_EXTENDED = "archlucid_nav_show_extended";
const STORAGE_SHOW_ADVANCED = "archlucid_nav_show_advanced";

function readBooleanStorage(key: string, defaultValue: boolean): boolean {
  if (typeof window === "undefined") {
    return defaultValue;
  }

  try {
    const raw = window.localStorage.getItem(key);

    if (raw === null) {
      return defaultValue;
    }

    return raw === "1";
  } catch {
    return defaultValue;
  }
}

function writeBooleanStorage(key: string, value: boolean): void {
  try {
    window.localStorage.setItem(key, value ? "1" : "0");
  } catch {
    /* private mode */
  }
}

/** Shared progressive nav flags (sidebar + mobile drawer). */
export function useNavProgressiveDisclosure(): {
  mounted: boolean;
  showExtended: boolean;
  showAdvanced: boolean;
  setShowExtended: (value: boolean) => void;
  setShowAdvanced: (value: boolean) => void;
} {
  const [mounted, setMounted] = useState(false);
  const [showExtended, setShowExtendedState] = useState(false);
  const [showAdvanced, setShowAdvancedState] = useState(false);

  useEffect(() => {
    setShowExtendedState(readBooleanStorage(STORAGE_SHOW_EXTENDED, false));
    setShowAdvancedState(readBooleanStorage(STORAGE_SHOW_ADVANCED, false));
    setMounted(true);
  }, []);

  const setShowExtended = useCallback((value: boolean) => {
    setShowExtendedState(value);
    writeBooleanStorage(STORAGE_SHOW_EXTENDED, value);

    if (!value) {
      setShowAdvancedState(false);
      writeBooleanStorage(STORAGE_SHOW_ADVANCED, false);
    }
  }, []);

  const setShowAdvanced = useCallback((value: boolean) => {
    setShowAdvancedState(value);
    writeBooleanStorage(STORAGE_SHOW_ADVANCED, value);
  }, []);

  return { mounted, showExtended, showAdvanced, setShowExtended, setShowAdvanced };
}
