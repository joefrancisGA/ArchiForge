import type { Metadata } from "next";
import type { ReactNode } from "react";

export const metadata: Metadata = {
  title: "Compare two reviews",
};

export default function CompareLayout({ children }: { children: ReactNode }) {
  return children;
}
