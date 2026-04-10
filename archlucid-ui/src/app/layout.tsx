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
        <a href="#main-content" className="skip-to-main">
          Skip to main content
        </a>
        <div className="mx-auto max-w-7xl p-6">
          <header className="mb-6 border-b border-neutral-200 pb-4">
            <h1 className="mb-3 text-2xl font-semibold tracking-tight">
              <Button variant="ghost" className="h-auto p-0 text-2xl font-semibold" asChild>
                <Link href="/" aria-label="ArchLucid — go to operator home">
                  ArchLucid
                </Link>
              </Button>
            </h1>
            <ShellNav />
          </header>
          <AuthPanel />
          <div
            id="main-content"
            tabIndex={-1}
            className="outline-none focus:outline-none focus-visible:ring-2 focus-visible:ring-neutral-400 focus-visible:ring-offset-2"
          >
            {children}
          </div>
        </div>
      </body>
    </html>
  );
}
