import type { Metadata } from "next";
import type { ReactNode } from "react";

export const metadata: Metadata = {
  title: "Replay run (authority)",
};

export default function ReplayLayout({ children }: { children: ReactNode }) {
  return children;
}
