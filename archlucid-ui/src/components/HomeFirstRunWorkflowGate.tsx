"use client";

import { OperatorFirstRunWorkflowPanel } from "@/components/OperatorFirstRunWorkflowPanel";
import { isNextPublicDemoMode } from "@/lib/demo-ui-env";

/**
 * Wraps {@link OperatorFirstRunWorkflowPanel}: in `NEXT_PUBLIC_DEMO_MODE`, the right rail elevates reviewing the
 * completed Claims Intake spine over pure first-run language.
 */
export function HomeFirstRunWorkflowGate() {
  return <OperatorFirstRunWorkflowPanel exploreCompletedOutput={isNextPublicDemoMode()} />;
}
