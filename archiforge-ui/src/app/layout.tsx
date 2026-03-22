import "./globals.css";
import { AuthStatus } from "@/components/AuthStatus";
import Link from "next/link";
import type { ReactNode } from "react";

export const metadata = {
  title: "ArchiForge",
  description: "ArchiForge operator shell",
};

export default function RootLayout({ children }: { children: ReactNode }) {
  return (
    <html lang="en">
      <body>
        <div style={{ padding: 24, fontFamily: "system-ui, Arial, sans-serif" }}>
          <header style={{ marginBottom: 24 }}>
            <h1 style={{ margin: "0 0 8px" }}>ArchiForge</h1>
            <nav style={{ display: "flex", gap: 16, flexWrap: "wrap" }}>
              <Link href="/">Home</Link>
              <Link href="/runs?projectId=default">Runs</Link>
              <Link href="/compare">Compare</Link>
              <Link href="/replay">Replay</Link>
              <Link href="/graph">Graph</Link>
              <Link href="/ask">Ask</Link>
              <Link href="/search">Search</Link>
              <Link href="/advisory">Advisory</Link>
              <Link href="/recommendation-learning">Learning</Link>
              <Link href="/advisory-scheduling">Schedules</Link>
              <Link href="/digests">Digests</Link>
              <Link href="/digest-subscriptions">Subscriptions</Link>
            </nav>
          </header>
          <AuthStatus />
          {children}
        </div>
      </body>
    </html>
  );
}
