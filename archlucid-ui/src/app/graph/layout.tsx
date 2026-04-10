import type { Metadata } from "next";
import type { ReactNode } from "react";

export const metadata: Metadata = {
  title: "Run graph (provenance)",
};

export default function GraphLayout({ children }: { children: ReactNode }) {
  return children;
}
