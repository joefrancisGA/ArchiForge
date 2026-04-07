import type { Metadata } from "next";
import type { ReactNode } from "react";

export const metadata: Metadata = {
  title: "Plan detail",
  description: "Read-only improvement plan: steps, priority, and evidence link counts (59R).",
};

export default function PlanningPlanLayout({ children }: { children: ReactNode }) {
  return children;
}
