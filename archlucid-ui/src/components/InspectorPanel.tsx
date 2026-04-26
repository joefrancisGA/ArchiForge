"use client";

import { X } from "lucide-react";
import { useEffect } from "react";

import { Button } from "@/components/ui/button";
import { cn } from "@/lib/utils";

export type InspectorPanelProps = {
  title: string;
  onClose: () => void;
  children: React.ReactNode;
  /** Tailwind width for docked layout; default `w-96` (24rem). */
  widthClassName?: string;
  className?: string;
  /** When false, Escape is not registered (e.g. empty docked column). */
  listenEscape?: boolean;
};

/**
 * Right-side inspector shell: title bar, close control, scrollable body. Not a modal — no focus trap.
 * Dock inside the main column (`lg:flex` sibling) or wrap in a fixed container for small-viewport sheets.
 */
export function InspectorPanel({
  title,
  onClose,
  children,
  widthClassName = "w-96",
  className,
  listenEscape = true,
}: InspectorPanelProps) {
  useEffect(() => {
    if (!listenEscape) {
      return;
    }

    function onKeyDown(e: KeyboardEvent) {
      if (e.key === "Escape") {
        onClose();
      }
    }

    window.addEventListener("keydown", onKeyDown);

    return () => {
      window.removeEventListener("keydown", onKeyDown);
    };
  }, [listenEscape, onClose]);

  return (
    <aside
      className={cn(
        "flex min-h-0 flex-col border-l border-neutral-200 bg-white shadow-md dark:border-neutral-700 dark:bg-neutral-900",
        widthClassName,
        className,
      )}
      aria-label="Inspector"
    >
      <div className="flex shrink-0 items-start justify-between gap-2 border-b border-neutral-200 px-3 py-2.5 dark:border-neutral-700">
        <h2 className="m-0 min-w-0 flex-1 truncate text-base font-semibold text-neutral-900 dark:text-neutral-100">
          {title}
        </h2>
        <Button
          type="button"
          variant="ghost"
          size="icon"
          className="shrink-0"
          aria-label="Close inspector"
          data-testid="inspector-panel-close"
          onClick={() => {
            onClose();
          }}
        >
          <X className="size-4" aria-hidden />
        </Button>
      </div>
      <div className="min-h-0 flex-1 overflow-y-auto p-3">{children}</div>
    </aside>
  );
}
