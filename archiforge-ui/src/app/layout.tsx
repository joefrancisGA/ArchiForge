import type { Metadata } from "next";
import Link from "next/link";
import type { ReactNode } from "react";

import { AuthPanel } from "@/components/AuthPanel";
import { ShellNav } from "@/components/ShellNav";

import "./globals.css";

export const metadata: Metadata = {
  title: {
    default: "ArchiForge operator shell",
    template: "%s · ArchiForge",
  },
  description:
    "Operator UI for architecture runs, manifests, artifacts, graphs, compare, replay, and governance.",
};

/** Root layout: shell chrome (header, grouped nav, auth) and page content. */
export default function RootLayout({ children }: { children: ReactNode }) {
  return (
    <html lang="en">
      <body>
        <div style={{ padding: 24, fontFamily: "system-ui, Arial, sans-serif" }}>
          <header style={{ marginBottom: 24 }}>
            <h1 style={{ margin: "0 0 12px", fontSize: 26 }}>
              <Link href="/" style={{ color: "inherit", textDecoration: "none" }}>
                ArchiForge
              </Link>
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
