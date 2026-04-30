import type { Metadata } from "next";
import Link from "next/link";
import { BarChart3, Search, Shield } from "lucide-react";

import { CorePilotOneSessionChecklist } from "@/components/CorePilotOneSessionChecklist";
import { AfterCorePilotChecklistHint } from "@/components/AfterCorePilotChecklistHint";
import { PilotOutcomeCard } from "@/components/PilotOutcomeCard";
import { OperatorTaskSuccessTile } from "@/components/OperatorTaskSuccessTile";
import { OperatorCorePilotDiagnosticsChecklist } from "@/components/OperatorCorePilotDiagnosticsChecklist";
import { BeforeAfterDeltaPanel } from "@/components/BeforeAfterDeltaPanel";
import { HomeFirstRunWorkflowGate } from "@/components/HomeFirstRunWorkflowGate";
import { OperatorHomeGate } from "@/components/OperatorHomeGate";
import { TrialWelcomeRunDeepLink } from "@/components/TrialWelcomeRunDeepLink";
import { OperationalMetricsGate } from "@/components/operator-home/OperationalMetricsGate";
import { RunsDashboardPanel } from "@/components/operator-home/RunsDashboardPanel";
import { HelpLink } from "@/components/HelpLink";
import { WelcomeBanner } from "@/components/WelcomeBanner";

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

      <CorePilotOneSessionChecklist />

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

            <section aria-labelledby="maturity-layers-heading">
              <h3
                id="maturity-layers-heading"
                className="mb-3 text-sm font-bold uppercase tracking-wide text-neutral-600 dark:text-neutral-300"
              >
                Explore when ready
              </h3>
              <div className="grid grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-3">
                <LayerCard
                  icon={<BarChart3 className="h-5 w-5 text-sky-600 dark:text-sky-400" aria-hidden />}
                  title="Advanced Analysis"
                  items={["Compare runs", "Replay", "Graph", "Ask", "Architecture advisory"]}
                  href="/compare"
                />
                <LayerCard
                  icon={<Shield className="h-5 w-5 text-violet-600 dark:text-violet-400" aria-hidden />}
                  title="Enterprise Controls"
                  items={["Governance", "Policy packs", "Audit log", "Alerts"]}
                  href="/governance/findings"
                />
                <LayerCard
                  icon={<Search className="h-5 w-5 text-amber-600 dark:text-amber-400" aria-hidden />}
                  title="Search & Insights"
                  items={["Indexed search", "Planning", "Digests", "Value report"]}
                  href="/search"
                />
              </div>
            </section>
          </OperationalMetricsGate>
        </div>

        <aside
          className="min-w-0 space-y-3 pt-0 lg:sticky lg:top-20 lg:self-start"
          aria-label="Explore completed output and first-manifest guide"
        >
          <HomeFirstRunWorkflowGate />
        </aside>
      </div>
    </main>
    </OperatorHomeGate>
  );
}

function LayerCard({
  icon,
  title,
  items,
  href,
}: {
  icon: React.ReactNode;
  title: string;
  items: string[];
  href: string;
}) {
  return (
    <Link
      href={href}
      className="group rounded-lg border border-neutral-200 bg-white p-4 no-underline shadow-sm transition-shadow hover:shadow-md dark:border-neutral-700 dark:bg-neutral-900"
    >
      <div className="flex items-center gap-2">
        {icon}
        <span className="text-sm font-semibold text-neutral-900 group-hover:text-teal-800 dark:text-neutral-100 dark:group-hover:text-teal-300">
          {title}
        </span>
      </div>
      <p className="mt-2 text-xs text-neutral-500 dark:text-neutral-400">
        {items.join(" · ")}
      </p>
    </Link>
  );
}
