import Link from "next/link";

import { type LayerId } from "@/lib/getLayerForRoute";
import { cn } from "@/lib/utils";

const LAYER_COPY: Record<LayerId, { label: string; question: string; strip: string; labelClass: string }> = {
  pilot: {
    label: "Pilot",
    question: "Can we go from request to committed manifest faster?",
    strip: "bg-blue-50/90 border-b border-blue-200/60 dark:border-blue-900/50 dark:bg-blue-950/30",
    labelClass: "text-blue-900 dark:text-blue-200"
  },
  "operate-analysis": {
    label: "Analysis",
    question: "What changed, why, and what does the architecture look like?",
    strip: "bg-teal-50/80 border-b border-teal-200/60 dark:border-teal-900/40 dark:bg-teal-950/30",
    labelClass: "text-teal-900 dark:text-teal-200"
  },
  "operate-governance": {
    label: "Governance",
    question: "How do we govern, audit, and operationalize architecture decisions?",
    strip: "bg-amber-50/80 border-b border-amber-200/60 dark:border-amber-900/40 dark:bg-amber-950/25",
    labelClass: "text-amber-900 dark:text-amber-200"
  }
};

export type LayerContextStripProps = {
  layerId: LayerId;
  className?: string;
};

/**
 * Persistent one-line product-layer cue under the app header: layer label, guiding question, optional
 * return link on Operate routes. Styling stays subtle (orientation, not a second hero).
 */
export function LayerContextStrip({ layerId, className }: LayerContextStripProps) {
  const copy = LAYER_COPY[layerId];
  const isOperate = layerId === "operate-analysis" || layerId === "operate-governance";

  return (
    <div
      aria-labelledby="operator-layer-context-text"
      className={cn("min-h-9 w-full", copy.strip, className)}
      data-layer-context-strip=""
      data-testid="layer-context-strip"
      role="region"
    >
      <div className="mx-auto flex h-full min-h-9 max-w-[1600px] flex-wrap items-center gap-x-2 gap-y-0.5 px-4 py-1.5 text-sm font-normal leading-tight text-neutral-800 dark:text-neutral-200 lg:px-6">
        <p
          className="m-0 min-w-0 flex-1 text-sm"
          id="operator-layer-context-text"
        >
          <span className={cn("font-medium", copy.labelClass)}>{copy.label}</span>
          <span className="text-neutral-500 dark:text-neutral-400" aria-hidden>
            {" "}
            —{" "}
          </span>
          <span className="font-normal text-neutral-800 dark:text-neutral-200">
            {copy.question}
          </span>
        </p>
        {isOperate ? (
          <Link
            className="shrink-0 text-sm font-medium text-neutral-600 underline decoration-neutral-300 underline-offset-2 transition hover:text-neutral-900 focus-visible:outline focus-visible:ring-2 focus-visible:ring-offset-1 dark:text-neutral-400 dark:decoration-neutral-600 dark:hover:text-neutral-100"
            data-testid="layer-context-back-pilot"
            href="/"
          >
            Back to Core Pilot
          </Link>
        ) : null}
      </div>
    </div>
  );
}
