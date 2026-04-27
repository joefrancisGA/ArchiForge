import { FeatureNotAvailable } from "@/components/product/FeatureAvailabilityStates";

/**
 * The prior cross-run dashboard is not demo-ready; nav entry is removed. Direct links show this state until rebuilt.
 */
export default function GovernanceDashboardPage() {
  return (
    <main className="mx-auto max-w-3xl space-y-4">
      <h1 className="text-2xl font-semibold text-neutral-900 dark:text-neutral-100">Governance dashboard</h1>
      <FeatureNotAvailable />
    </main>
  );
}
