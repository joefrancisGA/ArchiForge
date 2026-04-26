"use client";

import { useCallback, useEffect, useState } from "react";

import { cn } from "@/lib/utils";

const storageKey = "archlucid_color_mode";

export type ColorModePreference = "light" | "dark" | "system";

function readStoredPreference(): ColorModePreference {
  if (typeof window === "undefined") {
    return "system";
  }

  try {
    const raw = window.localStorage.getItem(storageKey);

    if (raw === "light" || raw === "dark" || raw === "system") {
      return raw;
    }
  } catch {
    // ignore
  }

  return "system";
}

function applyPreference(pref: ColorModePreference): void {
  if (typeof document === "undefined") {
    return;
  }

  const root = document.documentElement.classList;
  const prefersDark =
    typeof window !== "undefined" &&
    typeof window.matchMedia === "function" &&
    window.matchMedia("(prefers-color-scheme: dark)").matches;
  const dark = pref === "dark" || (pref === "system" && prefersDark);

  if (dark) {
    root.add("dark");
  }
  else {
    root.remove("dark");
  }
}

/**
 * Light / dark / system toggle for the operator shell. Persists to localStorage and applies `.dark` on `<html>`.
 */
export function ColorModeToggle() {
  const [preference, setPreference] = useState<ColorModePreference>("system");
  const [mounted, setMounted] = useState(false);

  useEffect(() => {
    setMounted(true);
    const initial = readStoredPreference();

    setPreference(initial);
    applyPreference(initial);
  }, []);

  useEffect(() => {
    if (!mounted || preference !== "system") {
      return;
    }

    if (typeof window.matchMedia !== "function") {
      return;
    }

    const media = window.matchMedia("(prefers-color-scheme: dark)");

    const onChange = (): void => {
      applyPreference("system");
    };

    media.addEventListener("change", onChange);

    return (): void => media.removeEventListener("change", onChange);
  }, [mounted, preference]);

  const setAndPersist = useCallback((next: ColorModePreference) => {
    setPreference(next);

    try {
      window.localStorage.setItem(storageKey, next);
    }
    catch {
      // ignore
    }

    applyPreference(next);
  }, []);

  if (!mounted) {
    return <div aria-hidden="true" className="h-8 w-8" />;
  }

  const nextMode: ColorModePreference =
    preference === "light" ? "dark" : preference === "dark" ? "system" : "light";
  const icon = preference === "light" ? "☀️" : preference === "dark" ? "🌙" : "💻";
  const label = `Theme: ${preference}. Click to switch to ${nextMode}.`;

  return (
    <button
      type="button"
      className={cn(
        "auth-panel-focus flex h-8 w-8 items-center justify-center rounded-md border border-neutral-200 bg-white text-sm transition-colors hover:bg-neutral-100 dark:border-neutral-700 dark:bg-neutral-800 dark:hover:bg-neutral-700",
      )}
      aria-label={label}
      title={label}
      onClick={() => setAndPersist(nextMode)}
    >
      <span aria-hidden className="text-xs">{icon}</span>
    </button>
  );
}
