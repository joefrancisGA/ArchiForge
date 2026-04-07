import type { Metadata } from "next";
import type { ReactNode } from "react";

export const metadata: Metadata = {
  title: "Planning",
  description:
    "59R improvement themes and prioritized plans: browse evidence-backed planning items (read-only).",
};

export default function PlanningLayout({ children }: { children: ReactNode }) {
  return children;
}
