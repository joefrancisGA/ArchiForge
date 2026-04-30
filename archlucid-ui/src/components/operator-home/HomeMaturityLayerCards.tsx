"use client";

import Link from "next/link";
import type { ReactNode } from "react";
import { BarChart3, Search, Shield } from "lucide-react";

import { isNextPublicDemoMode } from "@/lib/demo-ui-env";

type LayerCardProps = {
  icon: ReactNode;
  title: string;
  items: string[];
  href: string;
};

function LayerCard({ icon, title, items, href }: LayerCardProps) {
  return (
    <Link
      href={href}
      className="group rounded-lg border border-neutral-200 bg-white p-4 no-underline shadow-sm transition-shadow hover:shadow-md dark:border-neutral-700 dark:bg-neutral-900"
    >
      <div className="mb-2 flex items-center gap-2">
        {icon}
        <h2 className="m-0 text-base font-semibold text-neutral-900 dark:text-neutral-100">{title}</h2>
      </div>
      <ul className="m-0 list-disc space-y-1 pl-5 text-sm text-neutral-600 dark:text-neutral-400">
        {items.map((item) => (
          <li key={item}>{item}</li>
        ))}
      </ul>
    </Link>
  );
}

/**
 * Home “Explore when ready” cards: trims advanced-only labels outside demo mode so first sessions are not buried in
 * roadmap vocabulary (Planning, Value report, etc.) once a committed manifest exists.
 */
export function HomeMaturityLayerCards() {
  const demoUi = isNextPublicDemoMode();

  const advancedItems = demoUi
    ? (["Compare runs", "Replay", "Graph", "Ask", "Architecture advisory"] as const)
    : (["Compare runs", "Replay", "Graph"] as const);

  const searchItems = demoUi
    ? (["Indexed search", "Planning", "Digests", "Value report"] as const)
    : (["Indexed search"] as const);

  return (
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
          items={[...advancedItems]}
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
          items={[...searchItems]}
          href="/search"
        />
      </div>
    </section>
  );
}
