"use client";

import { useId, useState } from "react";

import { cn } from "@/lib/utils";

/**
 * Long policy or rule text: line-clamp by default with an accessible expand control.
 */
export function ClampedLongText({
  text,
  className,
  maxLines = 3,
}: {
  text: string;
  className?: string;
  /** Tailwind line-clamp utility (e.g. 3 → line-clamp-3). */
  maxLines?: 3 | 4 | 5 | 6;
}) {
  const [open, setOpen] = useState(false);
  const id = useId();
  const linesClass =
    maxLines === 3
      ? "line-clamp-3"
      : maxLines === 4
        ? "line-clamp-4"
        : maxLines === 5
          ? "line-clamp-5"
          : "line-clamp-6";

  if (text.length === 0) {
    return null;
  }

  const lineCount = text.split("\n").length;
  const needsToggle = lineCount > maxLines || text.length > 240;

  return (
    <div className={cn("min-w-0", className)}>
      <p id={id} className={cn("m-0 whitespace-pre-wrap break-words", !open && needsToggle && linesClass)}>
        {text}
      </p>
      {needsToggle ? (
        <button
          type="button"
          className="mt-1 text-left text-sm font-medium text-sky-800 underline focus:outline-none focus:ring-2 focus:ring-sky-500 dark:text-sky-200"
          onClick={() => setOpen((v) => !v)}
          aria-expanded={open}
          aria-controls={id}
        >
          {open ? "Show less" : "Read more"}
        </button>
      ) : null}
    </div>
  );
}
