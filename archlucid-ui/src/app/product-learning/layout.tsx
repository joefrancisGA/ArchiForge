import type { Metadata } from "next";
import type { ReactNode } from "react";

export const metadata: Metadata = {
  title: "Pilot feedback",
  description:
    "Product learning dashboard: trusted vs rejected trends, revision patterns, improvement opportunities, triage queue (58R).",
};

export default function ProductLearningLayout({ children }: { children: ReactNode }) {
  return children;
}
