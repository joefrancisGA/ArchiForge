"use client";

import type { ReactNode } from "react";

import { Tooltip, TooltipContent, TooltipTrigger } from "@/components/ui/tooltip";
import { type GlossaryTermId, GLOSSARY_TERMS } from "@/lib/glossary-terms";

export type GlossaryTermProps = {
  termId: GlossaryTermId;
  children: ReactNode;
};

/** Inline term with dotted underline and a concise definition (use inside an existing TooltipProvider when possible). */
export function GlossaryTerm({ termId, children }: GlossaryTermProps) {
  const definition = GLOSSARY_TERMS[termId];

  return (
    <Tooltip>
      <TooltipTrigger asChild>
        <span className="cursor-help border-b border-dotted border-neutral-500 text-inherit dark:border-neutral-400">
          {children}
        </span>
      </TooltipTrigger>
      <TooltipContent side="top" className="max-w-sm text-sm">
        {definition}
      </TooltipContent>
    </Tooltip>
  );
}
