import type { Metadata } from "next";
import type { ReactNode } from "react";

export const metadata: Metadata = {
  title: "Manifest",
};

export default function ManifestLayout({ children }: { children: ReactNode }) {
  return children;
}
