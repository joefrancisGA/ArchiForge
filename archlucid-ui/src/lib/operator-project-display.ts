import { isNextPublicDemoMode } from "@/lib/demo-ui-env";

/**
 * Maps raw project ids to buyer-facing labels in demo builds (e.g. {@code default} workspace).
 */
export function formatOperatorProjectIdDisplay(projectId: string): string {
  const trimmed = projectId.trim();

  if (isNextPublicDemoMode() && trimmed.toLowerCase() === "default") {
    return "Architecture Review Workspace";
  }

  return projectId;
}
