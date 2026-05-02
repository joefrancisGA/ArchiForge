import type { Metadata } from "next";

import { AfterCorePilotChecklistHint } from "@/components/AfterCorePilotChecklistHint";
import { OperatorNextActionsCard } from "@/components/OperatorNextActionsCard";
import { PilotOutcomeCard } from "@/components/PilotOutcomeCard";
import { OperatorTaskSuccessTile } from "@/components/OperatorTaskSuccessTile";
import { OperatorCorePilotDiagnosticsChecklist } from "@/components/OperatorCorePilotDiagnosticsChecklist";
import { SampleFirstReviewPackageCard } from "@/components/SampleFirstReviewPackageCard";
import { BeforeAfterDeltaPanel } from "@/components/BeforeAfterDeltaPanel";
import { HomeFirstRunWorkflowGate } from "@/components/HomeFirstRunWorkflowGate";
import { OperatorHomeGate } from "@/components/OperatorHomeGate";
import { TrialWelcomeRunDeepLink } from "@/components/TrialWelcomeRunDeepLink";
import { OperationalMetricsGate } from "@/components/operator-home/OperationalMetricsGate";
import { RunsDashboardPanel } from "@/components/operator-home/RunsDashboardPanel";
import { HomeMaturityLayerCards } from "@/components/operator-home/HomeMaturityLayerCards";
import { HelpLink } from "@/components/HelpLink";
import { WelcomeBanner } from "@/components/WelcomeBanner";
import { ValueRealizationDashboard } from "@/components/ValueRealizationDashboard";

export const metadata: Metadata = {
  title: "Operator home",
};

/** Landing page: hero CTA, action cards, workflow checklist, and operational metrics. */
export default function HomePage() {
  return (
    <OperatorHomeGate>
    <TrialWelcomeRunDeepLink />
    <main className="space-y-6">
      <WelcomeBanner />
      
      <ValueRealizationDashboard />

      <SampleFirstReviewPackageCard />

      <OperatorNextActionsCard />

      <div className="grid grid-cols-1 gap-6 lg:grid-cols-[minmax(0,1fr)_minmax(260px,320px)] lg:items-start">
        <div className="min-w-0 space-y-6">
          <RunsDashboardPanel />
          <OperatorCorePilotDiagnosticsChecklist />
          <AfterCorePilotChecklistHint />

          <OperationalMetricsGate>
            <section aria-labelledby="operational-metrics-heading">
              <div className="mb-3 flex flex-wrap items-center gap-2">
                <h3
                  id="operational-metrics-heading"
                  className="m-0 text-sm font-bold uppercase tracking-wide text-neutral-600 dark:text-neutral-300"
                >
                  Operational metrics
                </h3>

                <HelpLink
                  docPath="/docs/CORE_PILOT.md"
                  label="Open the core pilot guide on GitHub (new tab)"
                />
              </div>
              <div className="grid grid-cols-1 gap-4 lg:grid-cols-2">
                <PilotOutcomeCard />
                <OperatorTaskSuccessTile />
              </div>
            </section>

            <BeforeAfterDeltaPanel />

            <HomeMaturityLayerCards />
          </OperationalMetricsGate>
        </div>

        <aside
          className="min-w-0 space-y-3 pt-0 lg:sticky lg:top-20 lg:self-start"
          aria-label="Explore completed output and first-review checklist"
        >
          <HomeFirstRunWorkflowGate />
        </aside>
      </div>
    </main>
    </OperatorHomeGate>
  );
}
