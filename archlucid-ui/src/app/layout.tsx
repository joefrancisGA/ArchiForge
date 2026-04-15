import type { Metadata } from "next";
import type { ReactNode } from "react";

import { AppShellClient } from "@/components/AppShellClient";

import "./globals.css";

export const metadata: Metadata = {
  title: {
    default: "ArchLucid operator shell",
    template: "%s · ArchLucid",
  },
  description:
    "Operator UI for architecture runs, manifests, artifacts, graphs, compare, replay, and governance.",
};

/** Root layout: shell chrome (sidebar, header, auth) delegates to `AppShellClient`. */
export default function RootLayout({ children }: { children: ReactNode }) {
  return (
    <html lang="en" suppressHydrationWarning>
      <head>
        <script
          dangerouslySetInnerHTML={{
            __html:
              "(function(){try{var k='archlucid_color_mode';var m=localStorage.getItem(k)||'system';var d=m==='dark'||(m!=='light'&&window.matchMedia('(prefers-color-scheme: dark)').matches);document.documentElement.classList.toggle('dark',d);}catch(e){}})();",
          }}
        />
      </head>
      <body className="min-h-screen font-sans">
        <AppShellClient>{children}</AppShellClient>
      </body>
    </html>
  );
}
