"use client";

import type { ReactNode } from "react";

import { GlossaryTooltip } from "@/components/GlossaryTooltip";
import { type GlossaryTermKey } from "@/lib/glossary-terms";

export type GlossaryTermProps = {
  termId: GlossaryTermKey;
  children: ReactNode;
};

export function GlossaryTerm({ termId, children }: GlossaryTermProps) {
  return <GlossaryTooltip termKey={termId}>{children}</GlossaryTooltip>;
}
