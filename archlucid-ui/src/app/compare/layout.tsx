import type { Metadata } from "next";
import type { ReactNode } from "react";

export const metadata: Metadata = {
  title: "Compare two runs",
};

export default function CompareLayout({ children }: { children: ReactNode }) {
  return children;
}
