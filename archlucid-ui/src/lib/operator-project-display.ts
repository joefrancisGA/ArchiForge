import { isBuyerPolishedOperatorShellEnv } from "@/lib/demo-ui-env";

/**
 * Maps raw project ids to buyer-facing labels in demo builds (e.g. {@code default} workspace).
 */
export function formatOperatorProjectIdDisplay(projectId: string): string {
  const trimmed = projectId.trim();

  if (trimmed.toLowerCase() === "default") {
    return isBuyerPolishedOperatorShellEnv() ? "Architecture review workspace" : "Primary workspace";
  }

  if (trimmed === "claims-intake-sample-workspace") {
    return "Claims Intake sample workspace";
  }

  return projectId;
}
