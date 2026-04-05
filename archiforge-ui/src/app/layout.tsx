import type { Metadata } from "next";
import Link from "next/link";
import type { ReactNode } from "react";

import { AuthPanel } from "@/components/AuthPanel";
import { ShellNav } from "@/components/ShellNav";
import { Button } from "@/components/ui/button";

import "./globals.css";

export const metadata: Metadata = {
  title: {
    default: "ArchLucid operator shell",
    template: "%s · ArchLucid",
  },
  description:
    "Operator UI for architecture runs, manifests, artifacts, graphs, compare, replay, and governance.",
};

/** Root layout: shell chrome (header, grouped nav, auth) and page content. */
export default function RootLayout({ children }: { children: ReactNode }) {
  return (
    <html lang="en">
      <body className="min-h-screen font-sans">
        <div className="mx-auto max-w-7xl p-6">
          <header className="mb-6 border-b border-neutral-200 pb-4">
            <h1 className="mb-3 text-2xl font-semibold tracking-tight">
              <Button variant="ghost" className="h-auto p-0 text-2xl font-semibold" asChild>
                <Link href="/">ArchLucid</Link>
              </Button>
            </h1>
            <ShellNav />
          </header>
          <AuthPanel />
          {children}
        </div>
      </body>
    </html>
  );
}
