"use client";

import { cn } from "@/lib/utils";

const MILESTONE_SHORT: readonly string[] = ["Request", "Pipeline", "Finalize", "Package"];

/** Compact Core Pilot milestones aligned with docs/CORE_PILOT §3 four steps — server + checklist signals only. */
export function CorePilotMilestoneRail(props: {
  milestoneComplete: readonly [boolean, boolean, boolean, boolean];
  /** First incomplete index 0–3, or last index when all complete. */
  activeIndex: number;
}) {
  const { milestoneComplete, activeIndex } = props;

  return (
    <nav
      className="mb-3 rounded-md border border-neutral-200/90 bg-neutral-50/90 px-2 py-2 dark:border-neutral-700 dark:bg-neutral-900/50"
      aria-label="Core Pilot milestone progress"
      data-testid="core-pilot-milestone-rail"
    >
      <p className="m-0 mb-1.5 text-[10px] font-semibold uppercase tracking-wide text-neutral-600 dark:text-neutral-400">
        Milestones (architecture review packaging)
      </p>
      <ol className="m-0 flex min-w-0 flex-nowrap gap-1 overflow-x-auto pb-0.5 [scrollbar-width:thin]">
        {MILESTONE_SHORT.map((label, index) => {
          const complete = milestoneComplete[index] === true;
          const current = activeIndex === index;

          return (
            <li
              key={label}
              className={cn(
                "flex min-w-[5.25rem] flex-1 list-none flex-col items-center rounded-sm border px-1 py-1 text-center max-sm:min-w-[4.5rem]",
                complete
                  ? "border-teal-200 bg-white text-teal-900 dark:border-teal-900 dark:bg-teal-950/40 dark:text-teal-100"
                  : current
                    ? "border-teal-500 bg-teal-50 shadow-sm dark:border-teal-500 dark:bg-teal-950/60"
                    : "border-neutral-200 bg-white/70 text-neutral-600 dark:border-neutral-700 dark:bg-neutral-950/60 dark:text-neutral-400",
              )}
              data-testid={`core-pilot-milestone-${index}`}
              aria-current={current ? "step" : undefined}
            >
              <span className="text-[10px] font-semibold uppercase tracking-wide text-neutral-500 dark:text-neutral-400">
                {index + 1}
              </span>
              <span className="text-[11px] font-medium leading-snug text-neutral-900 dark:text-neutral-100">{label}</span>
              <span className="sr-only">{complete ? "complete" : "not complete"}</span>
            </li>
          );
        })}
      </ol>
      <p className="m-0 mt-1 text-[10px] leading-snug text-neutral-500 dark:text-neutral-400">
        Status comes from saved runs plus commit signals — checklist checkboxes capture what you verified.
      </p>
    </nav>
  );
}
