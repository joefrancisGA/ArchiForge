"use client";

import {
  useCallback,
  useEffect,
  useId,
  useRef,
  useState,
} from "react";

import { contextualHelpByKey, toDocsBlobUrl } from "@/lib/contextual-help-content";
import { cn } from "@/lib/utils";

export type ContextualHelpPlacement = "top" | "right" | "bottom" | "left";

export type ContextualHelpProps = {
  helpKey: string;
  placement?: ContextualHelpPlacement;
  /** Optional class on the trigger button wrapper. */
  className?: string;
};

const placementClasses: Record<
  ContextualHelpPlacement,
  { panel: string; arrow: string }
> = {
  bottom: { panel: "left-0 top-full mt-1.5", arrow: "left-3 -top-1" },
  top: { panel: "bottom-full left-0 mb-1.5", arrow: "left-3 -bottom-1" },
  right: { panel: "left-full top-0 ml-1.5", arrow: "-left-1 top-2" },
  left: { panel: "right-full top-0 mr-1.5", arrow: "-right-1 top-2" },
};

/**
 * In-context (?) help: click or keyboard toggles; hover does not (avoids clashing with long-press readers).
 * Dismiss with Escape or pointer outside. Content comes from `contextualHelpByKey` in
 * `src/lib/contextual-help-content.ts`.
 */
export function ContextualHelp({ helpKey, placement = "bottom", className }: ContextualHelpProps) {
  const [open, setOpen] = useState(false);
  const [hover, setHover] = useState(false);
  const visible = open || hover;
  const rootId = useId();
  const tooltipId = `${rootId}-tooltip`;
  const triggerRef = useRef<HTMLButtonElement>(null);
  const panelRef = useRef<HTMLDivElement>(null);
  const entry = contextualHelpByKey[helpKey];
  const place = placementClasses[placement];

  const close = useCallback(() => {
    setOpen(false);
    setHover(false);
  }, []);

  useEffect(() => {
    if (!visible) {
      return;
    }

    const onKey = (e: KeyboardEvent) => {
      if (e.key === "Escape") {
        e.preventDefault();
        close();
        triggerRef.current?.focus();
      }
    };

    const onPointerDown = (e: PointerEvent) => {
      const t = e.target as Node;

      if (triggerRef.current?.contains(t) || panelRef.current?.contains(t)) {
        return;
      }

      close();
    };

    document.addEventListener("keydown", onKey);
    document.addEventListener("pointerdown", onPointerDown, true);

    return () => {
      document.removeEventListener("keydown", onKey);
      document.removeEventListener("pointerdown", onPointerDown, true);
    };
  }, [close, visible]);

  if (entry == null) {
    return null;
  }

  const { text, learnMoreUrl } = entry;
  const moreHref = learnMoreUrl != null ? toDocsBlobUrl(learnMoreUrl) : null;

  return (
    <span
      className={cn("relative inline-flex items-center", className)}
      onPointerEnter={() => {
        setHover(true);
      }}
      onPointerLeave={() => {
        setHover(false);
      }}
    >
      <button
        ref={triggerRef}
        type="button"
        className="inline-flex h-5 w-5 shrink-0 items-center justify-center rounded-full border border-neutral-400 bg-white text-[10px] font-bold leading-none text-neutral-600 shadow-sm hover:border-teal-600 hover:text-teal-800 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-1 focus-visible:outline-teal-600 dark:border-neutral-500 dark:bg-neutral-900 dark:text-neutral-300 dark:hover:border-teal-500 dark:hover:text-teal-200"
        aria-expanded={visible}
        aria-controls={visible ? tooltipId : undefined}
        aria-describedby={visible ? tooltipId : undefined}
        aria-label={`Help: ${helpKey}`}
        onClick={() => {
          setOpen((o) => !o);
        }}
        onKeyDown={(e) => {
          if (e.key === " " || e.key === "Enter") {
            e.preventDefault();
            setOpen((o) => !o);
          }
        }}
      >
        ?
      </button>

      {visible && (
        <div
          ref={panelRef}
          id={tooltipId}
          role="tooltip"
          className={cn(
            "absolute z-50 w-64 max-w-[min(18rem,calc(100vw-2rem))] rounded-md border border-neutral-200 bg-white px-3 py-2 text-left text-sm leading-snug text-neutral-800 shadow-md dark:border-neutral-700 dark:bg-neutral-900 dark:text-neutral-100",
            place.panel,
          )}
        >
          <p className="m-0 text-xs text-neutral-700 dark:text-neutral-200">{text}</p>
          {moreHref != null && (
            <p className="m-0 mt-2 text-xs">
              <a
                className="font-medium text-teal-700 underline-offset-2 hover:underline dark:text-teal-300"
                href={moreHref}
                target="_blank"
                rel="noopener noreferrer"
              >
                Learn more →
              </a>
            </p>
          )}
        </div>
      )}
    </span>
  );
}
