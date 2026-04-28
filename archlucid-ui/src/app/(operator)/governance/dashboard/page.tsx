import { HelpLink } from "@/components/HelpLink";
import { ContextualHelp } from "@/components/ContextualHelp";
import { FeatureNotAvailable } from "@/components/product/FeatureAvailabilityStates";

/**
 * The prior cross-run dashboard is not demo-ready; nav entry is removed. Direct links show this state until rebuilt.
 */
export default function GovernanceDashboardPage() {
  return (
    <main className="mx-auto max-w-3xl space-y-4">
      <div className="flex flex-wrap items-center gap-2">
        <h1 className="m-0 text-2xl font-semibold text-neutral-900 dark:text-neutral-100">Governance dashboard</h1>
        <ContextualHelp helpKey="governance-dashboard" />
        <HelpLink
          docPath="/docs/library/GOVERNANCE_WORKFLOW_UI.md"
          label="Governance workflows documentation on GitHub (new tab)"
        />
      </div>
      <FeatureNotAvailable />
    </main>
  );
}
