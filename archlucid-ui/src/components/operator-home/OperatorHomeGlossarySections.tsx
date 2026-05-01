"use client";

import Link from "next/link";
import { Check, FileCheck, ListOrdered, Play, Rocket } from "lucide-react";
import type { ComponentType } from "react";
import { Fragment } from "react";

import { Tooltip, TooltipContent, TooltipTrigger } from "@/components/ui/tooltip";

type PipelineStepStatus = "not-started" | "current" | "completed";

type PipelineStepConfig = {
  step: 1 | 2 | 3 | 4;
  stage: string;
  icon: ComponentType<{ className?: string; "aria-hidden"?: boolean }>;
  label: string;
  description: string;
  href: string;
  shortcut?: string;
  linkAccessibleName?: string;
  /** Explains the Finalize step when the label alone is too abstract. */
  tooltip?: string;
};

const PIPELINE_STEPS: PipelineStepConfig[] = [
  {
    step: 1,
    stage: "Start",
    icon: Rocket,
    label: "Create Request",
    description: "Capture architecture intent, requirements, and constraints.",
    href: "/reviews/new",
    shortcut: "Alt+N",
  },
  {
    step: 2,
    stage: "Track",
    icon: ListOrdered,
    label: "Track Progress",
    description: "Monitor pipeline progress and inspect run detail.",
    href: "/reviews?projectId=default",
    shortcut: "Alt+R",
    linkAccessibleName: "Runs",
  },
  {
    step: 3,
    stage: "Finalize",
    icon: Play,
    label: "Finalize Manifest",
    description: "Finalize the reviewed manifest and export artifacts.",
    href: "/reviews?projectId=default",
    tooltip:
      "Finalizing records the reviewed manifest as the authoritative output for this run. You can still compare, export, and review its artifacts afterward.",
  },
  {
    step: 4,
    stage: "Review",
    icon: FileCheck,
    label: "Review Artifacts",
    description: "Review, download, and share architecture artifacts.",
    href: "/reviews?projectId=default",
  },
];

function PipelineConnectorBar() {
  return (
    <div className="flex w-5 shrink-0 items-center justify-center self-center" aria-hidden>
      <div className="h-0.5 w-4 shrink-0 rounded-full bg-teal-300 dark:bg-teal-700" />
    </div>
  );
}

/**
 * Product-layer cards for operator home — replaces the prior prose-heavy glossary sections.
 * Four action cards for Core Pilot + two summary cards for optional maturity layers.
 */
export function OperatorHomeGlossarySections() {
  return (
    <section className="mt-1 mb-2" aria-labelledby="quick-actions-heading">
      <h3 id="quick-actions-heading" className="sr-only">
        Quick actions
      </h3>
      <div className="grid grid-cols-1 gap-3 sm:grid-cols-2 lg:hidden">
        {PIPELINE_STEPS.map((config) => (
          <ActionCard key={config.step} {...config} />
        ))}
      </div>
      <div className="hidden lg:flex lg:items-stretch lg:gap-0">
        {PIPELINE_STEPS.map((config, index) => (
          <Fragment key={config.step}>
            <div className="min-w-0 flex-1">
              <ActionCard {...config} />
            </div>
            {index < PIPELINE_STEPS.length - 1 ? <PipelineConnectorBar /> : null}
          </Fragment>
        ))}
      </div>
    </section>
  );
}

type ActionCardProps = PipelineStepConfig & {
  /** Optional stepper state for future pipeline UX; omitted = all steps use the default “current” emphasis. */
  pipelineStatus?: PipelineStepStatus;
};

function StepIndicator({ step, pipelineStatus }: { step: 1 | 2 | 3 | 4; pipelineStatus?: PipelineStepStatus }) {
  const resolved: PipelineStepStatus = pipelineStatus ?? "current";
  const base =
    "flex h-5 w-5 shrink-0 items-center justify-center rounded-full text-[10px] font-bold leading-none";

  if (resolved === "completed") {
    return (
      <span
        className={`${base} bg-emerald-100 text-emerald-800 dark:bg-emerald-900/80 dark:text-emerald-300`}
        aria-hidden
      >
        <Check className="h-3 w-3" strokeWidth={3} aria-hidden />
      </span>
    );
  }

  if (resolved === "not-started") {
    return (
      <span
        className={`${base} bg-neutral-200 text-neutral-600 dark:bg-neutral-700 dark:text-neutral-400`}
        aria-hidden
      >
        {step}
      </span>
    );
  }

  return (
    <span
      className={`${base} bg-teal-100 text-teal-800 dark:bg-teal-900/80 dark:text-teal-300`}
      aria-hidden
    >
      {step}
    </span>
  );
}

function ActionCard({
  step,
  stage,
  icon: Icon,
  label,
  description,
  href,
  shortcut,
  linkAccessibleName,
  pipelineStatus,
  tooltip,
}: ActionCardProps) {
  const link = (
    <Link
      href={href}
      aria-label={linkAccessibleName}
      className={`group flex h-full flex-col gap-2 rounded-lg border p-4 no-underline transition-shadow ${
        pipelineStatus === "current"
          ? "border-teal-300 bg-teal-50/30 shadow-md ring-1 ring-teal-500/20 hover:shadow-lg dark:border-teal-700/60 dark:bg-teal-900/20 dark:ring-teal-500/20"
          : "border-neutral-200 bg-white shadow-sm hover:shadow-md dark:border-neutral-700 dark:bg-neutral-900"
      }`}
    >
      <div className="flex items-start gap-2">
        <StepIndicator step={step} pipelineStatus={pipelineStatus} />
        <div className="min-w-0 flex flex-col gap-0">
          <span className="text-[9px] font-semibold uppercase tracking-wide text-neutral-500 dark:text-neutral-500">
            {stage}
          </span>
          <p className="m-0 text-xs font-semibold uppercase tracking-wide text-neutral-500 dark:text-neutral-500">
            Step {step}
          </p>
        </div>
      </div>
      <div className="flex items-center gap-2">
        <Icon className="h-7 w-7 shrink-0 text-teal-700 dark:text-teal-400" aria-hidden />
        <span className="text-base font-bold text-neutral-900 group-hover:text-teal-800 dark:text-neutral-100 dark:group-hover:text-teal-300">
          {label}
        </span>
        {shortcut ? (
          <kbd className="ml-auto rounded border border-neutral-200 bg-neutral-50 px-1.5 py-0.5 font-mono text-[10px] text-neutral-500 dark:border-neutral-700 dark:bg-neutral-800 dark:text-neutral-400">
            {shortcut}
          </kbd>
        ) : null}
      </div>
      <span className="text-xs leading-snug text-neutral-600 dark:text-neutral-400">{description}</span>
    </Link>
  );

  if (tooltip !== undefined && tooltip.length > 0) {
    return (
      <Tooltip>
        <TooltipTrigger asChild>{link}</TooltipTrigger>
        <TooltipContent side="bottom" className="max-w-xs text-left">
          {tooltip}
        </TooltipContent>
      </Tooltip>
    );
  }

  return link;
}
