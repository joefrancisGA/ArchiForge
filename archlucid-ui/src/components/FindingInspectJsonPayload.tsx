"use client";

import { CollapsibleJsonTree } from "@/components/CollapsibleJsonTree";

/**
 * Client island for the finding inspector typed JSON payload (avoids adding "use client" to the full view).
 */
export function FindingInspectJsonPayload({ value }: { value: unknown }) {
  return <CollapsibleJsonTree value={value} aria-label="Typed finding payload" />;
}
