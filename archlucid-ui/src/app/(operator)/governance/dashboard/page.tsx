import Link from "next/link";

import { ExecutiveWorkspaceHealthDashboard } from "@/components/ExecutiveWorkspaceHealthDashboard";
import { isNextPublicDemoMode } from "@/lib/demo-ui-env";
import { isStaticDemoPayloadFallbackEnabled } from "@/lib/operator-static-demo";

/**
 * Executive Workspace Health — `/governance/dashboard` — five KPI tiles from existing APIs in the active SESSION_CONTEXT only.
 */
export default function GovernanceDashboardPage() {
  if (isNextPublicDemoMode() || isStaticDemoPayloadFallbackEnabled()) {
    return (
      <div className="rounded-lg border border-neutral-200 bg-neutral-50 p-6 text-sm text-neutral-600 dark:border-neutral-800 dark:bg-neutral-900 dark:text-neutral-400">
        <p className="m-0 font-medium text-neutral-800 dark:text-neutral-200">
          Executive Workspace Health is not available in demo mode.
        </p>
        <p className="m-0 mt-1">
          Use the <Link className="font-medium underline" href="/governance">governance workflow</Link> and{" "}
          <Link className="font-medium underline" href="/audit">
            audit log
          </Link>{" "}
          for current reviews.
        </p>
      </div>
    );
  }

  return <ExecutiveWorkspaceHealthDashboard />;
}
